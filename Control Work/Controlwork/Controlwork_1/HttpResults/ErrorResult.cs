using System.Net;
using System.Text;
using Controlwork_1.HttpResults.Abstractions;

namespace Controlwork_1.HttpResults;

public class ErrorResult : IActionResult
{
    private readonly int _status;
    private readonly string _message;

    public ErrorResult(int status, string message) { _status = status; _message = message; }

    public async Task ExecuteAsync(HttpListenerContext ctx)
    {
        var buffer = Encoding.UTF8.GetBytes(_message);
        ctx.Response.StatusCode = _status;
        ctx.Response.ContentType = "text/plain; charset=utf-8";
        ctx.Response.ContentLength64 = buffer.Length;
        await using var output = ctx.Response.OutputStream;
        await output.WriteAsync(buffer);
    }
}