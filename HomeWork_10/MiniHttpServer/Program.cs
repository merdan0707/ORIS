using MiniHttpServer.Framework;
using MiniHttpServer.Services.Authentication;

namespace MiniHttpServer;

class Program
{
    static async Task Main(string[] args)
    {
        var sessionService = new UserSessionService();
        await using var server = new HttpServer(sessionService);
        await server.StartAsync();
        
        Console.WriteLine("[APP] Server is running. Type 'stop' to shut down.");
        while (true)
        {
            var command = Console.ReadLine();
            if (command?.ToLower() == "stop")
            {
                break;
            }
        }
        Console.WriteLine("[APP] Shutting down...");
    }
}