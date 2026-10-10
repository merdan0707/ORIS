using System.Net;

namespace MyServer_v3.Framework.Handlers;

public abstract class RequestHandler
{
    private RequestHandler? _successor;

    public RequestHandler SetNext(RequestHandler successor)
    {
        _successor = successor;
        return successor;
    }

    public async Task<bool> HandleAsync(HttpListenerContext context)
    {
        bool handled = await HandleCoreAsync(context);

        if (!handled && _successor != null)
            return await _successor.HandleAsync(context);

        return handled;
    }

    protected abstract Task<bool> HandleCoreAsync(HttpListenerContext context);
}