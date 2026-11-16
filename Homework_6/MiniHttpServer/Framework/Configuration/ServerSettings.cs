namespace MiniHttpServer.Framework.Configuration;

public class ServerSettings
{
    public string Host { get; set; } = "127.0.1.1";
    public int Port { get; set; } = 8888;
    public string Path { get; set; } = "static/";
    public SmtpSettings Smtp { get; set; } = new();

    public bool IsValid => Port is > 0 and <= 65535
                           && !string.IsNullOrEmpty(Host);
}

public class SmtpSettings
{
    public string Host { get; set; } = "smtp.yandex.ru";
    public int Port { get; set; } = 465;
    public string Email { get; set; } = "";
    public string AppPassword { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Group { get; set; } = "";
    public string LinkToZip { get; set; } = "";
}