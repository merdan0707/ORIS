using System.Net;
using Microsoft.Win32.SafeHandles;
using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Controllers;
using MiniHttpServer.Framework.Results;

namespace MiniHttpServer.Controllers;

[HttpController]
public class CookieController : BaseController
{
    [HttpGet]
    //[Route("set")]
    public IHttpResult Set()
    {
        var cookie = new Cookie("testCookie", "ValueOfTestCookie", "/")
        {
            HttpOnly = true,
            Expires = DateTime.UtcNow.AddMinutes(30)
        };

        Context.Response.SetCookie(cookie);

        return Html("<h1>Cookie установлена</h1>");
    }

    [HttpGet]
    public IHttpResult Get()
    {
        string? value = Context.Request.Cookies["testCookie"]?.Value;

        return Json(new { Value = value });
    }

    [HttpGet]
    public IHttpResult Delete()
    {
        var cookie = new Cookie("testCookie", string.Empty, "/")
        {
            Expires = DateTime.UtcNow.AddDays(-1)
        };

        Context.Response.SetCookie(cookie);

        return Html("<h1>Cookie удалена</h1>");
    }
}