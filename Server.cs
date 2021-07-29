using System;
using System.Collections.Generic;
using System.IO;
using System.Json;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using HttpMultipartParser;
using Microsoft.AspNetCore.StaticFiles;
using Savok.Server.Abstractions;
using Savok.Server.Exceptions;
using Savok.Server.Utils;
using Action = Savok.Server.Abstractions.Action;
using Task = System.Threading.Tasks.Task;

namespace Savok.Server {
	public class Server {
		private HttpListener HttpListener { get; }
        
		private Dictionary<string, Action> Actions { get; }
		private Dictionary<string, MultipartAction> MultipartActions { get; }
		
		private List<Abstractions.Task> Tasks { get; }
		
		private List<HttpListenerWebSocketContext> WebSocketContexts { get; }

		public delegate void OnWebSocketConnectedHandler(HttpListenerWebSocketContext context);
		public delegate bool OnMethodVerificationRequired(HttpListenerContext context);
		public delegate void OnVerificationFailed(HttpListenerContext context);
		public delegate void CustomMethodHandler(HttpListenerContext context);

		public event OnWebSocketConnectedHandler OnWebSocketConnected;

		public event OnMethodVerificationRequired OnGetVerificationRequired;
		public event OnMethodVerificationRequired OnPostVerificationRequired;
		public event OnMethodVerificationRequired OnWebSocketVerificationRequired;
		
		public event OnVerificationFailed OnGetVerificationFailed;
		public event OnVerificationFailed OnPostVerificationFailed;
		public event OnVerificationFailed OnWebSocketVerificationFailed;

		public bool EnableCustomGetHander { get; set; }
		public bool EnableCustomPostHander { get; set; }
		
		public CustomMethodHandler CustomGetHandler { get; set; }
		public CustomMethodHandler CustomPostHandler { get; set; }

		public string StorageFolder { get; set; } = "storage";
		
		public bool DisableFileCaching { get; set; }
		
		public List<(Regex, string)> Redirects { get; }
		
		public string AccessControlAllowOrigin { get; set; }
		public bool AccessControlAllowCredentials { get; set; }
		
		public bool EnableWaterfallingIndex { get; set; }

		public Server(params string[] prefixes) {
			WebSocketContexts = new List<HttpListenerWebSocketContext>();
			
			HttpListener = new HttpListener();
			Actions = GetActions();
			MultipartActions = GetMultipartActions();
			Tasks = GetTasks();

			foreach (var prefix in prefixes) 
				HttpListener.Prefixes.Add(prefix);

			Redirects = new List<(Regex, string)>();
		}

		public void Start() {
			foreach (var task in Tasks) task.Start();
			
			HttpListener.Start();
			HttpListener.BeginGetContext(OnConnection, null);
		}

		public void Stop() {
			foreach (var task in Tasks) task.Stop();
			
			HttpListener.Stop();
		}

		private static Dictionary<string, Action> GetActions() {
			var dict = new Dictionary<string, Action>();

			var assembly = Assembly.GetEntryAssembly();
			if (assembly == null) return dict;
			
			foreach (var (name, action) in assembly.GetTypes()
				.Where(t => t.BaseType == typeof(Action))
				.Select(t => Activator.CreateInstance(t) as Action)
				.Where(a => a != null)
				.Select(a => (a.Name, a))) 
				dict.Add(name, action);

			return dict;
		}

		private List<Abstractions.Task> GetTasks() {
			var assembly = Assembly.GetEntryAssembly();
			if (assembly == null) return new List<Abstractions.Task>();;

			var result = new List<Abstractions.Task>(assembly.GetTypes()
				.Where(t => t.BaseType == typeof(Abstractions.Task))
				.Select(t => Activator.CreateInstance(t) as Abstractions.Task)
				.Where(t => t != null)).ToList();
			
			foreach (var t in result) t.Server = this;
			
			return result;
		}

		private Dictionary<string, MultipartAction> GetMultipartActions() {
			var dict = new Dictionary<string, MultipartAction>();

			var assembly = Assembly.GetEntryAssembly();
			if (assembly == null) return dict;
			
			foreach (var (name, action) in assembly.GetTypes()
				.Where(t => t.BaseType == typeof(MultipartAction))
				.Select(t => Activator.CreateInstance(t) as MultipartAction)
				.Where(a => a != null)
				.Select(a => (a.Name, a))) 
				dict.Add(name, action);

			return dict;
		}

