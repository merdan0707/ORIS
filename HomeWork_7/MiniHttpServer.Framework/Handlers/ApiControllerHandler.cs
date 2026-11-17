using System.Globalization;
using System.Net;
using System.Reflection;
using System.Web;
using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Controllers;
using MiniHttpServer.Framework.Results;

namespace MiniHttpServer.Framework.Handlers;

public class ApiControllerHandler : RequestHandler
{
    private readonly Dictionary<string, EndPointMetadata> _routes;

    public ApiControllerHandler(Assembly assemblyWithControllers)
    {
        _routes = new Dictionary<string, EndPointMetadata>(StringComparer.OrdinalIgnoreCase);
        ScanAssembly(assemblyWithControllers);
    }

    private void ScanAssembly(Assembly assembly)
    {
        var controllers = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && Attribute.IsDefined(t, typeof(HttpControllerAttribute)));

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

                _routes[fullRouteKey] = new EndPointMetadata(type, method, httpMethod, AnalyzeParameters(method));
                Console.WriteLine($"[API] Registered: {httpMethod} /{fullRouteKey}");
            }
        }
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

            instructions.Add(new ParameterInstruction(param.Name!, param.ParameterType, source,
                param.HasDefaultValue, param.DefaultValue));
        }

        return instructions;
    }

    protected override async Task<bool> HandleCoreAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        var path = request.Url!.LocalPath.TrimStart('/');
        if (!path.StartsWith("api/", StringComparison.OrdinalIgnoreCase))
            return false;

        // если нет такого route
        if (!_routes.TryGetValue(path, out var endpoint))
            return false; // отдаём дальше по цепочке (Static -> NotFound)

        // ответ закрываем ровно один раз, в finally
        try
        {
            if (endpoint.HttpMethod != request.HttpMethod)
            {
                await new TextResult($"Method Not Allowed. Expected {endpoint.HttpMethod}",
                    (int)HttpStatusCode.MethodNotAllowed).ExecuteAsync(response);
                return true;
            }
            
            object[] parameters;
            try
            {
                parameters = await ExtractParametersAsync(request, endpoint.ParameterInstructions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Binding error: {ex.Message}");
                await TryWriteErrorAsync(response, HttpStatusCode.BadRequest, "Некорректные параметры запроса");
                return true;
            }

            // Создание контроллера, контекст, вызов действия
            object? actionResult;
            try
            {
                var controller = (BaseController)Activator.CreateInstance(endpoint.ControllerType)!;
                controller.InitializeContext(context);

                actionResult = endpoint.Method.Invoke(controller, parameters);
                
                if (actionResult is Task task)
                {
                    await task;
                    actionResult = task.GetType().GetProperty("Result")?.GetValue(task);
                }
            }
            catch (Exception ex)
            {
                var actual = ex is TargetInvocationException { InnerException: { } inner } ? inner : ex;
                Console.WriteLine($"[API] Action error: {actual}");
                await TryWriteErrorAsync(response, HttpStatusCode.InternalServerError, "Внутренняя ошибка сервера");
                return true;
            }

            // Проверка контракта и полиморфное выполнение результата
            if (actionResult is not IHttpResult result)
            {
                Console.WriteLine($"[API] {endpoint.ControllerType.Name}.{endpoint.Method.Name} " +
                                  $"вернул {actionResult?.GetType().Name ?? "null"}, ожидался IHttpResult");
                await TryWriteErrorAsync(response, HttpStatusCode.InternalServerError, "Внутренняя ошибка сервера");
                return true;
            }

            try
            {
                await result.ExecuteAsync(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Ошибка записи ответа: {ex.Message}");
            }

            return true;
        }
        finally
        {
            try
            {
                response.Close();
            }
            catch
            {
                /* соединение уже разорвано */
            }
        }
    }

    private static async Task TryWriteErrorAsync(HttpListenerResponse response, HttpStatusCode status, string message)
    {
        try
        {
            await new JsonResult(new { success = false, message }, (int)status).ExecuteAsync(response);
        }
        catch
        {
            // ответ уже начал отправляться — статус/тело не изменить, просто завершаем
        }
    }

    private static async Task<object[]> ExtractParametersAsync(HttpListenerRequest request,
        List<ParameterInstruction> instructions)
    {
        var parameters = new object[instructions.Count];
        var formData = await GetFormDataAsync(request);

        for (int i = 0; i < instructions.Count; i++)
        {
            var instr = instructions[i];
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
                parameters[i] = Convert.ChangeType(rawValue, instr.Type, CultureInfo.InvariantCulture);
            else if (instr.HasDefaultValue)
                parameters[i] = instr.DefaultValue!;
            else
                throw new ArgumentException($"Missing required parameter: '{instr.Name}' from {instr.Source}");
        }

        return parameters;
    }

    private static async Task<Dictionary<string, string>> GetFormDataAsync(HttpListenerRequest request)
    {
        var formData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // ContentType может быть null (POST без заголовка)
        if (request.HttpMethod != "POST" ||
            request.ContentType?.Contains("application/x-www-form-urlencoded") != true)
            return formData;

        using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
        var body = await reader.ReadToEndAsync();

        // ParseQueryString корректно декодирует и %XX, и '+' как пробел
        var parsed = HttpUtility.ParseQueryString(body);
        foreach (string? key in parsed.AllKeys)
        {
            if (key != null)
                formData[key] = parsed[key] ?? string.Empty;
        }

        return formData;
    }

    private record EndPointMetadata(
        Type ControllerType,
        MethodInfo Method,
        string HttpMethod,
        List<ParameterInstruction> ParameterInstructions);

    private enum ParameterSource
    {
        Query,
        Form,
        Body
    }

    private record ParameterInstruction(
        string Name,
        Type Type,
        ParameterSource Source,
        bool HasDefaultValue,
        object? DefaultValue);
}