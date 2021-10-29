using System;

namespace Savok.Server.Attributes {
	[AttributeUsage(AttributeTargets.Class)]
	public class CustomCorsOriginAttribute : Attribute {
		public string Origin { get; }
		
		public CustomCorsOriginAttribute(string origin) { Origin = origin; }
	}
}