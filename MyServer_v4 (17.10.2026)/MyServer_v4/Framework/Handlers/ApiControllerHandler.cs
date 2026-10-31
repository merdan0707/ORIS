using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Web;
using MyServer_v4.Framework.Attributes;

namespace MyServer_v4.Framework.Handlers;

public class ApiControllerHandler : RequestHandler
{
    private readonly Dictionary<string, EndPointMetadata> _routes;

    public ApiControllerHandler()
    {
        _routes = new Dictionary<string, EndPointMetadata>(StringComparer.OrdinalIgnoreCase);
        ScanAssembly();
    }

    private void ScanAssembly()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var controllers = assembly.GetTypes()
            .Where(t => t.IsClass && Attribute.IsDefined(t, typeof(HttpControllerAttribute)));

        foreach (var type in controllers)
        {
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
                var httpMethod = Attribute.IsDefined(method, typeof(HttpGetAttribute)) 
                    ? "GET" : "POST";
                var fullRouteKey = $"api/{controllerPath}/{methodPath}".Trim('/');

                var paramInstructions = AnalyzeParameters(method);

                _routes[fullRouteKey] = new EndPointMetadata(type, method, httpMethod, paramInstructions);
                Console.WriteLine($"[API] Registered: {httpMethod} /{fullRouteKey}");
            }
        }
    }
    
    /// <summary>
    /// Анализирует параметры метода и определяет, откуда брать значения (Query, Form, Body)
    /// </summary>
    private static List<ParameterInstuction> AnalyzeParameters(MethodInfo method)
    {
        var instructions = new List<ParameterInstuction>();
        foreach (var param in method.GetParameters())
        {
            var source = ParameterSource.Query;

            if (Attribute.IsDefined(param, typeof(FromQueryAttribute))) source = ParameterSource.Query;
            else if (Attribute.IsDefined(param, typeof(FromFormAttribute))) source = ParameterSource.Form;
            else if (Attribute.IsDefined(param, typeof(FromBodyAttribute))) source = ParameterSource.Body;
            else if (method.GetCustomAttribute<HttpPostAttribute>() != null) source = ParameterSource.Form;
            
            instructions.Add(new ParameterInstuction(param.Name!, param.ParameterType, source, param.HasDefaultValue, param.DefaultValue));
        }

        return instructions;
    }

    protected override async Task<bool> HandleCoreAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        // убираем "api/" из начала пути для поиска в словаре
        var path = request.Url.LocalPath.TrimStart('/');
        if (!path.StartsWith("api/", StringComparison.OrdinalIgnoreCase))
            return false;
        
//--------------------- НУЖНА ПРОВЕРКА!!!!!
        //var routeKey = path.Substring(4); // "api/auth/login" -> "auth/login"
        if (!_routes.TryGetValue(path, out var endpoint))
            return false;

        if (endpoint.HttpMethod != request.HttpMethod)
        {
            response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
            await WriteTextResponseAsync(response, 
                $"Method Not Allowed. Expected {endpoint.HttpMethod}");
            return true;
        }

        try
        {
            //извлекаем параметры на основе заранее составленной инструкции
            var parameteres = await ExtractParametersAsync(request, endpoint.ParameterInstuctions);

            //Создаем и вызываем контроллер
            var instance = Activator.CreateInstance(endpoint.ControllerType);
            var result = endpoint.Method.Invoke(instance, parameteres);

            if (result is Task task)
            {
                await task;
                result = task.GetType().GetProperty("Result")?.GetValue(task);
            }

            await WriteJsonResponseAsync(response, result);
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private async Task<object[]> ExtractParametersAsync(HttpListenerRequest request, List<ParameterInstuction> instuctions)
    {
        var parameteres = new object[instuctions.Count];
        var formData = await GetFormDataAsync(request); // Кэшируем форму, если она есть

        for (int i = 0; i < instuctions.Count; i++)
        {
            var instr = instuctions[i];
            string? rawValue = null;
            
            switch (instr.Source)
            {
                case ParameterSource.Query:
                    rawValue = request.QueryString[instr.Name];
                    break;
                case ParameterSource.Form:
                    formData.TryGetValue(instr.Name, out rawValue);
                    break;
                case ParameterSource.Body:
                {
                    using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                    rawValue = await reader.ReadToEndAsync();
                    break;
                }
            }

            if (rawValue != null)
                parameteres[i] = Convert.ChangeType(rawValue, instr.Type);
            else if (instr.HasDefaultValue)
                parameteres[i] = instr.DefaultValue!;
            else
                throw new ArgumentException($"Missing required parameter: '{instr.Name}' from {instr.Source}");
        }

        return parameteres;
    }

    // лениво читает и парсит тело запроса как форму (x-www-form-urlencoded)
    private async Task<Dictionary<string, string>> GetFormDataAsync(HttpListenerRequest request)
    {
        var formData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (request.HttpMethod != "POST" || request.HttpMethod == null ||
            !request.ContentType.Contains("application/x-www-form-urlencoded"))
            return formData;

        using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
        var body = await reader.ReadToEndAsync();

        foreach (var pair in body.Split('&'))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2)
                formData[Uri.UnescapeDataString(parts[0])] = Uri.UnescapeDataString(parts[1]);
        }

        return formData;
    }

    private static async Task WriteJsonResponseAsync(HttpListenerResponse response, object? data)
    {
        response.ContentType = "application/json; charset=utf-8";
        response.StatusCode = (int)HttpStatusCode.OK;
        var json = data != null ? JsonSerializer.Serialize(data) : "null";
        var buffer = Encoding.UTF8.GetBytes(json);
        response.ContentLength64 = buffer.Length;
        await using var output = response.OutputStream;
        await output.WriteAsync(buffer);
    }

    private static async Task WriteTextResponseAsync(HttpListenerResponse response, string content)
    {
        response.ContentType = "text/plain; charset=utf-8";
        var buffer = Encoding.UTF8.GetBytes(content);
        response.ContentLength64 = buffer.Length;
        await using var output = response.OutputStream;
        await output.WriteAsync(buffer);
    }
    private record EndPointMetadata(
        Type ControllerType,
        MethodInfo Method,
        string HttpMethod,
        List<ParameterInstuction> ParameterInstuctions);

    private enum ParameterSource
    {
        Query,
        Form,
        Body
    }

    private record ParameterInstuction(
        string Name,
        Type Type,
        ParameterSource Source,
        bool HasDefaultValue,
        object? DefaultValue);

}