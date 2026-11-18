
namespace MiniHttpServer.ORM.UnitTests;

[TestClass]
public class OrmContextTests
{
    [TestMethod]
    [DataRow("")]
    [DataRow("  ")]
    [DataRow(null)]
    public void Constructor_InvalidConnectionString_ThrowsArgumentException(string? connectionString)
    {
        // Act
        // Assert
        Assert.Throws<ArgumentException>(() => new OrmContext(connectionString!));
    }

    [TestMethod]
    public void Constructor_ValidConnectionString_CreatesInstance()
    {
        // Act
        var context = new OrmContext("Host=localhost;Database=test;");
        
        // Assert
        Assert.IsNotNull(context);
    }
}