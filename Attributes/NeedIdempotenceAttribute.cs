using System;

namespace Savok.Server.Attributes {
	[AttributeUsage(AttributeTargets.Class)]
	public class NeedIdempotenceAttribute : Attribute { }
}