namespace Plugin.Maui.ApiCache;

internal sealed class StaticOptionsMonitor<T> : IOptionsMonitor<T>
    where T : class
{
    public StaticOptionsMonitor(T value) => CurrentValue = value;

    public T CurrentValue { get; }

    public T Get(string? name) => CurrentValue;

    public IDisposable? OnChange(Action<T, string?> listener) => null;
}
