using System.Net;
using MyServer_v3.Utils;

namespace MyServer_v3.Framework.Handlers;

public class StaticFileHandler : RequestHandler
{
    private readonly string _rootDirectory;

    public StaticFileHandler(string rootDirectory)
    {
        _rootDirectory = Path.GetFullPath(rootDirectory);
    }

    protected override async Task<bool> HandleCoreAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;
        
        var urlPath = request.Url.LocalPath;
        
        if (urlPath.Equals("/login", StringComparison.OrdinalIgnoreCase))
        {
            urlPath = "/LoginForm/login.html";
        }
        
        var physicalPath = UriFactory.ResolveSafePath(urlPath, _rootDirectory);
        
        if (physicalPath == null)
        {
            return false;
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
                return false;
            }
        }

        if (!File.Exists(physicalPath))
        {
            return false;
        }

        try
        {
            var fileBytes = await File.ReadAllBytesAsync(physicalPath);
            response.ContentType = MimeTypeHandler.GetMimeType(physicalPath);
            response.ContentLength64 = fileBytes.Length;
            response.StatusCode = (int)HttpStatusCode.OK;
            
            await using var output = response.OutputStream;
            await output.WriteAsync(fileBytes);
            await output.FlushAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при чтении файла {physicalPath}: {ex.Message}");
            return false;
        }
    }
}