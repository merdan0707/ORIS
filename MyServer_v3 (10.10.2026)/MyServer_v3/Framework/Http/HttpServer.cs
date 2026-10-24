using System.Net;
using MyServer_v3.Framework.Configuration;
using MyServer_v3.Framework.Handlers;

namespace MyServer_v3.Framework.Http;

public class HttpServer : IAsyncDisposable
{
    private readonly HttpListener _listener;
    private readonly RequestHandler _chainRoot;
    private CancellationTokenSource? _cts;
    private bool _isRunning;

    public HttpServer()
    {
        var config = ConfigService.Instance.Settings;
        var prefix = $"http://{config.Host}:{config.Port}/";

        _listener = new HttpListener();
        _listener.Prefixes.Add(prefix);

        var apiHandler = new ApiControllerHandler();
        var staticHandler = new StaticFileHandler(config.Path);
        var notFoundHandler = new NotFoundHandler(config.Path);

        _chainRoot = apiHandler;
        apiHandler.SetNext(staticHandler).SetNext(notFoundHandler);

        Console.WriteLine("[CHAIN] Built: API -> Static -> NotFound");
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning) return;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _listener.Start();
        _isRunning = true;

        Console.WriteLine($"[SERVER] Started: {_listener.Prefixes.First()}");

        _ = ListenLoopAsync(_cts.Token);
    }

    private async Task ListenLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var context = await _listener.GetContextAsync().WaitAsync(token);
                _ = HandleRequestAsync(context, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (HttpListenerException) when (_isRunning)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Getting context: {ex.Message}");
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken token)
    {
        try
        {
            Console.WriteLine($"[REQUEST] {context.Request.HttpMethod} {context.Request.Url.LocalPath}");

            await _chainRoot.HandleAsync(context).WaitAsync(token);
            Console.WriteLine($"[RESPONSE] {context.Response.StatusCode}");
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Handling request: {ex.Message}");
            try
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.Close();
            }
            catch
            {
                //ignore
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_isRunning) return;

        _isRunning = false;
        if (_cts != null)
        {
            await _cts.CancelAsync();
            _cts.Dispose();
        }
        
        _listener.Stop();
        _listener.Close();

        Console.WriteLine("[SERVER] Stopped.");
    }
}