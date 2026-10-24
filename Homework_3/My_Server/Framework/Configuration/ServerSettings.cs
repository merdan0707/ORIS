namespace My_Server.Framework.Configuration;

public class ServerSettings
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 8080;
    public string Path { get; set; } = "static";

    public bool IsValid => (Port is > 0 and <= 65535 && !string.IsNullOrWhiteSpace(Host));
}