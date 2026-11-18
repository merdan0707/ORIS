using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Authentication;

namespace MiniHttpServer.Framework.UnitTests.Authentication;

[TestClass]
public class AuthorizeAttributeTests
{
    // Фейковый сервис
    private class FakeSessionService : IUserSessionService
    {
        public string CreateSession(int userId) => "fake-session";
        public int? GetUserId(string? sessionId) => sessionId == "fake-session" ? 1 : null;

        public void RemoveSession(string? sessionId)
        {
        }
    }

    // Фиктивный контроллер для проверки атрибута
    private class TestController
    {
        [Authorize]
        public void ProtectedMethod()
        {
        }

        public void PublicMethod()
        {
        }
    }

    [TestMethod]
    public void MethodWithAuthorize_HasAttribute()
    {
        // Arrange
        var method = typeof(TestController).GetMethod(nameof(TestController.ProtectedMethod));

        // Act
        bool hasAttribute = method!.IsDefined(typeof(AuthorizeAttribute), true);

        // Assert
        Assert.IsTrue(hasAttribute);
    }

    [TestMethod]
    public void MethodWithoutAuthorize_DoesNotHaveAttribute()
    {
        // Arrange
        var method = typeof(TestController).GetMethod(nameof(TestController.PublicMethod));

        // Act
        bool hasAttribute = method!.IsDefined(typeof(AuthorizeAttribute), true);

        // Assert
        Assert.IsFalse(hasAttribute);
    }
}