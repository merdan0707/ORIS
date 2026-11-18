using System.Net;
using MiniHttpServer.Framework.Results;
using MiniHttpServer.HtmlTemplating;

namespace MiniHttpServer.Framework.Controllers;

public abstract class BaseController
{
    private static readonly IHtmlTemplateRenderer Renderer = new HtmlTemplateRenderer();
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

    protected IHttpResult Page(string templatePath, object dataModel, int statusCode = (int)HttpStatusCode.OK)
        => new PageResult(templatePath, dataModel, Renderer, statusCode);
}