using System.Text.Json;

namespace My_Server.Framework.Configuration;

public sealed class ConfigService
{
    private static ConfigService? _instance;
    private static readonly object _lock = new();

    public ServerSettings Settings { get; }

    private ConfigService()
    {
        var json =  File.ReadAllText("server.json");
        Settings = JsonSerializer.Deserialize<ServerSettings>(json)
            ?? throw new InvalidOperationException("Failed to deserialize server.json");

        if (!Settings.IsValid)
            throw new InvalidOperationException("Incorrect server settings in server.json");
    }

    public static ConfigService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new ConfigService();
                }
            }
            return _instance;
        }
    }
}