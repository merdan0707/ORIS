namespace MyServer_v4.Framework.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RouteAttribute : Attribute
{
    public string Template { get; }
    public RouteAttribute(string template) => Template = template;
}