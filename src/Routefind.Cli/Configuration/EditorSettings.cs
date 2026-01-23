using System.Text.Json.Serialization;

namespace Routefind.Cli.Configuration;

public class EditorSettings
{
    [JsonPropertyName("default")]
    public string? Default { get; set; }

    [JsonPropertyName("openFileCount")]
    public int OpenFileCount { get; set; } = 1;

    [JsonPropertyName("commands")]
    public Dictionary<string, EditorCommand?> Commands { get; set; } = new();
}
