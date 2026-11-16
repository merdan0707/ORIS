namespace MiniHttpServer.Framework.Attributes;

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class FromQueryAttribute : Attribute
{ }