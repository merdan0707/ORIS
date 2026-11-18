using MiniHttpServer.HtmlTemplating;

namespace MiniHttpServer.HtmlTemplating.UnitTests;

[TestClass]
public class CombinedTests
{
    private readonly HtmlTemplateRenderer _engine = new();

    [TestMethod]
    public void Combined_IfInsideForeach_FiltersItems()
    {
        // Arrange 
        var model = new { Numbers = new[] { 1, 2, 0, 4, -5, 6 } };
        var template = "$foreach(n in Numbers)$if(n > 2)[${n}]$endif$endfor";

        // Act
        var result = _engine.RenderFromString(template, model);
        // Assert
        Assert.AreEqual("[4][6]", result);
    }

    [TestMethod]
    public void Combined_IfElseInsideForeach_AlternatesOutput()
    {
        // Arrange 
        var model = new
        {
            staff = new List<Person>
            {
                new() { Name = "Bob", IsActive = true },
                new() { Name = "John", IsActive = true },
                new() { Name = "Charlie", IsActive = false }
            }
        };
        var template = "$foreach(x in staff)${x.Name}$if(x.IsActive)(Active)$else(NotActive)$endif $endfor";

        // Act
        var result = _engine.RenderFromString(template, model);
        // Assert
        Assert.AreEqual("Bob(Active) John(Active) Charlie(NotActive) ", result);
    }

    [TestMethod]
    public void Combined_ForeachInsideIf_RendersOnlyIfTrue()
    {
        // Arrange 
        var model = new { Show = false, Items = new[] { "a", "b" } };
        var template = "$if(Show)$foreach(x in Items)${x}$endfor$else Hidden$endif";

        // Act
        var result = _engine.RenderFromString(template, model);
        // Assert
        Assert.AreEqual(" Hidden", result);
    }

    [TestMethod]
    public void Combined_DeeplyNestedObjectsInForeach()
    {
        // Arrange 
        var order1 = new Order { Customer = new Person { Name = "John" } };
        var order2 = new Order { Customer = new Person { Name = "Bob" } };
        var model = new { Orders = new[] { order1, order2 } };
        var template = "$foreach(o in Orders)Order for ${o.Customer.Name}; $endfor";

        // Act
        var result = _engine.RenderFromString(template, model);
        // Assert
        Assert.AreEqual("Order for John; Order for Bob; ", result);
    }

    [TestMethod]
    public void Combined_ComplexConditionInsideForeach()
    {
        // Arrange 
        var items = new[] { 10, 15, 20, 25, 30 };
        var model = new { Items = items, Limit = 18 };
        var template = "$foreach(x in Items)$if(x > Limit && x < 30)[${x}]$endif$endfor";

        // Act
        var result = _engine.RenderFromString(template, model);
        // Assert
        Assert.AreEqual("[20][25]", result);
    }

    [TestMethod]
    public void Combined_NullModel_DoesNotThrow()
    {
        // Arrange 
        var template = 
            "Text ${Name} $if(IsActive)Active $elseNotActive$endif End";

        // Act
        var result = _engine.RenderFromString(template, null!);
        // Assert
        Assert.AreEqual("Text  NotActive End", result);
    }

    [TestMethod]
    public void Combined_EmptyTemplate_WithModel()
    {
        // Arrange 
        var model = new Person { Name = "John" };

        // Act
        var result = _engine.RenderFromString("", model);
        // Assert
        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void Combined_SpecialCharacters_AreNotEscaped()
    {
        // Arrange 
        var model = new Person { Name = "$if(10 == 10)True $elseFalse $endif" };
        var template = "User:${Name}";

        // Act
        var result = _engine.RenderFromString(template, model);
        // Assert
        Assert.AreEqual("User:$if(10 == 10)True $elseFalse $endif", result);
    }
}