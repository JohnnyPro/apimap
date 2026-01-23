using System.Text.Json;
using Routefind.Core.Cache;

namespace Routefind.Cli.Configuration;

public class ConfigService
{
    private const string ConfigDirectory = ".apimap";
    private readonly string _configPath;
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public CliConfig Config { get; private set; }

    public ConfigService()
    {
        var basePath = Path.Combine(Environment.CurrentDirectory, ConfigDirectory);
        _configPath = Path.Combine(basePath, "config.json");
        Directory.CreateDirectory(basePath); // Ensure the .apimap directory exists
        Config = LoadOrCreateConfig();
    }

    private CliConfig LoadOrCreateConfig()
    {
        if (File.Exists(_configPath))
        {
            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<CliConfig>(json) ?? CreateDefaultConfig();
        }

        return CreateDefaultConfigAndSave();
    }

    private CliConfig CreateDefaultConfigAndSave()
    {
        var config = CreateDefaultConfig();
        var json = JsonSerializer.Serialize(config, _jsonOptions);
        File.WriteAllText(_configPath, json);
        return config;
    }

    private static CliConfig CreateDefaultConfig()
    {
        return new CliConfig
        {
            Editor = new EditorSettings
            {
                Default = "code",
                OpenFileCount = 1,
                Commands = new Dictionary<string, EditorCommand?>
                {
                    { "code", new EditorCommand { Command = "code -g {FILENAME}:{LINENUMBER}", LaunchType = LaunchType.Background } },
                    { "vim", new EditorCommand { Command = "vim +{LINENUMBER} {FILENAME}", LaunchType = LaunchType.Inline } },
                    { "custom", null }
                }
            }
        };
    }
}