using System.Net;
using System.Text;
using System.Text.Json;

namespace My_Server;

using System.Net;
using System.Text;

class Program
{
    static async Task Main(string[] args)
    {
        HttpServer server = new HttpServer();
        server.Start();

        Task.Delay(500).Wait();
        while (true)
        {
            Console.WriteLine("Input 'close' to stop the server...");
            var input = Console.ReadLine();
            if (input == "close")
            {
                await server.Stop();
                break;
            }
        }
    }
}