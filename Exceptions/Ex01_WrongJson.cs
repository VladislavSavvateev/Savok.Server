using System.Json;
using Savok.Server.Abstractions;

namespace Savok.Server.Exceptions {
    public class Ex01_WrongJson : JsonableException {
        protected override int Code => 1;
        public override string Message => "Wrong JSON";
        protected override JsonObject Details => null;
    }
}