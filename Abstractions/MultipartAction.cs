using System.Json;
using System.Linq;
using System.Net;
using HttpMultipartParser;
using Savok.Server.Exceptions;
using Savok.Server.Utils;

namespace Savok.Server.Abstractions {
    public abstract class MultipartAction {
        public abstract string Name { get; }
        public abstract string[] Fields { get; }

        public virtual void ValidateRequest(Server server, HttpListenerContext context, MultipartFormDataParser multipart) {
            if (Fields == null) return;

            var listOfFields = Fields.Where(field => !multipart.HasParameter(field)).ToList();
            if (listOfFields.Count > 0) throw new Ex02_FieldNotFound(listOfFields.ToArray());
        }
        public abstract System.Threading.Tasks.Task DoWork(Server server, HttpListenerContext context, MultipartFormDataParser multipart);
    }
}