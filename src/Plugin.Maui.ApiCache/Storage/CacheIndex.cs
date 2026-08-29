namespace Plugin.Maui.ApiCache;

internal sealed class CacheIndex
{
    public List<CacheIndexEntry> Entries { get; set; } = [];
}

internal sealed class CacheIndexEntry
{
    public string Key { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public DateTimeOffset LastAccessUtc { get; set; }
}
