using System.Text.Json.Serialization;

namespace Routefind.Cli.Configuration;

public class EditorCommand
{
    [JsonPropertyName("command")]
    public string Command { get; set; } = "";

    [JsonPropertyName("launchType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public LaunchType LaunchType { get; set; } = LaunchType.Background;
}

public enum LaunchType
{
    Background,
    Inline
}
