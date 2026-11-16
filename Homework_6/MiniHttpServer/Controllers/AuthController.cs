using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Controllers;
using MiniHttpServer.Framework.Results;
using MiniHttpServer.Services;

namespace MiniHttpServer.Controllers;

[HttpController]
public class AuthController : BaseController
{
    private readonly IEmailService _emailService = new EmailService();

    [HttpPost]
    public IHttpResult Login([FromForm] string email, [FromForm] string password)
    {
        Console.WriteLine($"[AUTH] Login attempt: {email}");
        return Json(new { success = true, message = $"Login successful for {email}" });
    }
    
    [HttpGet]
    public IHttpResult GetUser([FromQuery] int id)
    {
        return Json(new { success = true, id, message = $"User with ID {id} found" });
    }
    
    [HttpGet]
    public async Task<IHttpResult> SendEmail([FromQuery] string to)
    {
        bool sent = await _emailService.SendHomeworkEmailAsync(to);
        return sent
            ? Json(new { success = true, message = "Сообщение отправлено" })
            : Json(new { success = false, message = "Не удалось отправить сообщение" }, 500);
    }
}