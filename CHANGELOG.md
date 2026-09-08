# Changelog

## 1.0.5

- Fix DI constructor ambiguity so `UseApiCache` / `AddApiCache` resolve when `HttpClient` is also registered
- Document the current pack artifact version
- Tests for `ApiCacheFactory`, ETag / 304 expiry refresh, `Cache-Control: max-age`, handler stale headers, and offline `NetworkOnly`
- Sample covers `SetAsync`, `ExistsAsync`, single-key invalidate, and `ClearAsync`

## 1.0.0

- CacheFirst, NetworkFirst, StaleWhileRevalidate, NetworkOnly, and CacheOnly
- Typed `IApiCache.GetAsync<T>` and `GetResultAsync<T>`
- Durable file store under app data with LRU eviction
- Optional `HttpClient` handler (`AddApiCache`)
- ETag / 304 and `Cache-Control: max-age`
- .NET MAUI support for iOS and Android (`net10.0-ios`, `net10.0-android`)
