using System.Globalization;
using System.Net;
using System.Reflection;
using System.Web;
using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Authentication;
using MiniHttpServer.Framework.Controllers;
using MiniHttpServer.Framework.Results;
using MiniHttpServer.Framework.Routing;

namespace MiniHttpServer.Framework.Handlers;

public class ApiControllerHandler : RequestHandler
{
    private readonly Dictionary<string, EndPointMetadata> _routes;
    private readonly IUserSessionService _sessionService;

    public ApiControllerHandler(Assembly assemblyWithControllers, IUserSessionService sessionService)
    {
        var scanner = new RouteScanner();
        _routes = scanner.Scan(assemblyWithControllers);
        _sessionService = sessionService;
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

            if (!IsAuthorized(context, endpoint.ControllerType, endpoint.Method))
            {
                await new TextResult("401 Unauthorized", (int)HttpStatusCode.Unauthorized)
                    .ExecuteAsync(context.Response);
                return true;
            }

            object[] parameters;
            try
            {
                var formData = await GetFormDataAsync(request);
                var body = await GetBodyAsync(request);
                parameters = ParameterBinder.Bind(endpoint.ParameterInstructions, request.QueryString, formData, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Binding error: {ex.Message}");
                await TryWriteErrorAsync(response, HttpStatusCode.BadRequest, "Некорректные параметры запроса");
                return true;
            }

            // создание контроллера, передача сервиса и контекста, вызов действия
            object? actionResult;
            try
            {
                var controller = (BaseController)Activator.CreateInstance(endpoint.ControllerType)!;
                controller.Initialize(context, _sessionService);
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

    private static async Task<string?> GetBodyAsync(HttpListenerRequest request)
    {
        if (request.HasEntityBody)
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            return await reader.ReadToEndAsync();
        }

        return null;
    }

    private bool IsAuthorized(HttpListenerContext context, Type controllerType, MethodInfo methodInfo)
    {
        bool requiresAuth = controllerType.IsDefined(typeof(AuthorizeAttribute), true) ||
                            methodInfo.IsDefined(typeof(AuthorizeAttribute), true);

        // Если не требуется авторизация то пропускаем
        if (!requiresAuth) return true;

        string? sessionId = context.Request.Cookies["sessionId"]?.Value;

        int? userId = _sessionService.GetUserId(sessionId);

        return userId.HasValue;
    }
}