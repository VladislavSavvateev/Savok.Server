using System;
using System.Collections.Generic;
using System.Json;
using System.Linq;
using Savok.Server.Abstractions;
using Savok.Server.Exceptions;

namespace Savok.Server.Utils {
    public class Json {
        public static readonly Dictionary<Type, JsonType> AtomicTypes = new() {
            [typeof(string)] = JsonType.String,
            [typeof(int)] = JsonType.Number,
            [typeof(decimal)] = JsonType.Number,
            [typeof(JsonObject)] = JsonType.Object,
            [typeof(bool)] = JsonType.Boolean,
            [typeof(JsonArray)] = JsonType.Array
        };
        
        public static void CheckFields(JsonObject json, params string[] fields) {
            var missedFields = fields.Where(field => !json.ContainsKey(field)).ToList();
            if (missedFields.Count != 0) throw new Ex02_FieldNotFound(missedFields.ToArray());
        }

        public static void CheckTypes(JsonObject json, string[] fields, JsonType[] types) {
            var fieldsWithInvalidValue = new List<string>();
            for (var i = 0; i < Math.Min(fields.Length, types.Length); i++)
                if (json.ContainsKey(fields[i]) && json[fields[i]].JsonType != types[i])
                    fieldsWithInvalidValue.Add(fields[i]);
            if (fieldsWithInvalidValue.Count != 0) throw new Ex03_InvalidValue(fieldsWithInvalidValue.ToArray());
        }
        
        public abstract class JsonAnswer : JsonObject {}

        public class JsonOk : JsonAnswer { public JsonOk() => this["status"] = true; }

        public class JsonError : JsonAnswer {
            public JsonError(JsonableException ex) {
                this["status"] = false;
                this["error"] = ex.ToJson();
            }
        }
    }
}