namespace Plugin.Maui.ApiCache;

/// <summary>
/// Static accessor for the cache registered by <c>AddApiCache</c> / <c>UseApiCache</c>.
/// </summary>
public static class ApiCache
{
    private static IApiCache? _default;

    /// <summary>
    /// The cache instance registered with the MAUI host.
    /// </summary>
    public static IApiCache Default =>
        _default ?? throw new InvalidOperationException(
            "ApiCache is not initialized. Call services.AddApiCache(...) or builder.UseApiCache(...) first.");

    /// <summary>
    /// True after <see cref="SetDefault"/> has been called.
    /// </summary>
    public static bool IsInitialized => _default is not null;

    internal static void SetDefault(IApiCache instance)
        => _default = instance ?? throw new ArgumentNullException(nameof(instance));
}
