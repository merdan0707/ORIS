using System.Net;
using System.Reflection;
using Controlwork_1.Attributes;
using Controlwork_1.Exeptions;
using Controlwork_1.Handlers.Abstractions;
using Controlwork_1.HttpResults;
using Controlwork_1.HttpResults.Abstractions;

namespace Controlwork_1.Handlers;

public class ApiControllerHandler : RequestHandler
{
    private readonly Dictionary<string, EndPointMetadata> _routes =
        new(StringComparer.OrdinalIgnoreCase);

    public ApiControllerHandler() => ScanAssembly();

    private void ScanAssembly()
    {
        var controllers = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsClass && Attribute.IsDefined(t, typeof(HttpControllerAttribute)));

        foreach (var type in controllers)
        {
            var controllerTemplate = type.GetCustomAttribute<RouteAttribute>()?.Template?.Trim('/')
                                     ?? type.Name.Replace("Controller", "").ToLowerInvariant();

            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => Attribute.IsDefined(m, typeof(HttpGetAttribute))
                            || Attribute.IsDefined(m, typeof(HttpPostAttribute))
                            || Attribute.IsDefined(m, typeof(HttpPutAttribute)));

            foreach (var method in methods)
            {
                var methodTemplate = method.GetCustomAttribute<RouteAttribute>()?.Template?.Trim('/')
                                     ?? method.Name.ToLowerInvariant();

                var httpMethod =
                    Attribute.IsDefined(method, typeof(HttpGetAttribute)) ? "GET" :
                    Attribute.IsDefined(method, typeof(HttpPutAttribute)) ? "PUT" : "POST";

                var fullRoute = methodTemplate.Contains('/')
                    ? methodTemplate
                    : $"{controllerTemplate}/{methodTemplate}".Trim('/');

                _routes[fullRoute] = new EndPointMetadata(type, method, httpMethod, AnalyzeParameters(method));
                Console.WriteLine($"[API] {httpMethod} /{fullRoute}");
            }
        }
    }

    private static List<ParameterInstruction> AnalyzeParameters(MethodInfo method)
    {
        var isPost = method.GetCustomAttribute<HttpPostAttribute>() != null;
        var isPut  = method.GetCustomAttribute<HttpPutAttribute>()  != null;
        var defaultSource = (isPost || isPut) ? ParameterSource.Form : ParameterSource.Query;

        return method.GetParameters()
            .Select(p =>
            {
                var source =
                    Attribute.IsDefined(p, typeof(FromQueryAttribute)) ? ParameterSource.Query :
                    Attribute.IsDefined(p, typeof(FromFormAttribute))  ? ParameterSource.Form  :
                    defaultSource;

                return new ParameterInstruction(p.Name!, p.ParameterType, source, p.HasDefaultValue, p.DefaultValue);
            })
            .ToList();
    }

    protected override async Task<bool> HandleCoreAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        var path = request.Url!.LocalPath.TrimStart('/');
        if (!path.StartsWith("api/", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!_routes.TryGetValue(path, out var endpoint))
            return false;

        if (endpoint.HttpMethod != request.HttpMethod)
        {
            await new ErrorResult((int)HttpStatusCode.MethodNotAllowed,
                $"Method Not Allowed. Expected {endpoint.HttpMethod}").ExecuteAsync(context);
            return true;
        }

        try
        {
            var parameters = await ExtractParametersAsync(request, endpoint.Instructions);

            var instance = Activator.CreateInstance(endpoint.ControllerType);
            var result = endpoint.Method.Invoke(instance, parameters);

            if (result is Task task)
            {
                await task;
                result = task.GetType().GetProperty("Result")?.GetValue(task);
            }

            if (result is IActionResult actionResult)
                await actionResult.ExecuteAsync(context);
            else
                await new JsonResult(result).ExecuteAsync(context);

            return true;
        }
        // Оборачиваемое исключение из контроллера
        catch (TargetInvocationException tie) when (tie.InnerException != null)
        {
            await WriteErrorForAsync(context, tie.InnerException);
            return true;
        }
        // Прямое исключение из ExtractParametersAsync (до Invoke)
        catch (ArgumentException argEx)
        {
            await new ErrorResult((int)HttpStatusCode.BadRequest, argEx.Message).ExecuteAsync(context);
            return true;
        }
    }

    private static Task WriteErrorForAsync(HttpListenerContext context, Exception inner)
    {
        var status = inner switch
        {
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            ConflictException    => (int)HttpStatusCode.Conflict,
            ArgumentException    => (int)HttpStatusCode.BadRequest,
            _                    => (int)HttpStatusCode.InternalServerError
        };
        return new ErrorResult(status, inner.Message).ExecuteAsync(context);
    }

    private static async Task<object?[]> ExtractParametersAsync(
        HttpListenerRequest request, List<ParameterInstruction> instructions)
    {
        var form = await ReadFormAsync(request);
        var parameters = new object?[instructions.Count];

        for (var i = 0; i < instructions.Count; i++)
        {
            var instr = instructions[i];
            var rawValue = instr.Source switch
            {
                ParameterSource.Query => request.QueryString[instr.Name],
                ParameterSource.Form  => form.GetValueOrDefault(instr.Name),
                _ => null
            };

            if (rawValue != null)
            {
                try
                {
                    parameters[i] = ConvertValue(rawValue, instr.Type);
                }
                catch
                {
                    throw new ArgumentException($"Некорректное значение параметра '{instr.Name}'.");
                }
            }
            else if (instr.HasDefaultValue)
            {
                parameters[i] = instr.DefaultValue;
            }
            else
            {
                throw new ArgumentException($"Отсутствует обязательный параметр '{instr.Name}' ({instr.Source}).");
            }
        }

        return parameters;
    }

    private static object ConvertValue(string rawValue, Type targetType)
    {
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
        return underlying.IsEnum
            ? Enum.Parse(underlying, rawValue, ignoreCase: true)
            : Convert.ChangeType(rawValue, underlying);
    }

    private static async Task<Dictionary<string, string>> ReadFormAsync(HttpListenerRequest request)
    {
        var form = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (request.HttpMethod is not ("POST" or "PUT"))
            return form;

        if (request.ContentType is null ||
            !request.ContentType.Contains("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
            return form;

        using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
        var body = await reader.ReadToEndAsync();

        foreach (var pair in body.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0].Replace('+', ' '));
            var val = parts.Length == 2 ? Uri.UnescapeDataString(parts[1].Replace('+', ' ')) : "";
            form[key] = val;
        }

        return form;
    }

    private record EndPointMetadata(
        Type ControllerType,
        MethodInfo Method,
        string HttpMethod,
        List<ParameterInstruction> Instructions);

    private enum ParameterSource { Query, Form }

    private record ParameterInstruction(
        string Name,
        Type Type,
        ParameterSource Source,
        bool HasDefaultValue,
        object? DefaultValue);
}