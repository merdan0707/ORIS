using System.Net;

namespace Controlwork_1.HttpResults.Abstractions;

public interface IActionResult
{
    Task ExecuteAsync(HttpListenerContext context);
}