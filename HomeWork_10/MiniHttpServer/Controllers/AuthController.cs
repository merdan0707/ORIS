using System.Net;
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
        const string testLogin = "student";
        const string testPassword = "test123";

        if (email != testLogin || password != testPassword)
        {
            return Json(new { Success = false, Message = "Неверный логин или пароль" }, 401);
        }

        int userId = 1;
        string sessionId = SessionService.CreateSession(userId);
        
        // отправляем sessionId клиенту в куке
        var cookie = new Cookie("sessionId", sessionId, "/")
        {
            HttpOnly = true,
            Expires = DateTime.UtcNow.AddMinutes(30)
        };
        
        Context.Response.SetCookie(cookie);
        
        return Json(new { Success = true, message = "Вход выполнен" });
    }

    [HttpPost]
    public IHttpResult Logout()
    {
        string? sessionId = Context.Request.Cookies["sessionId"]?.Value;
        
        SessionService.RemoveSession(sessionId);

        var cookie = new Cookie("sessionId", string.Empty, "/")
        {
            HttpOnly = true,
            Secure = Context.Request.IsSecureConnection,
            Expires = DateTime.UtcNow.AddDays(-1)
        };
        
        Context.Response.SetCookie(cookie);
        
        return Json(new { Success = true, Message = "Выход выполнен" });
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