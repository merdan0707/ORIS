namespace MyServer_v3.Utils;

public static class UriFactory
{
    /// <summary>
    /// Safely converts a URL path to a physical path on disk.
    /// </summary>
    /// <returns>null if the path contains attempts to access beyond the rootDirectory</returns>
    public static string? ResolveSafePath(string urlPath, string rootDirectory)
    {
        var relativePath = urlPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var absolutePath = Path.GetFullPath(Path.Combine(rootDirectory, relativePath));
        
        var rootAbsolutePath = Path.GetFullPath(rootDirectory);
        if (!absolutePath.StartsWith(rootAbsolutePath, StringComparison.OrdinalIgnoreCase))
            return null;

        return absolutePath;
    }
}