using System.Net;
using System.Text;

namespace MiniHttpServer.Framework.Results;

public sealed class TextResult : IHttpResult
{
    private readonly string _text;
    private readonly int _statusCode;

    public TextResult(string text, int statusCode = (int)HttpStatusCode.OK)
    {
        _text = text ?? throw new ArgumentNullException(nameof(text));
        _statusCode = statusCode;
    }

    public HttpResponseData Prepare() => new(
        _statusCode,
        "text/plain; charset=utf-8",
        Encoding.UTF8.GetBytes(_text));

    public Task ExecuteAsync(HttpListenerResponse response, CancellationToken cancellationToken = default)
        => HttpResponseWriter.WriteAsync(response, Prepare(), cancellationToken);
}