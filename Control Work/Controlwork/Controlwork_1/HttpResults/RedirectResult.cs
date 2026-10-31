using System.Net;
using Controlwork_1.HttpResults.Abstractions;

namespace Controlwork_1.HttpResults;

public class RedirectResult : IActionResult
{
    private readonly string _location;
    private readonly int _status;

    public RedirectResult(string location, int status = 303) { _location = location; _status = status; }

    public Task ExecuteAsync(HttpListenerContext ctx)
    {
        ctx.Response.StatusCode = _status;
        ctx.Response.RedirectLocation = _location;
        ctx.Response.ContentLength64 = 0;
        ctx.Response.Close();
        return Task.CompletedTask;
    }
}