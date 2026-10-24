using Microsoft.VisualBasic.CompilerServices;

namespace My_Server.Utils;

public static class MimeTypeHandler
{
    private static readonly Dictionary<string, string> MimeTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            {".html", "text/html; charset=utf-8" },
            {".htm", "text/html; charset=utf-8" },
            {".css" , "text/css; charset=utf-8" },
            {".js" , "application/javascript; charset=utf-8" },
            {".json" , "application/json; charset=utf-8" },
            {".jpg" , "image/jpeg" },
            {".jpeg" , "image/jpeg" },
            {".png" , "image/png" },
            {".gif" , "image/gif" },
            {".svg" , "image/svg+xml" },
            {".ico" , "image/x-icon" },
            {".webp" , "image/webp" },
            {".txt" , "text/plain; charset=utf-8" },
        };

    public static string GetMimeType(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        MimeTypes.TryGetValue(extension, out var mimeType);
        return mimeType?? "application/octet-stream";
    }
}