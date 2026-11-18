using MiniHttpServer.ORM.Builders;

namespace MiniHttpServer.ORM.UnitTests;

[TestClass]
public class SqlQueryBuilderTests
{
    // Тестовые модели
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string GroupName { get; set; } = "";
        public int Age { get; set; }
    }

    public class Product
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public decimal Price { get; set; }
    }
    
    public class EmptyModel
    {
        public int Id { get; set; }
    }
    
    public class NullableModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    [TestMethod]
    [DataRow("Users", "\"users\"")]
    [DataRow("Products", "\"products\"")]
    [DataRow("my_table", "\"my_table\"")]
    [DataRow("_internal", "\"_internal\"")]
    [DataRow("Table123", "\"table123\"")]
    public void EscapeIdentifier_ValidNames_ReturnsQuotedLowercase(string input, string expected)
    {
        // Arrange, Act
        var result = SqlQueryBuilder.EscapeIdentifier(input, toLower: true);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow(null)]
    [DataRow("bad name")]
    [DataRow("name-with-dash")]
    [DataRow("name;drop")]
    [DataRow("123start")]
    [DataRow("table.name")]
    public void EscapeIdentifier_InvalidNames_ThrowsArgumentException(string? input)
    {
        // Arrange, Act, Assert
        Assert.Throws<ArgumentException>(() =>
            SqlQueryBuilder.EscapeIdentifier(input!, true));
    }

    [TestMethod]
    public void BuildSelectById_GeneratesCorrectSql()
    {
        // Arrange
        // Act
        var query = SqlQueryBuilder.BuildSelectById("users", 11);

        // Assert
        Assert.AreEqual("SELECT * FROM \"users\" WHERE \"id\" = @Id", query.Query);
        Assert.AreEqual(11, query.Parameters["@Id"]);
    }

    [TestMethod]
    public void BuildInsert_GeneratesCorrectSql_AndExcludesId()
    {
        // Arrange
        var user = new User { Id = 999, Name = "John", Age = 20, GroupName = "1234" };
        
        // Act
        var query = SqlQueryBuilder.BuildInsert("users", user);
        
        //Assert
        StringAssert.StartsWith(query.Query, "INSERT INTO \"users\" (");
        StringAssert.Contains(query.Query, "\"name\"");
        StringAssert.Contains(query.Query, "\"groupname\"");
        StringAssert.Contains(query.Query, "\"age\"");
        StringAssert.Contains(query.Query, "VALUES");
        
        // Id не должен быть ни в SQL, ни в параметрах
        Assert.DoesNotContain("\"id\"", query.Query, "Id не должен быть в INSERT");
        Assert.IsFalse(query.Parameters.ContainsKey("@Id"), "Id не должен быть в параметрах");
        
        // Значения параметров
        Assert.AreEqual("John", query.Parameters["@Name"]);
        Assert.AreEqual("1234", query.Parameters["@GroupName"]);
        Assert.AreEqual(20, query.Parameters["@Age"]);
    }
    
    [TestMethod]
    public void BuildInsert_WorksWithDifferentModels()
    {
        // Arrange
        var product = new Product { Id = 0, Title = "Книга", Price = 500.5m };
        
        // Act
        var query = SqlQueryBuilder.BuildInsert("products", product);
        
        // Assert
        StringAssert.Contains(query.Query, "\"title\"");
        StringAssert.Contains(query.Query, "\"price\"");
        Assert.DoesNotContain("\"id\"", query.Query);
        Assert.AreEqual("Книга", query.Parameters["@Title"]);
        Assert.AreEqual(500.5m, query.Parameters["@Price"]);
    }
    
    [TestMethod]
    public void BuildInsert_EmptyModel_Throws()
    {
        // Arrange
        var empty = new EmptyModel();
        
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            SqlQueryBuilder.BuildInsert("empty", empty));
    }

    [TestMethod]
    public void BuildUpdate_GeneratesCorrectSql_AndExcludesIdFromSet()
    {
        // Arrange
        var user = new User { Name = "Пётр", GroupName = "1233", Age = 25 };
        
        // Act
        var query = SqlQueryBuilder.BuildUpdate("users", 42, user);
        
        // Assert
        Assert.AreEqual("UPDATE \"users\" SET \"name\" = @Name, \"groupname\" = @GroupName, \"age\" = @Age WHERE \"id\" = @Id", 
            query.Query);
        // В SET нет @Id, но в параметрах есть
        Assert.AreEqual(42, query.Parameters["@Id"]);
        Assert.AreEqual("Пётр", query.Parameters["@Name"]);
    }
    
    [TestMethod]
    public void BuildUpdate_DoesNotModifyPrimaryKey()
    {
        // Arrange
        var user = new User { Id = 999, Name = "Пётр" };
        
        // Act
        var query = SqlQueryBuilder.BuildUpdate("users", 42, user);
        
        // Assert
        // Id из модели (999) не должен попасть никуда — только id из аргумента (42)
        Assert.AreEqual(42, query.Parameters["@Id"]);
        Assert.DoesNotContain("\"id\" = @Id,", query.Query, "Id не должен быть в SET");
    }
    
    [TestMethod]
    public void BuildDelete_GeneratesCorrectSql()
    {
        // Arrange
        // Act
        var query = SqlQueryBuilder.BuildDelete("users", 42);
        
        // Assert
        Assert.AreEqual("DELETE FROM \"users\" WHERE \"id\" = @Id", query.Query);
        Assert.AreEqual(42, query.Parameters["@Id"]);
    }
    
    [TestMethod]
    public void BuildInsert_NullableProperty_PassedAsNull()
    {
        // Arrange
        var model = new NullableModel { Name = null };
        
        // Act
        var query = SqlQueryBuilder.BuildInsert("test", model);
        
        // Assert
        Assert.IsTrue(query.Parameters.ContainsKey("@Name"));
        Assert.IsNull(query.Parameters["@Name"]);
    }
}