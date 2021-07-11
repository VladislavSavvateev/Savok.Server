using System.Json;
using System.Net;
using System.Threading.Tasks;
using Savok.Server.Utils;

namespace Savok.Server.Abstractions {
    public abstract class Action {
        public abstract string Name { get; }
        public abstract string[] Fields { get; }
        public abstract JsonType[] Types { get; }

        public virtual void ValidateJson(Server server, HttpListenerContext context, JsonObject json) {
            if (Fields == null) return;
            
            Json.CheckFields(json, Fields);
            if (Types != null) Json.CheckTypes(json, Fields, Types);
        }
        public abstract Task<Json.JsonAnswer> DoWork(Server server, HttpListenerContext context, JsonObject json);
    }
}