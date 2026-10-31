using System.Net;
using System.Text;
using Controlwork_1.HttpResults.Abstractions;

namespace Controlwork_1.HttpResults;

public class HtmlResult : IActionResult
{
    private readonly string _html;
    private readonly int _status;

    public HtmlResult(string html, int status = 200) { _html = html; _status = status; }

    public async Task ExecuteAsync(HttpListenerContext ctx)
    {
        var buffer = Encoding.UTF8.GetBytes(_html);
        ctx.Response.StatusCode = _status;
        ctx.Response.ContentType = "text/html; charset=utf-8";
        ctx.Response.ContentLength64 = buffer.Length;
        await using var output = ctx.Response.OutputStream;
        await output.WriteAsync(buffer);
    }
}