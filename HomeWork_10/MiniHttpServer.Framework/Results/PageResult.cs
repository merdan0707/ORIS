using System.Net;
using MiniHttpServer.HtmlTemplating;

namespace MiniHttpServer.Framework.Results;

public sealed class PageResult : IHttpResult
{
    private readonly string _templatePath;
    private readonly object _dataModel;
    private readonly IHtmlTemplateRenderer _renderer;
    private readonly int _statusCode;

    public PageResult(string templatePath, object dataModel, IHtmlTemplateRenderer renderer, int statusCode = (int)HttpStatusCode.OK)
    {
        _templatePath = templatePath ?? throw new ArgumentNullException(nameof(templatePath));
        _dataModel = dataModel;
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _statusCode = statusCode;
    }
    
    public HttpResponseData Prepare()
    {
        string normalizedTemplatePath = _templatePath
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        string fullPath = Path.IsPathRooted(normalizedTemplatePath)
            ? normalizedTemplatePath
            : Path.Combine(AppContext.BaseDirectory, "Templates", normalizedTemplatePath);

        try
        {
            return new HtmlResult(_renderer.RenderFromFile(fullPath, _dataModel), _statusCode).Prepare();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PAGE] Ошибка рендеринга '{fullPath}': {ex}");
            return new HtmlResult("<h1>500 — ошибка рендеринга шаблона</h1>", 500).Prepare();
        }
    }

    public Task ExecuteAsync(HttpListenerResponse response, CancellationToken cancellationToken = default)
        => HttpResponseWriter.WriteAsync(response, Prepare(), cancellationToken);
}