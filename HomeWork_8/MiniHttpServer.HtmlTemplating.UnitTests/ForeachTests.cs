using MiniHttpServer.HtmlTemplating;

namespace MiniHttpServer.HtmlTemplating.UnitTests;

[TestClass]
public class ForeachTests
{
    // экземпляр движка для всех тестов
    private readonly HtmlTemplateRenderer _engine = new();

    [TestMethod]
    [DataRow("$foreach(x in Items)[${x}]$endfor", "[a][b][c]")]
    [DataRow("$foreach(x in Items)${x},$endfor", "a,b,c,")]
    [DataRow("$foreach(varx in Items)${varx},$endfor", "a,b,c,")]
    [DataRow("$foreach(var x in Items)${x},$endfor", "a,b,c,")]
    public void Foreach_StringList_RendersEachItem(string template, string expected)
    {
        // Arrange
        var model = new Order { Items = ["a", "b", "c"] };
        // Act 
        var result = _engine.RenderFromString(template, model);
        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Foreach_StringAsCollection_RendersEachCharacter()
    {
        // Arrange
        var model = new Person { Name = "John" };
        // Act
        var result = _engine.RenderFromString("$foreach(x in Name)${x}$endfor", model);
        // Assert
        Assert.AreEqual("John", result);
    }

    [TestMethod]
    public void Foreach_EmptyCollection_RendersNothing()
    {
        // Arrange
        var model = new Order { Items = new List<string>() };
        var template = "A$foreach(x in Items)[${x}]$endfor B";
        // Act
        var result = _engine.RenderFromString(template, model);
        // Assert
        Assert.AreEqual("A B", result);
    }

    [TestMethod]
    public void Foreach_ObjectList_AccessesProperties()
    {
        // Arrange
        var model = new Cart
        {
            Customers = new List<Person>
            {
                new Person { Name = "Alice", Age = 25 },
                new Person { Name = "John", Age = 30 }
            }
        };
        var template = "$foreach(p in Customers)${p.Name}(${p.Age}) $endfor";

        // Act
        var result = _engine.RenderFromString(template, model);

        // Assert
        Assert.AreEqual("Alice(25) John(30) ", result);
    }

    [TestMethod]
    public void Foreach_AccessOuterVariables_InsideLoop()
    {
        // Arrange
        var model = new Cart
        {
            Owner = "Manager",
            Tags = ["A", "B"]
        };
        var template = "$foreach(t in Tags)${Owner}-${t} $endfor";

        // Act
        var result = _engine.RenderFromString(template, model);
        // Assert
        Assert.AreEqual("Manager-A Manager-B ", result);
    }

    [TestMethod]
    public void Foreach_NestedLoops_RendersCorrectly()
    {
        // Arrange
        var model = new Order { Items = ["a", "b"] };
        // Act
        var template = "$foreach(x in Items)$foreach(y in Items)${x}${y} $endfor$endfor";
        var result = _engine.RenderFromString(template, model);
        // Assert
        Assert.AreEqual("aa ab ba bb ", result);
    }

    [TestMethod]
    public void Foreach_LoopVariable_DoesNotLeakOutside()
    {
        // Arrange
        var model = new Order { Items = ["a", "b"] };
        // Act
        // после $endfor переменная x должна исчезнуть
        var template = "$foreach(x in Items)${x}$endfor ${x}";
        var result = _engine.RenderFromString(template, model);
        // Assert
        Assert.AreEqual("ab ", result); // Вторая ${x} выдаст пустоту, так как контекст цикла разрушен
    }

    [TestMethod]
    public void Foreach_NullElement_RendersEmpty()
    {
        // Arrange
        var model = new Order { Items = ["a", null!, "c"] };
        // Act
        var result = _engine.RenderFromString("$foreach(x in Items)[${x}]$endfor", model);
        // Assert
        Assert.AreEqual("[a][][c]", result);
    }

    [TestMethod]
    public void Foreach_NotACollection_Throws()
    {
        // Arrange
        var model = new Person { IsActive = true };
        // Act
        // Assert
        Assert.Throws<InvalidOperationException>(() =>
            _engine.RenderFromString("$foreach(x in IsActive)${x}$endfor", model));
    }
    
    [TestMethod]
    public void Foreach_MissingEndfor_Throws()
    {
        // Arrange
        var model = new Order { Items = ["a"] };
        // Act
        // Assert
        Assert.Throws<System.Data.SyntaxErrorException>(
            () => _engine.RenderFromString("$foreach(x in Items)${x}", model));
    }
    
    [TestMethod]
    [DataRow("$foreach(x  Items)${x}$endfor")] // Нет "in"
    [DataRow("$foreach(x in)$endfor")] // Нет коллекции
    [DataRow("$foreach(in Items)$endfor")] // Нет переменной
    public void Foreach_InvalidHeader_Throws(string template)
    {
        // Arrange
        var model = new Order { Items = ["a"] };
        // Act
        // Assert
        Assert.Throws<System.Data.SyntaxErrorException>(
            () => _engine.RenderFromString(template, model));
    }
}