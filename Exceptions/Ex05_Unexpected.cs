using System;
using System.Json;
using Savok.Server.Abstractions;

namespace Savok.Server.Exceptions {
    public class Ex05_Unexpected : JsonableException {
        protected override int Code => 5;
        public override string Message => "Unexpected exception was occurred";
        protected override JsonObject Details
            => new() {
                ["message"] = Exception.Message,
                ["stackTrace"] = Exception.StackTrace,
                ["name"] = Exception.GetType().Name
            };
        
        private Exception Exception { get; }

        public Ex05_Unexpected(Exception ex) => Exception = ex;
    }
}