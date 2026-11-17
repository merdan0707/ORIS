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
    
    public async Task ExecuteAsync(HttpListenerResponse response, CancellationToken cancellationToken = default)
    {
        string fullPath = Path.IsPathRooted(_templatePath)
            ? _templatePath
            : Path.Combine(AppContext.BaseDirectory, "Templates", _templatePath);

        string html;
        try
        {
            html = _renderer.RenderFromFile(fullPath, _dataModel);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PAGE] Ошибка рендеринга '{fullPath}': {ex}");
            await new HtmlResult("<h1>500 — ошибка рендеринга шаблона</h1>", 500)
                .ExecuteAsync(response, cancellationToken);
            return;
        }

        await new HtmlResult(html, _statusCode).ExecuteAsync(response, cancellationToken);
    }
}