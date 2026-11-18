using System.Text;
using MiniHttpServer.Framework.Results;
using MiniHttpServer.HtmlTemplating;

namespace MiniHttpServer.Framework.UnitTests.Results;

public class FakeRenderer : IHtmlTemplateRenderer
{
    public string? RequestedPath;
    public object? RequestedModel;
    public string Html { get; set; } = "<p>ok</p>";
    public bool Throw { get; set; }

    public string RenderFromString(string template, object model) => Html;
    public string RenderFromFile(string filePath, object model)
    {
        RequestedPath = filePath;
        RequestedModel = model;
        if (Throw) throw new FileNotFoundException($"Файл шаблона не найден: {filePath}");
        return Html;
    }
    public string RenderToFile(string input, string output, object model) => Html;
}

[TestClass]
public class ResultTests
{
    [TestMethod]
    public void JsonResult_Prepare_BuildsCorrectResponseData()
    {
        // Arrange
        var result = new JsonResult(new { success = true, message = "ok" });

        // Act
        var responseData = result.Prepare();

        // Assert
        Assert.AreEqual(200, responseData.StatusCode);
        Assert.AreEqual("application/json; charset=utf-8", responseData.ContentType);
        Assert.AreEqual("{\"success\":true,\"message\":\"ok\"}", Encoding.UTF8.GetString(responseData.Body));
    }

    [TestMethod]
    public void JsonResult_PrepareNullObject_ReturnCorrectResponseDate()
    {
        // Arrange
        var result = new JsonResult(null, 404);

        // Act
        var responseData = result.Prepare();

        // Assert
        Assert.AreEqual(404, responseData.StatusCode);
        Assert.AreEqual("null", Encoding.UTF8.GetString(responseData.Body));
    }

    [TestMethod]
    public void HtmlResult_Prepare_BuildsCorrectResponseData()
    {
        // Arrange
        var result = new HtmlResult("<h1>Test</h1>", 200);

        // Act
        var responseData = result.Prepare();

        // Assert
        Assert.AreEqual(200, responseData.StatusCode);
        Assert.AreEqual("text/html; charset=utf-8", responseData.ContentType);
        Assert.AreEqual("<h1>Test</h1>", Encoding.UTF8.GetString(responseData.Body));
    }

    [TestMethod]
    public void TextResult_Prepare_BuildsCorrectResponseData()
    {
        // Arrange
        var result = new TextResult("Plain text", 200);

        // Act
        var responseData = result.Prepare();

        // Assert
        Assert.AreEqual(200, responseData.StatusCode);
        Assert.AreEqual("text/plain; charset=utf-8", responseData.ContentType);
        Assert.AreEqual("Plain text", Encoding.UTF8.GetString(responseData.Body));
    }

    [TestMethod]
    public void PageResult_Prepare_RelativePath_ResolvesDirectories()
    {
        // Arrange
        var renderer = new FakeRenderer();
        var result = new PageResult("User/Profile.html", new { Name = "Alex" }, renderer);

        // Act
        result.Prepare();

        // Assert
        var expectedPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Templates", "User", "Profile.html"));
        var actualPath = Path.GetFullPath(renderer.RequestedPath!);
    
        Assert.AreEqual(expectedPath, actualPath);
    }

    [TestMethod]
    public void PageResult_Prepare_RendererOk_ReturnsHtmlStatus200()
    {
        // Arrange
        var renderer = new FakeRenderer { Html = "<h1>Hi</h1>" };
        var result = new PageResult("/abs/p.html", new object(), renderer);

        // Act
        var responseData = result.Prepare();

        // Assert
        Assert.AreEqual(200, responseData.StatusCode);
        Assert.AreEqual("text/html; charset=utf-8", responseData.ContentType);
        Assert.AreEqual("<h1>Hi</h1>", Encoding.UTF8.GetString(responseData.Body));
    }

    [TestMethod]
    public void PageResult_Prepare_RendererThrows_FallsBackTo500()
    {
        // Arrange
        var renderer = new FakeRenderer { Throw = true };
        var result = new PageResult("/abs/p.html", new object(), renderer);

        // Act
        var payload = result.Prepare();

        // Assert
        Assert.AreEqual(500, payload.StatusCode);
        StringAssert.Contains(Encoding.UTF8.GetString(payload.Body), "500");
    }
}