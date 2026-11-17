using MiniHttpServer.Framework.Content;

namespace MiniHttpServer.Framework.UnitTests.Content;

[TestClass]
public class ContentTests
{
    [TestMethod]
    [DataRow("file.html", "text/html; charset=utf-8")]
    [DataRow("style.css", "text/css; charset=utf-8")]
    [DataRow("script.js", "application/javascript; charset=utf-8")]
    [DataRow("image.png", "image/png")]
    [DataRow("image.jpg", "image/jpeg")]
    [DataRow("web.webp" , "image/webp")]
    [DataRow("text.txt" , "text/plain; charset=utf-8")]
    [DataRow("gif.gif" , "image/gif")]
    [DataRow("favicon.ico" , "image/x-icon")]
    [DataRow("arrow.svg" , "image/svg+xml")]
    [DataRow("unknown.xyz", "application/octet-stream")]
    [DataRow("file", "application/octet-stream")] // без расширения
    public void GetMimeType_VariousExtensions_ReturnsCorrectMimeType(string filePath, string expectedMimeType)
    {
        // Arrange
        // Act
        var result = MimeTypeProvider.GetMimeType(filePath);

        // Assert
        Assert.AreEqual(expectedMimeType, result);
    }

    [TestMethod]
    public void ResolveSafePath_ValidPath_ReturnsAbsolutePath()
    {
        // Arrange
        string root = Path.GetFullPath(@"C:\www");
        string urlPath = "/css/style.css";

        // Act
        var result = UriFactory.ResolveSafePath(urlPath, root);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(Path.Combine(root, "css", "style.css"), result);
    }

    [TestMethod]
    public void ResolveSafePath_DirectoryTraversal_ReturnsNull()
    {
        // Arrange
        string root = Path.GetFullPath(@"C:\www");
        string urlPath = "/../Windows/System32/cmd.exe"; // за пределы root

        // Act
        var result = UriFactory.ResolveSafePath(urlPath, root);

        // Assert
        Assert.IsNull(result, "Безопасный путь не должен позволять выход за пределы корневой папки.");
    }
}