using MiniHttpServer.HtmlTemplating;

namespace MiniHttpServer.HtmlTemplating.UnitTests;

[TestClass]
public class IfStatementTests
{
    // экземпляр движка для всех тестов
    private readonly HtmlTemplateRenderer _engine = new();

    // [TestMethod]
    // public void If_TrueCondition_RendersBody()
    // {
    //     // Arrange 
    //     // Act
    //     var model = new Person();
    //     var result = _engine.RenderFromString("$if(true)Yes$endif", model);
    //     // Assert
    //     Assert.AreEqual("Yes", result);
    // }
    [TestMethod]
    [DataRow("$if(true)Yes$endif", "Yes")]
    [DataRow("$if(false)Yes$endif", "")]
    public void If_BooleanCondition_RendersBodyOrEmpty(string template, string expected)
    {
        // Arrange 
        // Act
        var model = new Person();
        var result = _engine.RenderFromString(template, model);
        // Assert
        Assert.AreEqual(expected, result);
    }
    
    [TestMethod]
    public void If_PropertyTrue_RendersBody()
    {
        // Arrange 
        // Act
        var model = new Person { IsActive = true };
        var template = "$if(IsActive)Active$endif";
        var result = _engine.RenderFromString(template, model);
        // Assert
        Assert.AreEqual("Active", result);
    }
    
    [TestMethod]
    public void If_PropertyFalse_RendersEmpty()
    {
        // Arrange 
        // Act
        var model = new Person { IsActive = false };
        var template = "$if(IsActive)Active$endif";
        var result = _engine.RenderFromString(template, model);
        // Assert
        Assert.AreEqual("", result);
    }
    
    [TestMethod]
    public void If_MissingVariable_TreatedAsFalse()
    {
        // Arrange 
        // Act
        var model = new Person { IsActive = true, Age = 22 };
        var template = "$if(UnknownVar)Yes$endif";
        var result = _engine.RenderFromString(template, model);
        // Assert
        Assert.AreEqual("", result);
    }
    
    [TestMethod]
    public void IfElse_TrueCondition_RendersIfBranch()
    {
        // Arrange 
        // Act
        var model = new Person();
        var template = "$if(true)Yes$else No$endif";
        var result = _engine.RenderFromString(template, model);
        // Assert
        Assert.AreEqual("Yes", result);
    }

    [TestMethod]
    public void IfElse_FalseCondition_RendersElseBranch()
    {
        // Arrange 
        // Act
        var model = new Person ();
        var template = "$if(false)Yes$elseNo$endif";
        var result = _engine.RenderFromString(template, model);
        // Assert
        Assert.AreEqual("No", result);
    }

    [TestMethod]
    public void NestedIf_BothTrue_RendersBoth()
    {
        // Arrange 
        // Act
        var model = new Person();
        var template = "A $if(true)B $if(true)C$endif$endif";
        var result = _engine.RenderFromString(template, model);
        // Assert
        Assert.AreEqual("A B C", result);
    }
    
    [TestMethod]
    public void NestedIf_InnerElse_BelongsToInnerBlock()
    {
        // Arrange 
        // Act
        var model = new Person();
        var template = "$if(true)Outer $if(false)Inner$else InnerElse$endif$endif";
        // Внешний true, внутренний false. Должен выполниться inner else
        var result = _engine.RenderFromString(template, model);
        // Assert
        Assert.AreEqual("Outer  InnerElse", result);
    }

    [TestMethod]
    public void NestedIf_OuterElse_SkipsInnerBlockEntirely()
    {
        // Arrange 
        // Act
        var model = new Person();
        var template = "$if(false)Outer $if(true)Inner$else InnerElse$endif$else OuterElse$endif";
        // Внешний false. Внутренний $if не должен анализироваться
        var result = _engine.RenderFromString(template, model);
        // Assert
        Assert.AreEqual(" OuterElse", result);
    }
    
    [TestMethod]
    [DataRow("hello", "Yes")] // непустая строка -> true
    [DataRow("", "")] // пустая строка -> false
    [DataRow(0, "")] // ноль -> false
    [DataRow(42, "Yes")] // число != 0 -> true
    [DataRow(-5, "Yes")] // отрицательное число != 0 -> true
    [DataRow(null, "")] // null -> false
    public void If_Truthiness_Primitives(object value, string expected)
    {
        // Arrange 
        // Act
        var model = new { Value = value };
        var template = "$if(Value)Yes$endif";
        var result = _engine.RenderFromString(template, model);
        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void If_EmptyCollection_RendersEmpty()
    {
        // Arrange
        // Act
        var model = new Order { Items = new List<string>() };
        var template = "$if(Items)HasItems$endif";
        var result = _engine.RenderFromString(template, model);
        // Assert
        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void If_NonEmptyCollection_RendersBody()
    {
        // Arrange
        // Act
        var model = new Order { Items = new List<string> { "John" } };
        var template = "$if(Items)HasItems$endif";
        var result = _engine.RenderFromString(template, model);
        // Assert
        Assert.AreEqual("HasItems", result);
    }

    [TestMethod]
    public void If_NullObjectProperty_RendersEmpty()
    {
        // Arrange
        // Act
        var model = new NullableModel { User = null };
        var template = "$if(User)HasUser$endif";
        var result = _engine.RenderFromString(template, model);
        // Assert
        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void If_NonNullObjectProperty_RendersBody()
    {
        // Arrange
        // Act
        var model = new Order { Customer = new Person() };
        var template = "$if(Customer)HasCustomer$endif";
        var result = _engine.RenderFromString(template, model);
        // Assert
        Assert.AreEqual("HasCustomer", result);
    }

    [TestMethod]
    public void If_MissingEndif_Throws()
    {
        // Arrange
        var model = new Person();
        var template = "$if(true)Yes";
        
        // Act
        // Assert
        Assert.Throws<System.Data.SyntaxErrorException>(
            () => _engine.RenderFromString(template, model));
    }

    [TestMethod]
    public void If_MissingClosingBracket_Throws()
    {
        // Arrange
        var model = new Person();
        var template = "$if(true Yes$endif";
        
        // Act
        // Assert
        Assert.Throws<System.Data.SyntaxErrorException>(
            () => _engine.RenderFromString(template, model));
    }
}