		private async void OnConnection(IAsyncResult result) {
			HttpListener.BeginGetContext(OnConnection, null);

			HttpListenerContext context = null;

			try {
				context = HttpListener.EndGetContext(result);

				if (context.Request.IsWebSocketRequest) {
					OnWebSocketConnection(context);
				} else {
					switch (context.Request.HttpMethod) {
						case "GET": await OnGET(context); break;
						case "POST":
							if (!string.IsNullOrWhiteSpace(AccessControlAllowOrigin))
								context.Response.AddHeader("Access-Control-Allow-Origin", AccessControlAllowOrigin);
							context.Response.AddHeader("Access-Control-Allow-Credentials",
								AccessControlAllowCredentials.ToString().ToLower());
							await OnPOST(context); break;
						
						case "OPTIONS":
							if (!string.IsNullOrWhiteSpace(AccessControlAllowOrigin))
								context.Response.AddHeader("Access-Control-Allow-Origin", AccessControlAllowOrigin);
							context.Response.AddHeader("Access-Control-Allow-Credentials",
								AccessControlAllowCredentials.ToString().ToLower());
							break;
						default:
							context.Response.StatusCode = 405;
							break;
					}

					context.Response.Close();
				}
			} catch (Exception ex) {
				var answer = new Json.JsonError(ex is JsonableException jsonableException
					? jsonableException
					: ex is ArgumentException argumentException && argumentException.Message.Contains("JSON")
						? new Ex01_WrongJson()
						: new Ex05_Unexpected(ex is TargetInvocationException tie ? tie.InnerException : ex));
				try {
					await Answer.Json(context, answer);
				} catch { /**/ }
			}
		}

		private async Task OnPOST(HttpListenerContext context) {
			if (OnPostVerificationRequired?.GetInvocationList().Select(d => d.DynamicInvoke(context))
				.Where(o => o != null).Select(o => (bool) o).Any(b => b!) ?? false) {
				OnPostVerificationFailed?.Invoke(context);
				return;
			}

			if (EnableCustomPostHander) {
				CustomPostHandler?.Invoke(context);
				return;
			}

			if (context.Request.ContentType?.Contains("multipart/form-data") ?? false) {
				var multipart = await MultipartFormDataParser.ParseAsync(context.Request.InputStream);
				if (!multipart.HasParameter("action")) throw new Ex02_FieldNotFound("action");
				
				if (!MultipartActions.TryGetValue(multipart.GetParameterValue("action"), out var multipartAction))
					throw new Ex04_ActionNotFound();
				
				multipartAction.ValidateRequest(this, context, multipart);
				multipartAction.DoWork(this, context, multipart);
			}
			
			JsonObject json;
			using (var sr = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding)) 
				json = JsonValue.Parse(await sr.ReadToEndAsync()) as JsonObject;

			if (json == null) throw new Ex01_WrongJson();
            
			Json.CheckFields(json, "action");
			
			Actions.TryGetValue(json["action"], out var action);
			if (action == null) throw new Ex04_ActionNotFound();
            
			action.ValidateJson(this, context, json);
			await Answer.Json(context, await action.DoWork(this, context, json));
		}

		private async Task OnGET(HttpListenerContext context) {
			var url = context.Request.Url?.AbsolutePath;
			
			if (string.IsNullOrEmpty(url)) return;
			
			var redirect = Redirects.FirstOrDefault(r => r.Item1.IsMatch(url));
			if (redirect != default) {
				context.Response.Redirect(redirect.Item1.Replace(url, redirect.Item2));
				return;
			}
			
			if (OnGetVerificationRequired?.GetInvocationList().Select(d => d.DynamicInvoke(context))
				.Where(o => o != null).Select(o => (bool) o).Any(b => !b) ?? false) {
				OnGetVerificationFailed?.Invoke(context);
				return;
			}

			if (EnableCustomGetHander) {
				CustomGetHandler.Invoke(context);
				return;
			}
			
			var storage = Directory.CreateDirectory(StorageFolder);

			if (url.EndsWith("/")) url += "index.html";
			
			if (url.Contains("..")) {
				context.Response.StatusCode = 403;
				return;
			}

			var path = Path.Join(storage.FullName, url);
			if (Directory.Exists(path)) {
				context.Response.Redirect(url + '/' + context.Request.Url.Query);
				return;
			}

			var file = new FileInfo(path);

			if (EnableWaterfallingIndex)
				while (!file.Exists && file.Directory?.Parent != null && file.Directory?.FullName != storage.FullName)
					file = new FileInfo(Path.Join(file.Directory.Parent.FullName, file.Name));

			await Answer.FileAsync(context, file, DisableFileCaching);
		}
		
		private static readonly WebSocketState[] StatesForRemoving =
			{WebSocketState.Closed, WebSocketState.CloseReceived, WebSocketState.CloseSent, WebSocketState.Aborted};

		public void SendMessageToAll(JsonValue val) {
			lock (WebSocketContexts) {
				WebSocketContexts.RemoveAll(c => StatesForRemoving.Contains(c.WebSocket.State));

				foreach (var context in WebSocketContexts) 
					context.WebSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(val.ToString())),
						WebSocketMessageType.Text, true, CancellationToken.None);
			}
		}

		private void OnWebSocketConnection(HttpListenerContext context) {
			if (OnWebSocketVerificationRequired?.GetInvocationList().Select(d => d.DynamicInvoke(context))
				.Where(o => o != null).Select(o => (bool) o).Any(b => b!) ?? false) {
				OnWebSocketVerificationFailed?.Invoke(context);
				return;
			}
			
			context.AcceptWebSocketAsync(null, TimeSpan.FromSeconds(5)).ContinueWith(t => {
				if (t.IsCompleted) lock (WebSocketContexts) WebSocketContexts.Add(t.Result);
				OnWebSocketConnected?.Invoke(t.Result);
			});
		}
	}
}