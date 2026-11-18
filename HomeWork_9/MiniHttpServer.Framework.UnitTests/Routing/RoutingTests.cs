using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Controllers;
using MiniHttpServer.Framework.Results;
using MiniHttpServer.Framework.Routing;

namespace MiniHttpServer.Framework.UnitTests.Routing;

// Фиктивные контроллеры для сканирования
[HttpController]
public class TestApiController : BaseController
{
    [HttpGet]
    public IHttpResult GetAction() => null!;
    
    [HttpPost]
    public IHttpResult PostAction([FromQuery] int id, [FromForm] string name) => null!;
}

[TestClass]
public class RoutingTests
{
    [TestMethod]
    public void RouteScanner_ScansAssembly_FindsCorrectRoutes()
    {
        // Arrange
        var scanner = new RouteScanner();

        // Act
        var routes = scanner.Scan(typeof(TestApiController).Assembly);

        // Assert
        Assert.IsTrue(routes.ContainsKey("api/testapi/getaction"), "GET маршрут не найден");
        Assert.IsTrue(routes.ContainsKey("api/testapi/postaction"), "POST маршрут не найден");
        Assert.AreEqual("GET", routes["api/testapi/getaction"].HttpMethod);
        Assert.AreEqual("POST", routes["api/testapi/postaction"].HttpMethod);
    }

    [TestMethod]
    public void ParameterBinder_QueryAndFormParameters_BindsCorrectly()
    {
        // Arrange
        var instructions = new List<ParameterInstruction>
        {
            new("id", typeof(int), ParameterSource.Query, false, null),
            new("name", typeof(string), ParameterSource.Form, false, null)
        };
        var query = System.Web.HttpUtility.ParseQueryString("id=42");
        var formData = new Dictionary<string, string> { { "name", "John" } };

        // Act
        var result = ParameterBinder.Bind(instructions, query, formData, null);

        // Assert
        Assert.HasCount(2, result);
        Assert.AreEqual(42, result[0]);
        Assert.AreEqual("John", result[1]);
    }

    [TestMethod]
    public void ParameterBinder_MissingRequiredParameter_ThrowsArgumentException()
    {
        // Arrange
        var instructions = new List<ParameterInstruction>
        {
            new("id", typeof(int), ParameterSource.Query, false, null)
        };
        var query = System.Web.HttpUtility.ParseQueryString(""); 

        // Act
        // Assert
        Assert.Throws<ArgumentException>(() => 
            ParameterBinder.Bind(instructions, query, new Dictionary<string, string>(), null));
    }

    [TestMethod]
    public void ParameterBinder_InvalidTypeConversion_ThrowsFormatException()
    {
        // Arrange
        var instructions = new List<ParameterInstruction>
        {
            new("id", typeof(int), ParameterSource.Query, false, null)
        };
        var query = System.Web.HttpUtility.ParseQueryString("id=not-a-number");

        // Act
        // Assert
        Assert.Throws<FormatException>(() => 
            ParameterBinder.Bind(instructions, query, new Dictionary<string, string>(), null));
    }
}