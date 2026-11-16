using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Controllers;
using MiniHttpServer.Framework.Results;

namespace MiniHttpServer.Controllers;

[HttpController]
public class TestController : BaseController
{
    [HttpGet]
    public IHttpResult Index()
    {
        return Html("<h1>Hello from controller!</h1>");
    }

    [HttpGet]
    public IHttpResult Info()
    {
        return Json(new { id = 1, message = "Hello World!", success = true });
    }

    [HttpGet]
    public IHttpResult RequestInfo()
    {
        var request = Context.Request;
        return Json(new
        {
            method = request.HttpMethod,
            path = request.Url?.LocalPath,
            userAgent = request.Headers["User-Agent"]
        });
    }
}