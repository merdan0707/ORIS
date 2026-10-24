using MyServer_v3.Framework.Attributes;
using MyServer_v3.Framework.Handlers;

namespace MyServer_v3.Controllers;

[HttpController]
public class AuthController
{
    [HttpPost]
    public string Login(string email, string password)
    {
        Console.WriteLine($"Email: {email}, Password: {password}");
        return $"Login successful for {email}";
    }

    [HttpGet]
    public string GetUser(int id)
    {
        Console.WriteLine($"[GET USER] ID: {id}");
        return $"User with ID {id} found";
    }
}