using System.Net;
using My_Server.Utils;

namespace My_Server.Framework.Http;

public class StaticFileHandler
{
    private readonly string _rootDirectory;
    private readonly byte[] _notFoundContentBytes;
    private readonly string _notFoundContentType;

    public StaticFileHandler(string rootDirectory)
    {
        _rootDirectory = Path.GetFullPath(rootDirectory);
        
        // cache the 404-page once when it is created
        var notFoundPath = Path.Combine(rootDirectory, "404.html");
        if (File.Exists(notFoundPath))
        {
            _notFoundContentBytes = File.ReadAllBytes(notFoundPath);
            _notFoundContentType = MimeTypeHandler.GetMimeType(notFoundPath);
        }
        else
        {
            _notFoundContentBytes = System.Text.Encoding.UTF8.GetBytes("<h1>404 Not Found</h1>");
            _notFoundContentType = "text/html; charset=utf-8";
        }
    }

    public async Task HandleAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;
        var physicalPath = UriFactory.ResolveSafePath(request.Url.LocalPath,  _rootDirectory);

        if (physicalPath == null)
        {
            await SendNotFoundAsync(response);
            return;
        }

        if (Directory.Exists(physicalPath))
        {
            var indexPath = Path.Combine(physicalPath, "index.html");
            if (File.Exists(indexPath))
            {
                physicalPath = indexPath;
            }
            else
            {
                await SendNotFoundAsync(response);
                return;
            }
        }

        if (!File.Exists(physicalPath))
        {
            await SendNotFoundAsync(response);
            return;
        }

        try
        {
            var fileBytes = await File.ReadAllBytesAsync(physicalPath);
            response.ContentType = MimeTypeHandler.GetMimeType(physicalPath);
            response.ContentLength64 = fileBytes.Length;
            response.StatusCode = (int)HttpStatusCode.OK;

            await using var output = response.OutputStream;
            await output.WriteAsync(fileBytes, 0, fileBytes.Length);
            await output.FlushAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error with reading file {physicalPath}: {ex.Message}");
            await SendNotFoundAsync(response);
        }
    }

    private async Task SendNotFoundAsync(HttpListenerResponse response)
    {
        response.ContentType = _notFoundContentType;
        response.ContentLength64 = _notFoundContentBytes.Length;
        response.StatusCode = (int)HttpStatusCode.NotFound;
        
        await using var output = response.OutputStream;
        await output.WriteAsync(_notFoundContentBytes, 0, _notFoundContentBytes.Length);
        await output.FlushAsync();
    }
}