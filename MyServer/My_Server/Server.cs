namespace My_Server;

public class Server
{
    public Settings? settings { get; set; }
}

public class Settings
{
    public int Port { get; set; } = 8888;
    public string Host { get; set; } = "127.0.0.1";
    public string Path { get; set; } = "test/";
}