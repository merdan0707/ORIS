using MyServer_v4.Framework.Handlers;
using MyServer_v4.Framework.Attributes;

namespace MyServer_v4.Controllers;

[HttpController]
public class AuthController
{
    [HttpPost]
    public string Login([FromForm] string email, [FromForm] string password)
    {
        Console.WriteLine($"Email: {email}, Password: {password}");
        return $"Login successful for {email}";
    }

    [HttpGet]
    public string GetUser([FromQuery] int id)
    {
        Console.WriteLine($"[GET USER] ID: {id}");
        return $"User with ID {id} found";
    }
}