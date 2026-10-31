using System.Net;
using Controlwork_1.HttpResults.Abstractions;

namespace Controlwork_1.HttpResults;

public class NoContentResult : IActionResult
{
    public Task ExecuteAsync(HttpListenerContext ctx)
    {
        ctx.Response.StatusCode = (int)HttpStatusCode.NoContent;
        ctx.Response.ContentLength64 = 0;
        ctx.Response.Close();
        return Task.CompletedTask;
    }
}