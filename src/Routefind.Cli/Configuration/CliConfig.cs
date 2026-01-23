using System.Text.Json.Serialization;

namespace Routefind.Cli.Configuration;

public class CliConfig
{
    [JsonPropertyName("editor")]
    public EditorSettings Editor { get; set; } = new();
}
