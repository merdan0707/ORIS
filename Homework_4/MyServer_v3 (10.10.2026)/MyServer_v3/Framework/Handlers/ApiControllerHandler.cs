using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using MyServer_v3.Framework.Attributes;

namespace MyServer_v3.Framework.Handlers;

public class ApiControllerHandler : RequestHandler
{
    private Dictionary<string, EndpointMetadata> _routes;

    public ApiControllerHandler()
    {
        _routes = new Dictionary<string, EndpointMetadata>(StringComparer.OrdinalIgnoreCase);
        ScanAssembly();
    }

    private void ScanAssembly()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var controllerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && Attribute.IsDefined(t, typeof(HttpControllerAttribute)));

        foreach (var controllerType in controllerTypes)
        {
            var controllerName = controllerType.Name;
            if (controllerName.EndsWith("Controller"))
                controllerName = controllerName[..^10]; //убираем последние 10 символов
            controllerName = controllerName.ToLowerInvariant();

            var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => Attribute.IsDefined(m, typeof(HttpGetAttribute)) || 
                            Attribute.IsDefined(m, typeof(HttpPostAttribute)));

            foreach (var method in methods)
            {
                var httpMethod = Attribute.IsDefined(method, typeof(HttpGetAttribute))
                    ? "GET"
                    : "POST";

                var methodName = method.Name.ToLowerInvariant();
                var routeKey = $"api/{controllerName}/{methodName}";

                _routes[routeKey] = new EndpointMetadata(controllerType, method, httpMethod);
                Console.WriteLine($"[API] Зарегистрирован путь: {routeKey} ({httpMethod})");
            }
        }
    }

        protected override async Task<bool> HandleCoreAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        var segments = request.Url.Segments; 
        // segments будет: ["/", "api/", "auth/", "getuser/", "42/"]
        if (segments.Length < 4)
            return false;
        
        var controllerPart = segments[2].TrimEnd('/').ToLowerInvariant(); // "auth"
        var methodPart = segments[3].TrimEnd('/').ToLowerInvariant();     // "getuser"
        var routeKey = $"api/{controllerPart}/{methodPart}";              // "api/auth/getuser"
        
        if (!_routes.TryGetValue(routeKey, out var endpoint))
            return false;
        
        if (endpoint.HttpMethod != request.HttpMethod)
        {
            response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
            await WriteResponseAsync(response, "Method Not Allowed");
            return true;
        }

        try
        {
            object[] parameters;

            if (request.HttpMethod == "GET")
            {
                parameters = ExtractParametersFromUrl(segments, endpoint.Method);
            }
            else // POST
            {
                parameters = await ExtractParametersFromBodyAsync(request, endpoint.Method);
            }

            var controllerInstance = Activator.CreateInstance(endpoint.ControllerType);
            var result = endpoint.Method.Invoke(controllerInstance, parameters);

            if (result is Task task)
            {
                await task;
                var resultProperty = task.GetType().GetProperty("Result");
                result = resultProperty?.GetValue(task);
            }

            var json = JsonSerializer.Serialize(result);
            await WriteJsonResponseAsync(response, json);

            return true;
        }
        catch (Exception ex)
        {
            response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await WriteResponseAsync(response, $"Error: {ex.Message}");
            return true;
        }
    }

    private async Task<object[]> ExtractParametersFromBodyAsync(HttpListenerRequest request, MethodInfo method)
    {
        var methodParams = method.GetParameters();
        var parameters = new object[methodParams.Length];

        using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
        var body = await reader.ReadToEndAsync();
        
        // парсим form-urlencoded: "email=user@mail.com&password=123"
        var formData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in body.Split('&'))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2)
            {
                var key = Uri.UnescapeDataString(parts[0]);
                var value = Uri.UnescapeDataString(parts[1]);
                formData[key] = value;
            }
        }

        for (int i = 0; i < methodParams.Length; i++)
        {
            var paramName = methodParams[i].Name;

            if (formData.TryGetValue(paramName, out var value))
            {
                parameters[i] = Convert.ChangeType(value, methodParams[i].ParameterType);
            }
            else if (methodParams[i].HasDefaultValue)
            {
                parameters[i] = methodParams[i].DefaultValue!;
            }
            else
            {
                throw new ArgumentException($"Пропушен параметр: {paramName}");
            }
        }

        return parameters;
    }

    private object[] ExtractParametersFromUrl(string[] segments, MethodInfo method)
    {
        var methodParams = method.GetParameters();
        var parameters = new object[methodParams.Length];
        
        // сегменты URL: [1: "/", 2: "api/", 3: "auth/", 4: "login/", "param1/", "param2/"]
        var paramSegments = segments.Skip(4).ToArray();

        for (int i = 0; i < methodParams.Length; i++)
        {
            if (i < paramSegments.Length)
            {
                var value = paramSegments[i].TrimEnd('/');
                value = Uri.UnescapeDataString(value);

                parameters[i] = Convert.ChangeType(value, methodParams[i].ParameterType);
            }
            else
            {
                if (methodParams[i].HasDefaultValue)
                    parameters[i] = methodParams[i].DefaultValue!;
                else
                    throw new ArgumentException($"Пропушен параметр: {methodParams[i].Name}");
            }
        }

        return parameters;
    }

    private static async Task WriteJsonResponseAsync(HttpListenerResponse response, string json)
    {
        response.ContentType = "application/json; charset=utf-8";
        response.StatusCode = (int)HttpStatusCode.OK;

        var buffer = Encoding.UTF8.GetBytes(json);
        response.ContentLength64 = buffer.Length;

        await using var output = response.OutputStream;
        await output.WriteAsync(buffer);
        await output.FlushAsync();
    }
    
    private static async Task WriteResponseAsync(HttpListenerResponse response, string content)
    {
        response.ContentType = "text/plain; charset=utf-8";

        var buffer = Encoding.UTF8.GetBytes(content);
        response.ContentLength64 = buffer.Length;

        await using var output = response.OutputStream;
        await output.WriteAsync(buffer);
        await output.FlushAsync();
    }

    private record EndpointMetadata(Type ControllerType, MethodInfo Method, string HttpMethod);
}