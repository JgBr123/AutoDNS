using System.Text.Json.Serialization;

namespace AutoDNSd
{
    public record Settings
    {
        public required int delay { get; init; }
        public required Entry[] entries { get; init; }
    }
    public record Entry
    {
        public required string api_token { get; init; }
        public required Zone[] zones { get; init; }
    }
    public record Zone
    {
        public required string id { get; init; }
        public required string[] records { get; init; }
    }

    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(Settings))]
    internal partial class SettingsJsonContext : JsonSerializerContext { }
}
