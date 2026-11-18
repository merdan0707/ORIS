namespace MiniHttpServer.HtmlTemplating.UnitTests;

[TestClass]
public class ArithmeticTests
{
    // экземпляр движка для всех тестов
    private readonly HtmlTemplateRenderer _engine = new();
    // экземпляр модели для сравнения в тестах
    private readonly Person _model = new() { Age = 20 }; 
    
    [TestMethod]
    [DataRow("$if(1 + 1 == 2)Yes$endif", "Yes")]
    [DataRow("$if(5 - 3 == 2)Yes$endif", "Yes")]
    [DataRow("$if(2 * 3 == 6)Yes$endif", "Yes")]
    [DataRow("$if(10 / 2 == 5)Yes$endif", "Yes")]
    [DataRow("$if(10 % 3 == 1)Yes$endif", "Yes")]
    [DataRow("$if(Age + 5 == 25)Yes$endif", "Yes")]
    public void If_BasicArithmetic_EvaluatesCorrectly(string template, string expected)
    {
        // Arrange
        // Act
        var result = _engine.RenderFromString(template, _model);
        // Assert
        Assert.AreEqual(expected, result);
    }
    
    [TestMethod]
    [DataRow("$if(-5 + 10 == 5)Yes$endif", "Yes")] 
    [DataRow("$if(5 - -5 == 10)Yes$endif", "Yes")] 
    [DataRow("$if(2 + 3 * 4 == 14)Yes$endif", "Yes")] 
    [DataRow("$if(10 - 3 - 2 == 5)Yes$endif", "Yes")] // (10-3)-2 = 5
    [DataRow("$if(12 / 3 / 2 == 2)Yes$endif", "Yes")] // (12/3)/2 = 2
    public void If_ArithmeticPriority_EvaluatesCorrectly(string template, string expected)
    {
        // Arrange
        // Act
        var result = _engine.RenderFromString(template, _model);
        // Assert
        Assert.AreEqual(expected, result);
    }
    
    [TestMethod]
    [DataRow("$if(1 / 0 > 1)Yes$endif", "")] // деление на 0 -> null -> false
    [DataRow("$if(1 % 0 == 0)Yes$endif", "")] // остаток на 0 -> null -> false
    [DataRow("$if(\"text\" + 5 > 0)Yes$endif", "")] // сложить строку и число -> null -> false
    public void If_ArithmeticErrors_TreatedAsFalse(string template, string expected)
    {
        // Arrange
        // Act
        var result = _engine.RenderFromString(template, _model);
        // Assert
        Assert.AreEqual(expected, result);
    }
}