namespace MiniHttpServer.Services;

public interface IEmailService
{
    Task<string> SendHomeworkEmailAsync(string to);
}