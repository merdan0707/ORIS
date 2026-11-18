using MimeKit;
using MailKit.Net.Smtp;
using MiniHttpServer.Framework.Configuration;

namespace MiniHttpServer.Services;

public class EmailService : IEmailService
{
    public async Task<bool> SendHomeworkEmailAsync(string to)
    {
        var smtp = ConfigurationManager.Instance.Settings.Smtp;
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress($"{smtp.FullName}, группа: {smtp.Group}", smtp.Email));
            message.To.Add(new MailboxAddress("Teacher", to));
            message.Subject = "Homework";
            var bodyBuilder = new BodyBuilder
            {
                TextBody = $"Здравствуйте!\n\nНиже находится ссылка на диск где расположен zip-архив с выполненной домашней работой. {smtp.LinkToZip}\n" +
                           $"\n\nГруппа: {smtp.Group}\nФИО: {smtp.FullName}"
            };
            
            message.Body = bodyBuilder.ToMessageBody();
            
            using var client = new SmtpClient();
            client.CheckCertificateRevocation = false;
            
            await client.ConnectAsync(smtp.Host, smtp.Port, MailKit.Security.SecureSocketOptions.SslOnConnect);
            await client.AuthenticateAsync(smtp.Email, smtp.AppPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            
            Console.WriteLine("[EMAIL] Успешно отправлено");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EMAIL ERROR] {ex}");
            return false;
        }
    }
}