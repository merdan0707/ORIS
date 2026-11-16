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

    public async Task ExecuteAsync(HttpListenerResponse response, CancellationToken cancellationToken = default)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(_text);

        response.StatusCode = _statusCode;
        response.ContentType = "text/plain; charset=utf-8";
        response.ContentLength64 = buffer.Length;

        await response.OutputStream.WriteAsync(buffer, cancellationToken);
    }
}