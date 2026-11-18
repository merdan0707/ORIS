namespace MiniHttpServer.Framework.Results;

public sealed record HttpResponseData(int StatusCode, string ContentType, byte[] Body);