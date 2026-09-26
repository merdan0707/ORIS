using System.Net;
using System.Text;
using System.Text.Json;

namespace My_Server;

public class HttpServer
{
    private HttpListener _server = new HttpListener();
    private bool IsListening;

    public async Task Start()
    {
        try
        {
            Server externalServer = new Server();
            string json = File.ReadAllText("settings.json");
            externalServer.settings = JsonSerializer.Deserialize<Settings>(json);

            string prefix =
                $"http://{externalServer.settings?.Host}:{externalServer.settings?.Port}/{externalServer.settings?.Path}";
            _server.Prefixes.Add(prefix);
            _server.Start();
            IsListening = true;
            Console.WriteLine($"Сервер успешно запущен на {prefix}");

            _ = ListenAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    private async Task ListenAsync()
    {
        while (IsListening)
        {
            try
            {
                var context = await _server.GetContextAsync();
                var response = context.Response;

                string responseText = File.ReadAllText("index.html");
                byte[] buffer = Encoding.UTF8.GetBytes(responseText);

                response.ContentType = "text/html; charset=utf-8";
                response.ContentLength64 = buffer.Length;
                await using Stream output = response.OutputStream;
                await output.WriteAsync(buffer);
                await output.FlushAsync();

                Console.WriteLine("Запрос обработан");
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
            }
        }
    }

    public async Task Stop()
    {
        if (!IsListening)
            return;
        IsListening = false;
        _server.Stop();
        _server.Close();
        Console.WriteLine("Server stopped");
    }
}