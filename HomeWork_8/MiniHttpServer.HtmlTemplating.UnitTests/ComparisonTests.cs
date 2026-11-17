using MiniHttpServer.HtmlTemplating;

namespace MiniHttpServer.HtmlTemplating.UnitTests;

[TestClass]
public class ComparisonTests
{
    // экземпляр движка для всех тестов
    private readonly HtmlTemplateRenderer _engine = new();
    // экземпляр модели для сравнения в тестах
    private readonly Person _model = new Person { Age = 20, Name = "John", IsActive = true };

    [TestMethod]
    [DataRow("$if(20 == 20)Yes$endif", "Yes")]
    [DataRow("$if(20 == 21)Yes$endif", "")]
    [DataRow("$if(Age == 20)Yes$endif", "Yes")]
    [DataRow("$if(Age != 20)Yes$endif", "")]
    [DataRow("$if(20 != 21)Yes$endif", "Yes")]
    [DataRow("$if(Age > 18)Yes$endif", "Yes")]
    [DataRow("$if(Age < 18)Yes$endif", "")]
    [DataRow("$if(Age >= 20)Yes$endif", "Yes")]
    [DataRow("$if(Age <= 19)Yes$endif", "")]
    public void If_NumericComparisons_EvaluatesCorrectly(string template, string expected)
    {
        // Arrange 
        // Act
        var result = _engine.RenderFromString(template, _model);
        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("$if(Name == \"John\")Yes$endif", "Yes")]
    [DataRow("$if(Name == 'John')Yes$endif", "Yes")]
    [DataRow("$if(Name == \"Jane\")Yes$endif", "")]
    [DataRow("$if(Name != \"Jane\")Yes$endif", "Yes")]
    [DataRow("$if(\"apple\" < \"banana\")Yes$endif", "Yes")]
    [DataRow("$if(\"apple\" > \"banana\")Yes$endif", "")]
    [DataRow("$if('' == '')Yes$endif", "Yes")]
    [DataRow("$if(\"\" == \"\")Yes$endif", "Yes")]
    public void If_StringComparisons_EvaluatesCorrectly(string template, string expected)
    {
        // Arrange
        // Act
        var result = _engine.RenderFromString(template, _model);
        // Assert
        Assert.AreEqual(expected, result);
    }
    
    [TestMethod]
    [DataRow("$if(IsActive == true)Yes$endif", "Yes")]
    [DataRow("$if(IsActive != true)Yes$endif", "")]
    [DataRow("$if(IsActive == false)Yes$endif", "")]
    [DataRow("$if(IsActive != false)Yes$endif", "Yes")]
    [DataRow("$if(Unknown == null)Yes$endif", "Yes")] // Unknown ищется в модели, не находится -> null
    [DataRow("$if(Unknown != null)Yes$endif", "")]
    [DataRow("$if(Unknown == '')Yes$endif", "")]
    [DataRow("$if(null == '')Yes$endif", "")]
    [DataRow("$if( == '')Yes$endif", "")]
    public void If_BooleanAndNullComparisons_EvaluatesCorrectly(string template, string expected)
    {
        // Arrange
        // Act
        var result = _engine.RenderFromString(template, _model);
        // Assert
        Assert.AreEqual(expected, result);
    }
}