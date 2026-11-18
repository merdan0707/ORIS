using MiniHttpServer.HtmlTemplating;

namespace MiniHttpServer.HtmlTemplating.UnitTests;

[TestClass]
public class FileTests
{
    // экземпляр движка для всех тестов
    private readonly HtmlTemplateRenderer _engine = new();
    private string _testDirectory = "";

    // Arrange
    // временная папка для каждого теста
    [TestInitialize]
    public void Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(),
            "TemplateEngineTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);
    }

    // Cleanup: удаляем папку после теста
    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    [TestMethod]
    public void RenderFromFile_ValidFile_RendersCorrectly()
    {
        // Arrange
        string inputPath = Path.Combine(_testDirectory, "input.txt");
        File.WriteAllText(inputPath, "Hello, ${Name}!");
        var model = new Person { Name = "Alice" };
        // Act
        var result = _engine.RenderFromFile(inputPath, model);
        // Assert
        Assert.AreEqual("Hello, Alice!", result);
    }

    [TestMethod]
    public void RenderFromFile_FileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        string missingPath = Path.Combine(_testDirectory, "does_not_exist.txt");
        var model = new Person();
        // Act
        // Assert
        Assert.Throws<FileNotFoundException>(() => _engine.RenderFromFile(missingPath, model));
    }

    [TestMethod]
    public void RenderToFile_ValidPaths_CreatesFileAndReturnsResult()
    {
        // Arrange
        string inputPath = Path.Combine(_testDirectory, "in.txt");
        string outputPath = Path.Combine(_testDirectory, "out.txt");
        File.WriteAllText(inputPath, "Hello, ${Name}!");
        var model = new Person { Name = "Bob" };

        // Act
        var result = _engine.RenderToFile(inputPath, outputPath, model);

        // Assert
        Assert.AreEqual("Hello, Bob!", result);
        Assert.IsTrue(File.Exists(outputPath), "Выходной файл не был создан");
        Assert.AreEqual("Hello, Bob!", File.ReadAllText(outputPath), "Содержимое файла не совпадает");
    }

    [TestMethod]
    public void RenderToFile_OutputInNewSubdirectory_CreatesDirectoryAutomatically()
    {
        // Arrange
        string inputPath = Path.Combine(_testDirectory, "in.txt");
        string outputPath = Path.Combine(_testDirectory, "sub", "folder", "out.txt");
        File.WriteAllText(inputPath, "Hello, ${Name}!");
        var model = new Person { Name = "John" };
        // Act 
        var result = _engine.RenderToFile(inputPath, outputPath, model);
        // Assert
        Assert.AreEqual("Hello, John!", result);
        Assert.IsTrue(File.Exists(outputPath));
        Assert.IsTrue(Directory.Exists(Path.Combine(_testDirectory, "sub", "folder")));
    }

    [TestMethod]
    public void RenderToFile_OverwritesExistingFile()
    {
        // Arrange
        string inputPath = Path.Combine(_testDirectory, "in.txt");
        string outputPath = Path.Combine(_testDirectory, "out.txt");
        File.WriteAllText(inputPath, "New content: ${Name}");
        File.WriteAllText(outputPath, "Old content that should be deleted");
        var model = new Person { Name = "John" };
        
        // Act
        _engine.RenderToFile(inputPath, outputPath, model);
        // Assert
        Assert.AreEqual("New content: John", File.ReadAllText(outputPath));
    }
}