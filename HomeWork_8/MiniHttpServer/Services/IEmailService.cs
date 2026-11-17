namespace MiniHttpServer.Services;

public interface IEmailService
{
    Task<bool> SendHomeworkEmailAsync(string to);
}