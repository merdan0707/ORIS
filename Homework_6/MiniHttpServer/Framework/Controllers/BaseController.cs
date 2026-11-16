using System.Net;
using MiniHttpServer.Framework.Results;

namespace MiniHttpServer.Framework.Controllers;

public abstract class BaseController
{
    protected HttpListenerContext Context { get; private set; } = null!;

    internal void InitializeContext(HttpListenerContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    protected IHttpResult Html(string html, int statusCode = (int)HttpStatusCode.OK)
        => new HtmlResult(html, statusCode);

    protected IHttpResult Json(object? data, int statusCode = (int)HttpStatusCode.OK)
        => new JsonResult(data, statusCode);

    protected IHttpResult Text(string text, int statusCode = (int)HttpStatusCode.OK)
        => new TextResult(text, statusCode);
}