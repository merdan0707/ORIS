using System.Text.Json;

namespace My_Server;

public sealed class ConfigurationManager
{
    private static ConfigurationManager? _instance;
    private static readonly object _lock = new();

    public ServerSettings Settings { get; }

    private ConfigurationManager()
    {
        var json =  File.ReadAllText("server.json");
        Settings = JsonSerializer.Deserialize<ServerSettings>(json)
            ?? throw new InvalidOperationException("Failed to deserialize server.json");

        if (!Settings.IsValid)
            throw new InvalidOperationException("Incorrect server settings in server.json");
    }

    public static ConfigurationManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new ConfigurationManager();
                }
            }
            return _instance;
        }
    }
}