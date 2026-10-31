using System.Net;
using System.Text;
using System.Text.Json;
using Controlwork_1.HttpResults.Abstractions;

namespace Controlwork_1.HttpResults;

public class JsonResult : IActionResult
{
    private readonly object? _data;

    public JsonResult(object? data) => _data = data;

    public async Task ExecuteAsync(HttpListenerContext ctx)
    {
        var json = JsonSerializer.Serialize(_data);
        var buffer = Encoding.UTF8.GetBytes(json);
        ctx.Response.StatusCode = (int)HttpStatusCode.OK;
        ctx.Response.ContentType = "application/json; charset=utf-8";
        ctx.Response.ContentLength64 = buffer.Length;
        await using var output = ctx.Response.OutputStream;
        await output.WriteAsync(buffer);
    }
}