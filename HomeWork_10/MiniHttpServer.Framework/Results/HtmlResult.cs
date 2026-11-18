using System.Net;
using System.Text;

namespace MiniHttpServer.Framework.Results;

public sealed class HtmlResult : IHttpResult
{
    private readonly string _html;
    private readonly int _statusCode;

    public HtmlResult(string html, int statusCode = (int)HttpStatusCode.OK)
    {
        _html = html ?? throw new ArgumentNullException(nameof(html));
        _statusCode = statusCode;
    }
    
    public HttpResponseData Prepare() => new(
        _statusCode,
        "text/html; charset=utf-8",
        Encoding.UTF8.GetBytes(_html));

    public Task ExecuteAsync(HttpListenerResponse response, CancellationToken cancellationToken = default)
        => HttpResponseWriter.WriteAsync(response, Prepare(), cancellationToken);
}