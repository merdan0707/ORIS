using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Services;

namespace MiniHttpServer.Controllers;

[HttpController]
[Route("homework")]
public class TestController
{
    [HttpGet]
    [Route("send")]
    public async Task<string> Send([FromQuery] string to)
    {
        IEmailService emailService = new EmailService();
        return await emailService.SendHomeworkEmailAsync(to);
    }
}