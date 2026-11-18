using System.Reflection;
using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Controllers;

namespace MiniHttpServer.Framework.Routing;

public class RouteScanner
{
    public Dictionary<string, EndPointMetadata> Scan(Assembly assembly)
    {
        var routes = new Dictionary<string, EndPointMetadata>(StringComparer.OrdinalIgnoreCase);
        
        var controllers = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && Attribute.IsDefined(t, typeof(HttpControllerAttribute)));

        foreach (var type in controllers)
        {
            if (!typeof(BaseController).IsAssignableFrom(type))
                throw new InvalidOperationException($"Контроллер {type.Name} должен наследоваться от BaseController.");

            var controllerRoute = Attribute.GetCustomAttribute(type, typeof(RouteAttribute)) as RouteAttribute;
            var controllerPath = controllerRoute?.Template?.Trim('/')
                ?? type.Name.Replace("Controller", "").ToLowerInvariant();

            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => Attribute.IsDefined(m, typeof(HttpGetAttribute))
                         || Attribute.IsDefined(m, typeof(HttpPostAttribute)));

            foreach (var method in methods)
            {
                var methodRoute = Attribute.GetCustomAttribute(method, typeof(RouteAttribute)) as RouteAttribute;
                var methodPath = methodRoute?.Template?.Trim('/')
                    ?? method.Name.ToLowerInvariant();
                var httpMethod = Attribute.IsDefined(method, typeof(HttpGetAttribute)) ? "GET" : "POST";
                var fullRouteKey = $"api/{controllerPath}/{methodPath}".Trim('/');

                routes[fullRouteKey] = new EndPointMetadata(type, method, httpMethod, AnalyzeParameters(method));
                Console.WriteLine($"[API] Registered: {httpMethod} /{fullRouteKey}");
            }
        }

        return routes;
    }

    private static List<ParameterInstruction> AnalyzeParameters(MethodInfo method)
    {
        var instructions = new List<ParameterInstruction>();
        foreach (var param in method.GetParameters())
        {
            var source = ParameterSource.Query;
            if (Attribute.IsDefined(param, typeof(FromQueryAttribute))) source = ParameterSource.Query;
            else if (Attribute.IsDefined(param, typeof(FromFormAttribute))) source = ParameterSource.Form;
            else if (Attribute.IsDefined(param, typeof(FromBodyAttribute))) source = ParameterSource.Body;
            else if (method.GetCustomAttribute<HttpPostAttribute>() != null) source = ParameterSource.Form;

            instructions.Add(new ParameterInstruction(
                param.Name!,
                param.ParameterType,
                source,
                param.HasDefaultValue,
                param.DefaultValue));
        }
        return instructions;
    }
}