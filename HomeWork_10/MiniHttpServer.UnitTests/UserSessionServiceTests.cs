using System.Collections.Concurrent;
using MiniHttpServer.Services.Authentication;

namespace MiniHttpServer.UnitTests;

[TestClass]
public class UserSessionServiceTests
{
    private UserSessionService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _service = new UserSessionService();
    }

    [TestMethod]
    public void CreateSession_ReturnsValidSessionId()
    {
        // Act
        string sessionId = _service.CreateSession(1);

        // Assert
        Assert.IsNotNull(sessionId);
        Assert.IsFalse(string.IsNullOrWhiteSpace(sessionId));
        Assert.AreEqual(32, sessionId.Length); // Guid N
    }

    [TestMethod]
    public void GetUserId_ValidSession_ReturnsUserId()
    {
        // Arrange
        int expectedUserId = 42;
        string sessionId = _service.CreateSession(expectedUserId);

        // Act
        int? actualUserId = _service.GetUserId(sessionId);

        // Assert
        Assert.AreEqual(expectedUserId, actualUserId);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("invalid-session-id")]
    public void GetUserId_InvalidOrMissingSession_ReturnsNull(string? sessionId)
    {
        // Act
        int? result = _service.GetUserId(sessionId);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void RemoveSession_MakesSessionInvalid()
    {
        // Arrange
        string sessionId = _service.CreateSession(1);
        Assert.IsNotNull(_service.GetUserId(sessionId)); // Сеанс существует

        // Act
        _service.RemoveSession(sessionId);

        // Assert
        int? result = _service.GetUserId(sessionId);
        Assert.IsNull(result, "Сеанс должен быть удален");
    }

    [TestMethod]
    public void CreateSession_GeneratesUniqueIds()
    {
        // Act
        string session1 = _service.CreateSession(1);
        string session2 = _service.CreateSession(1); // Тот же юзер, но новый сеанс

        // Assert
        Assert.AreNotEqual(session1, session2);
    }
    
    [TestMethod]
    public void RemoveSession_NonExistentSession_DoesNotThrow()
    {
        // Act
        // Assert
        // не должно падать
        _service.RemoveSession("non-existent-id"); 
        _service.RemoveSession(null);
    }
    
    [TestMethod]
    public void CreateAndRemoveSession_ConcurrentAccess_RemainsConsistent()
    {
        // Arrange
        var generatedIds = new ConcurrentBag<string>();
        const int operationsCount = 1000;

        // Act
        // Параллельное создание сессий
        Parallel.For(0, operationsCount, i =>
        {
            string id = _service.CreateSession(i);
            generatedIds.Add(id);
        });

        // Assert
        // все сгенерированные ID уникальны (нет коллизий)
        Assert.AreEqual(operationsCount, generatedIds.Distinct().Count(), "Количество уникальных сессий должно совпадать с числом операций");
        
        // Act
        // Параллельное удаление сессий
        Parallel.ForEach(generatedIds, id =>
        {
            _service.RemoveSession(id);
        });

        // Assert 
        // Assert лучше делать в обычном цикле, чтобы MSTest корректно поймал ошибку и указал на конкретный ID.
        foreach (var id in generatedIds)
        {
            Assert.IsNull(_service.GetUserId(id), $"Сессия с ID '{id}' должна быть удалена, но GetUserId вернул значение");
        }
    }
}