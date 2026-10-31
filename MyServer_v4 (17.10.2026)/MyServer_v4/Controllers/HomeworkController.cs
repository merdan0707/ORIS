
using MimeKit;
using MailKit.Net.Smtp;
using MyServer_v4.Framework.Attributes;

namespace MyServer_v4.Controllers;

[HttpController]
[Route("homework")]
public class HomeworkController
{
    [HttpGet]
    [Route("send")]
    public async Task<string> Send([FromQuery] string to)
    {
        try
        {
            // добавь логин и пароль !!!
            string myEmail = "your-email@email.com";
            string appPassword = "your-password";
            string fullName = "your-full-name";
            string group = "your-group";
            
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress($"{fullName}, группа: {group}", myEmail));
            message.To.Add(new MailboxAddress("Teacher", to));
            message.Subject = "Homework";
            var bodyBuilder = new BodyBuilder();

            // добавь ссылку диска !!!
            var linkToZip = "https://your-link-to-disk";
            bodyBuilder.TextBody = $"Здравствуйте!\n\nНиже находится ссылка на диск где расположен zip-архив с выполненной домашней работой. {linkToZip}\n" +
                                   $"\n\nГруппа: {group}\nФИО: {fullName}";
            
            message.Body = bodyBuilder.ToMessageBody();
            
            using var client = new SmtpClient();

            client.CheckCertificateRevocation = false;
            
            await client.ConnectAsync("smtp.yandex.ru", 465, MailKit.Security.SecureSocketOptions.SslOnConnect);
            await client.AuthenticateAsync(myEmail, appPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            Console.WriteLine("[EMAIL] Успешно отправлено");
            return "[EMAIL] Успешно отправлено";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EMAIL ERROR] {ex.Message}");
            return "[EMAIL ERROR]";
        }
    }
}