using System.IO;
using System.Json;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.StaticFiles;

namespace Savok.Server.Utils {
    public static class Answer {
        public static async Task Json(HttpListenerContext context, JsonValue json) {
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.ContentType = "application/json";
            await using var sw = new StreamWriter(context.Response.OutputStream, Encoding.UTF8);
            await sw.WriteAsync(json.ToString());
        }

        public static async Task FileAsync(HttpListenerContext context, FileInfo file, bool withoutCaching) {
            if (file.Exists) {
                if (!new FileExtensionContentTypeProvider().TryGetContentType(file.FullName, out var contentType))
                    contentType = "application/octet-stream";

                if (withoutCaching) {
                    context.Response.ContentType = contentType;
                    await using var os = context.Response.OutputStream;
                    await using var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				
                    await fs.CopyToAsync(os);
                    return;
                }
                
                var remoteETag = context.Request.Headers.Get("If-None-Match");

                if (FileCacheManager.TryToGet(context, file, out var hash) && remoteETag != null && remoteETag == hash) {
                    context.Response.StatusCode = 304;
                } else {
                    context.Response.AddHeader("ETag", hash);
				
                    context.Response.ContentType = contentType;
                    await using var os = context.Response.OutputStream;
                    await using var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				
                    await fs.CopyToAsync(os);
                }
            } else context.Response.StatusCode = 404;
        }
    }
}