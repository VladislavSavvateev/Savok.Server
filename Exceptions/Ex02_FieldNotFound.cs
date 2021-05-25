using System.Json;
using System.Linq;
using Savok.Server.Abstractions;

namespace Savok.Server.Exceptions {
    public class Ex02_FieldNotFound : JsonableException {
        protected override int Code => 2;
        public override string Message => "Field not found.";
        protected override JsonObject Details => new() {["fields"] = new JsonArray(Fields.Select(f => (JsonValue) f))};
        
        private string[] Fields { get; }

        public Ex02_FieldNotFound(params string[] fields) => Fields = fields;
    }
}