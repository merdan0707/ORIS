using System.Net;
using MiniHttpServer.Framework.Handlers;

namespace MiniHttpServer.Framework.UnitTests.Handlers;

// тестовый обработчик
public class TestHandler : RequestHandler
{
    private readonly bool _canHandle;

    public bool WasCalled { get; private set; }

    public TestHandler(bool canHandle)
    {
        _canHandle = canHandle;
    }

    protected override Task<bool> HandleCoreAsync(HttpListenerContext context)
    {
        WasCalled = true;
        return Task.FromResult(_canHandle); // true или false
    }
}

[TestClass]
public class HandlerChainTests
{
    [TestMethod]
    public async Task HandleAsync_FirstHandlerCanHandle_DoesNotPassToNext()
    {
        // Arrange
        var handler1 = new TestHandler(canHandle: true);
        var handler2 = new TestHandler(canHandle: true);
        handler1.SetNext(handler2);
        // фейковый контекст для обработчика
        var context = CreateFakeContext();

        // Act
        var result = await handler1.HandleAsync(context);

        // Assert
        Assert.IsTrue(result, "Цепочка должна вернуть true, так как первый обработчик справился.");
        Assert.IsTrue(handler1.WasCalled, "Первый обработчик должен был быть вызван.");
        Assert.IsFalse(handler2.WasCalled, "Второй обработчик не должен вызываться, если первый вернул true.");
    }

    [TestMethod]
    public async Task HandleAsync_FirstHandlerCannotHandle_PassesToNext()
    {
        // Arrange
        var handler1 = new TestHandler(canHandle: false);
        var handler2 = new TestHandler(canHandle: true);  
        handler1.SetNext(handler2);
        var context = CreateFakeContext();

        // Act
        var result = await handler1.HandleAsync(context);

        // Assert
        Assert.IsTrue(result, "Цепочка должна вернуть true так как второй обработчик справился.");
        Assert.IsTrue(handler1.WasCalled, "Первый обработчик должен был быть вызван.");
        Assert.IsTrue(handler2.WasCalled, "Второй обработчик должен быть вызван так как первый вернул false.");
    }

    [TestMethod]
    public async Task HandleAsync_NoHandlersCanHandle_ReturnsFalse()
    {
        // Arrange
        var handler1 = new TestHandler(canHandle: false);
        var handler2 = new TestHandler(canHandle: false);
        handler1.SetNext(handler2);
        var context = CreateFakeContext();

        // Act
        var result = await handler1.HandleAsync(context);

        // Assert
        Assert.IsFalse(result, "Цепочка должна вернуть false если никто не смог обработать запрос.");
        Assert.IsTrue(handler1.WasCalled);
        Assert.IsTrue(handler2.WasCalled);
    }

    // вспомогательный метод для создания контекста
    private HttpListenerContext CreateFakeContext()
    {
        // можно просто передать null!, так как TestHandler не обращается к context
        return null!; 
    }
}