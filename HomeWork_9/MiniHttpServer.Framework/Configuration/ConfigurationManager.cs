using System.Text.Json;

namespace MiniHttpServer.Framework.Configuration;

public sealed class ConfigurationManager
{
    private static ConfigurationManager? _instance;
    private static readonly object _lock = new();

    public ServerSettings Settings { get;}

    private ConfigurationManager()
    {
        const string configFileName = "settings.json";

        if (!File.Exists(configFileName))
            throw new FileNotFoundException($"Settings file '{configFileName}' not found.");

        var json = File.ReadAllText(configFileName);
        Settings = JsonSerializer.Deserialize<ServerSettings>(json)
            ?? throw new InvalidOperationException("Не удалось прочитать settings.json");

        if (!Settings.IsValid)
            throw new InvalidOperationException("Настройки в settings.json некорректны.");
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