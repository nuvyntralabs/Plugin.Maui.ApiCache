# Changelog

## 1.0.0

- CacheFirst, NetworkFirst, StaleWhileRevalidate, NetworkOnly, and CacheOnly
- Typed `IApiCache.GetAsync<T>` and `GetResultAsync<T>`
- Durable file store under app data with LRU eviction
- Optional `HttpClient` handler (`AddApiCache`)
- ETag / 304 and `Cache-Control: max-age`
- .NET MAUI support for iOS and Android (`net10.0-ios`, `net10.0-android`)
