using Microsoft.Maui.Storage;

namespace Plugin.Maui.ApiCache;

/// <summary>
/// <see cref="IAppStorage"/> backed by MAUI <see cref="FileSystem.AppDataDirectory"/>.
/// </summary>
public sealed class MauiAppStorage : IAppStorage
{
    /// <inheritdoc />
    public string AppDataDirectory
    {
        get
        {
            try
            {
                return FileSystem.AppDataDirectory;
            }
            catch (Exception)
            {
                return Path.Combine(Path.GetTempPath(), "Plugin.Maui.ApiCache");
            }
        }
    }
}
