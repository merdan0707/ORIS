using System.Text.Json;

namespace MyServer_v3.Framework.Configuration;

public sealed class ConfigService
{
    private static ConfigService? _instance;
    private static readonly object _lock = new();

    public ServerSettings Settings { get;}

    private ConfigService()
    {
        const string configFileName = "server.json";

        if (!File.Exists(configFileName))
            throw new FileNotFoundException($"Settings file '{configFileName}' not found.");

        var json = File.ReadAllText(configFileName);
        Settings = JsonSerializer.Deserialize<ServerSettings>(json)
            ?? throw new InvalidOperationException("Не удалось прочитать server.json");

        if (!Settings.IsValid)
            throw new InvalidOperationException("Настройки в server.json некорректны.");
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