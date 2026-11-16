using MimeKit;
using MailKit.Net.Smtp;

namespace MiniHttpServer.Services;

public class EmailService : IEmailService
{
    private readonly string _myEmail = "your-email@email.com";
    private readonly string _appPassword = "your-password"; // надо пароль приложения
    private readonly string _fullName = "your-full-name";
    private readonly string _group = "your-group";
    private readonly string _linkToZip = "https://your-link-to-disk";

    public async Task<string> SendHomeworkEmailAsync(string to)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress($"{_fullName}, группа: {_group}", _myEmail));
            message.To.Add(new MailboxAddress("Teacher", to));
            message.Subject = "Homework";
            
            var bodyBuilder = new BodyBuilder
            {
                TextBody = $"Здравствуйте!\n\nНиже находится ссылка на диск где расположен zip-архив с выполненной домашней работой. {_linkToZip}\n" +
                           $"\n\nГруппа: {_group}\nФИО: {_fullName}"
            };
            
            message.Body = bodyBuilder.ToMessageBody();
            
            using var client = new SmtpClient();
            client.CheckCertificateRevocation = false;
            
            await client.ConnectAsync("smtp.yandex.ru", 465, MailKit.Security.SecureSocketOptions.SslOnConnect);
            await client.AuthenticateAsync(_myEmail, _appPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            
            Console.WriteLine("[EMAIL] Успешно отправлено");
            return "[EMAIL] Успешно отправлено";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EMAIL ERROR] {ex.Message}");
            return $"[EMAIL ERROR]: {ex.Message}";
        }
    }
}