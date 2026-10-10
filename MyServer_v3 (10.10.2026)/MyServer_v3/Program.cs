using MyServer_v3.Framework.Configuration;
using MyServer_v3.Framework.Http;

namespace MyServer_v3;

class Program
{
    static async Task Main(string[] args)
    {
        await using var server = new HttpServer();
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