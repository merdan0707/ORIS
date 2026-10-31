using MimeKit;
using MailKit.Net.Smtp;
using MyServer_v4.Framework.Attributes;
using MyServer_v4.Framework.Configuration;
using MyServer_v4.Framework.Http;

namespace MyServer_v4;

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