using System.Net;
using System.Text.Json;

namespace MiniHttpServer.Framework.Results;

public sealed class JsonResult : IHttpResult
{
    private readonly object? _data;
    private readonly int _statusCode;

    public JsonResult(object? data, int statusCode = (int)HttpStatusCode.OK)
    {
        _data = data;
        _statusCode = statusCode;
    }
    
    public HttpResponseData Prepare() => new(
        _statusCode,
        "application/json; charset=utf-8",
        JsonSerializer.SerializeToUtf8Bytes(_data));

    public Task ExecuteAsync(HttpListenerResponse response, CancellationToken cancellationToken = default)
        => HttpResponseWriter.WriteAsync(response, Prepare(), cancellationToken);
}