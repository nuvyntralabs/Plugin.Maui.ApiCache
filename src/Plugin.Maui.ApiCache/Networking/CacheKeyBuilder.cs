namespace Plugin.Maui.ApiCache;

internal static class CacheKeyBuilder
{
    public static string Build(string path, CacheRequestOptions? request, ApiCacheOptions options)
    {
        if (!string.IsNullOrWhiteSpace(request?.CacheKey))
        {
            return request.CacheKey.Trim();
        }

        var normalized = NormalizePath(path);
        if (options.VaryHeaders.Count == 0 || request?.Headers is null)
        {
            return normalized;
        }

        var vary = new StringBuilder(normalized);
        foreach (var header in options.VaryHeaders.OrderBy(h => h, StringComparer.OrdinalIgnoreCase))
        {
            if (request.Headers.TryGetValue(header, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                vary.Append('|').Append(header).Append('=').Append(value);
            }
        }

        return vary.ToString();
    }

    public static string NormalizePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var trimmed = path.Trim();

        // On Unix, Uri.TryCreate("/customers", Absolute) becomes file:///customers.
        // Only treat real HTTP(S) URLs as absolute.
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var absolute)
            && (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
        {
            var builder = new UriBuilder(absolute)
            {
                Fragment = string.Empty
            };
            return builder.Uri.GetLeftPart(UriPartial.Query);
        }

        if (trimmed.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed["file:".Length..].TrimStart('/');
            if (!trimmed.StartsWith('/'))
            {
                trimmed = "/" + trimmed;
            }
        }

        return trimmed.StartsWith('/') ? trimmed : "/" + trimmed;
    }
}
