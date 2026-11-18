using MiniHttpServer.HtmlTemplating;

namespace MiniHttpServer.HtmlTemplating.UnitTests;

[TestClass]
public class VariableTests
{
    // экземпляр движка для всех тестов
    private readonly HtmlTemplateRenderer _engine = new ();

    [TestMethod]
    public void EmptyTemplate_ReturnsEmpty()
    {
        // Arrange
        // Act
        var result = _engine.RenderFromString("", new Person());
        // Assert
        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void TemplateWithoutTags_ReturnsAsIs()
    {
        // Arrange
        // Act
        var result = _engine.RenderFromString("Hello, World!", new Person());
        // Assert
        Assert.AreEqual("Hello, World!", result);
    }

    [TestMethod]
    public void SimpleVariable_Replaced()
    {
        // Arrange
        // Act
        var model = new Person { Name = "John" };
        var result = _engine.RenderFromString("Hello, ${Name}!", model);
        // Assert
        Assert.AreEqual("Hello, John!", result);
    }

    [TestMethod]
    public void MultipleVariables_AllReplaced()
    {
        // Arrange
        // Act
        var model = new Person { Name = "John", Age = 30 };
        var result = _engine.RenderFromString("${Name} is ${Age}", model);
        // Assert
        Assert.AreEqual("John is 30", result);
    }
    
    [TestMethod]
    public void VariableInsideText_ReplacedInPlace()
    {
        // Arrange
        // Act
        var model = new Person { Name = "John", Age = 30 };
        var result = _engine.RenderFromString("a${Name}b${Age}c", model);
        // Assert
        Assert.AreEqual("aJohnb30c", result);
    }
    
    [TestMethod]
    public void NestedProperty_Replaced()
    {
        // Arrange
        // Act
        var model = new Order
        {
            Customer = new Person { Name = "Alice", Age = 25 }
        };
        var result = _engine.RenderFromString("${Customer.Name}", model);
        // Assert
        Assert.AreEqual("Alice", result);
    }
    
    [TestMethod]
    public void UnknownVariable_RendersEmpty()
    {
        // Arrange
        // Act
        var model = new Person { Name = "John" };
        var result = _engine.RenderFromString("Hello, ${Unknown}!", model);
        // Assert
        Assert.AreEqual("Hello, !", result);
    }
    
    [TestMethod]
    public void NullProperty_RendersEmpty()
    {
        // Arrange
        // Act
        var model = new Person { Name = "John", Nickname = null };
        var result = _engine.RenderFromString("Nickname: ${Nickname}", model);
        // Assert
        Assert.AreEqual("Nickname: ", result);
    }
    
    [TestMethod]
    public void NullNestedProperty_RendersEmpty()
    {
        // Arrange
        // Act
        var model = new NullableModel { User = null };
        var result = _engine.RenderFromString("User: ${User.Name}", model);
        // Assert
        Assert.AreEqual("User: ", result);
    }
    
    [TestMethod]
    public void UnknownNestedProperty_RendersEmpty()
    {
        // Arrange
        // Act
        var model = new Person { Name = "John" };
        var result = _engine.RenderFromString("Username: ${Name.UnknownProperty}", model);
        // Assert
        Assert.AreEqual("Username: ", result);
    }
    
    [TestMethod]
    public void NullModel_AllVariablesEmpty()
    {
        // Arrange
        // Act
        var result = _engine.RenderFromString("Hello, ${Name}!", null!);
        // Assert
        Assert.AreEqual("Hello, !", result);
    }
    
    [TestMethod]
    [DataRow("${name}")]
    [DataRow("${NAME}")]
    [DataRow("${nAmE}")]
    public void VariableName_IsCaseInsensitive(string template)
    {
        // Arrange
        var model = new Person { Name = "John" };
        // Act
        // Assert
        Assert.AreEqual("John", _engine.RenderFromString(template, model));
    }
    
    [TestMethod]
    [DataRow("${ Name }")]
    [DataRow("${  Name  }")]
    [DataRow("${\tName\t}")]
    public void SpacesAroundVariable_Trimmed(string template)
    {
        // Arrange
        var model = new Person { Name = "John" };
        // Act
        // Assert
        Assert.AreEqual("John", _engine.RenderFromString(template, model));
    }
    
    [TestMethod]
    public void UnclosedVariable_Throws()
    {
        // Arrange
        var model = new Person { Name = "John" };
        
        // ${Name — нет закрывающей скобки }
        // Act
        // Assert
        Assert.Throws<System.Data.SyntaxErrorException>(
            () => _engine.RenderFromString("${Name", model));
    }
    
    [TestMethod]
    public void EmptyVariableExpression_RendersEmpty()
    {
        // Arrange
        // Act
        var model = new Person { Name = "John" };
        var result = _engine.RenderFromString("a${}b", model);
        // Assert
        Assert.AreEqual("ab", result);
    }
}