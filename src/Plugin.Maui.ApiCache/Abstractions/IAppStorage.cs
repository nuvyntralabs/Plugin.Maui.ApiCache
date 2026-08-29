namespace Plugin.Maui.ApiCache;

/// <summary>
/// Resolves a writable app-data folder for the on-disk cache.
/// </summary>
public interface IAppStorage
{
    /// <summary>
    /// Directory that survives app restarts and is private to the app.
    /// </summary>
    string AppDataDirectory { get; }
}
