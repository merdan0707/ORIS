using System.Net;

namespace MiniHttpServer.Framework.Results;

internal static class HttpResponseWriter
{
    public static async Task WriteAsync(HttpListenerResponse response, HttpResponseData preparedResponseData, CancellationToken cancellationToken = default)
    {
        response.StatusCode = preparedResponseData.StatusCode;
        response.ContentType = preparedResponseData.ContentType;
        response.ContentLength64 = preparedResponseData.Body.Length;
        await response.OutputStream.WriteAsync(preparedResponseData.Body, cancellationToken);
    }
}