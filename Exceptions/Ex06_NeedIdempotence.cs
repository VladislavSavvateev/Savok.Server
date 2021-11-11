using System.Json;
using Savok.Server.Abstractions;

namespace Savok.Server.Exceptions {
	public class Ex06_NeedIdempotence : JsonableException {
		protected override int Code => 6;
		public override string Message => "This action needs idempotence";
		protected override JsonObject Details => null;
	}
}