using System.Json;
using System.Linq;
using Savok.Server.Abstractions;

namespace Savok.Server.Exceptions {
    public class Ex03_InvalidValue : JsonableException {
        protected override int Code => 3;
        public override string Message => "Invalid value.";
        protected override JsonObject Details => new() {["fields"] = new JsonArray(Fields.Select(f => (JsonValue) f))};
        
        private string[] Fields { get; }

        public Ex03_InvalidValue(params string[] fields) => Fields = fields;
    }
}