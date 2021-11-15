using System;
using System.Collections.Generic;
using System.Json;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Savok.Server.Utils {
	public class IdempotenceManager {
		private List<IdemtopenceItem> IdemtopenceItems { get; }

		private static IdempotenceManager Instance { get; set; }
		
		private IdempotenceManager() {
			IdemtopenceItems = new List<IdemtopenceItem>();
		}

		public async Task<bool> CheckIdempotenceAsync(Server server, HttpListenerContext context) {
			var idempotenceGuid = GetIdempotenceValueFromHeaders(server, context);
			if (idempotenceGuid is null) return false;
			
			var idempotenceItem = GetByGuid(idempotenceGuid.Value);
			if (idempotenceItem is null) return false;
			
			await Answer.Json(context, idempotenceItem.Answer);
			return true;
		}

		public void StoreAnswer(Server server, HttpListenerContext context, JsonValue answer) {
			var idempotenceGuid = GetIdempotenceValueFromHeaders(server, context);
			if (idempotenceGuid is not null) Store(idempotenceGuid.Value, answer);
		}

		public Guid? GetIdempotenceValueFromHeaders(Server server, HttpListenerContext context) {
			var idempotenceHeaderValue = context.Request.Headers[server.IdempotenceKeyHeaderName];
			if (!string.IsNullOrWhiteSpace(idempotenceHeaderValue) &&
			    Guid.TryParse(idempotenceHeaderValue, out var idempotenceGuid))
				return idempotenceGuid;
			
			return null;
		}

		public IdemtopenceItem GetByGuid(Guid guid) {
			lock (IdemtopenceItems) {
				IdemtopenceItems.RemoveAll(i => i.LastUntil < DateTimeOffset.Now);

				return IdemtopenceItems.FirstOrDefault(i => i.Guid == guid);
			}
		}

		public void Store(Guid guid, JsonValue answer) {
			lock (IdemtopenceItems) {
				IdemtopenceItems.RemoveAll(i => i.LastUntil < DateTimeOffset.Now);

				var existingItem = GetByGuid(guid);
				if (existingItem is not null) {
					existingItem.Answer = answer;
					existingItem.LastUntil = DateTimeOffset.Now;
				} else {
					IdemtopenceItems.Add(new IdemtopenceItem {
						Guid = guid,
						Answer = answer,
						LastUntil = DateTimeOffset.Now.AddMinutes(5)
					});
				}
			}
		}

		public static IdempotenceManager GetInstance() => Instance ??= new IdempotenceManager();

		public class IdemtopenceItem {
			public Guid Guid { get; init; }
			public JsonValue Answer { get; set; }
			public DateTimeOffset LastUntil { get; set; }
		}
	}
}