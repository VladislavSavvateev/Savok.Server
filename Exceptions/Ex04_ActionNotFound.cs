using System.Json;
using Savok.Server.Abstractions;

namespace Savok.Server.Exceptions {
    public class Ex04_ActionNotFound : JsonableException {
        protected override int Code => 4;
        public override string Message => "Action not found.";
        protected override JsonObject Details => null;
    }
}