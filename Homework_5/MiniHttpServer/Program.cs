using MimeKit;
using MailKit.Net.Smtp;
using MiniHttpServer.Framework;
using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Configuration;

namespace MiniHttpServer;

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