using System;
using System.Json;

namespace Savok.Server.Abstractions {
    public abstract class JsonableException : Exception {
        protected abstract int Code { get; }
        public abstract override string Message { get; }
        protected abstract JsonObject Details { get; }

        public JsonObject ToJson()
            => new() {
                ["code"] = Code,
                ["message"] = Message,
                ["details"] = Details
            };
    }
}