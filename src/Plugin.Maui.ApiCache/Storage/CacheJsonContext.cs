namespace Plugin.Maui.ApiCache;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(CacheRecord))]
[JsonSerializable(typeof(CacheIndex))]
[JsonSerializable(typeof(CacheIndexEntry))]
[JsonSerializable(typeof(List<CacheIndexEntry>))]
internal partial class CacheJsonContext : JsonSerializerContext;
