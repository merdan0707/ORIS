using System.Net;
using System.Text;

namespace MyServer_v3.Framework.Handlers;

public class NotFoundHandler : RequestHandler
{
    private readonly byte[] _notFoundContentBytes;
    private readonly string _notFoundContentType;

    public NotFoundHandler(string staticPath)
    {
        var notFoundPath = Path.Combine(staticPath, "404.html");

        if (File.Exists(notFoundPath))
        {
            _notFoundContentBytes = File.ReadAllBytes(notFoundPath);
            _notFoundContentType = "text/html; charset=utf-8";
        }
        else
        {
            _notFoundContentBytes = Encoding.UTF8.GetBytes("<h1>404 Not Found</h1>");
            _notFoundContentType = "text/html; charset=utf-8";
        }
    }

    protected override async Task<bool> HandleCoreAsync(HttpListenerContext context)
    {
        var response = context.Response;

        response.StatusCode = (int)HttpStatusCode.NotFound;
        response.ContentType = _notFoundContentType;
        response.ContentLength64 = _notFoundContentBytes.Length;

        await using var output = response.OutputStream;
        await output.WriteAsync(_notFoundContentBytes);
        await output.FlushAsync();

        return true;
    }
}