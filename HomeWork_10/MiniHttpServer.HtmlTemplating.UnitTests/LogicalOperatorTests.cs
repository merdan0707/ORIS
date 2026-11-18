using MiniHttpServer.HtmlTemplating;

namespace MiniHttpServer.HtmlTemplating.UnitTests;

[TestClass]
public class LogicalOperatorTests
{
    // экземпляр движка для всех тестов
    private readonly HtmlTemplateRenderer _engine = new();
    // экземпляр модели для сравнения в тестах
    private readonly Person _model = new() { Age = 20, Name = "John", IsActive = true };
    
    [TestMethod]
    [DataRow("$if(true && true)Yes$endif", "Yes")]
    [DataRow("$if(true && false)Yes$endif", "")]
    [DataRow("$if(false || true)Yes$endif", "Yes")]
    [DataRow("$if(false || false)Yes$endif", "")]
    [DataRow("$if(IsActive && Age > 18)Yes$endif", "Yes")]
    [DataRow("$if(IsActive || Age < 18)Yes$endif", "Yes")]
    public void If_LogicalAndOr_EvaluatesCorrectly(string template, string expected)
    {
        // Arrange
        // Act
        var result = _engine.RenderFromString(template, _model);
        // Assert
        Assert.AreEqual(expected, result);
    }
    
    [TestMethod]
    [DataRow("$if(!false)Yes$endif", "Yes")]
    [DataRow("$if(!true)Yes$endif", "")]
    [DataRow("$if(!IsActive)Yes$endif", "")]
    [DataRow("$if(!Unknown)Yes$endif", "Yes")] // !null -> true
    [DataRow("$if(!!IsActive)Yes$endif", "Yes")] // двойное отрицание
    public void If_LogicalNot_EvaluatesCorrectly(string template, string expected)
    {
        // Arrange
        // Act
        var result = _engine.RenderFromString(template, _model);
        // Assert
        Assert.AreEqual(expected, result);
    }
    
    [TestMethod]
    [DataRow("$if(true || false && false)Yes$endif", "Yes")] // true || (false && false)
    [DataRow("$if(false && true || true)Yes$endif", "Yes")]  // (false && true) || true
    [DataRow("$if(Age > 10 && Age < 30 || Name == 'Jane')Yes$endif", "Yes")]
    public void If_OperatorPriority_EvaluatesCorrectly(string template, string expected)
    {
        // Arrange
        // Act
        var result = _engine.RenderFromString(template, _model);
        // Assert
        Assert.AreEqual(expected, result);
    }
    
    [TestMethod]
    public void If_DivisionByZero_DoesNotThrowReturnFalse()
    {
        // Arrange
        // Act
        var result = _engine.RenderFromString("$if(false && 1/0 > 1)Yes$endif", _model);
        // Assert
        Assert.AreEqual("", result);
    }
}