using My_Server.Framework.Http;

namespace My_Server;

class Program
{
    static async Task Main(string[] args)
    {
        await using var server = new HttpServer();
        await server.StartAsync();

        Console.WriteLine("Enter \"close\" to stop the server...");
        while (Console.ReadLine()?.ToLower() != "close") 
        { /*Ignore*/ }
    }
}