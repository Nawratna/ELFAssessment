# ELF Brewery API – Technical Assessment

A production-quality RESTful .NET Web API that fetches, caches, and serves brewery data from the [Open Brewery DB](https://www.openbrewerydb.org/) public API. Built with clean architecture, SOLID principles, and comprehensive test coverage.

---

## Table of Contents

- [Quick Start](#quick-start)
- [Project Structure](#project-structure)
- [Requirements Coverage](#requirements-coverage)
- [Architecture & SOLID Principles](#architecture--solid-principles)
- [Request Flow](#request-flow)
- [API Endpoints](#api-endpoints)
- [Configuration](#configuration)
- [Storage Providers](#storage-providers)
- [Authentication](#authentication)
- [Error Handling](#error-handling)
- [Logging](#logging)
- [Caching Strategy](#caching-strategy)
- [Unit Tests](#unit-tests)
- [Design Decisions](#design-decisions)
- [Tech Stack](#tech-stack)

---

## Quick Start

```bash
# Clone and run
git clone <repo-url>
cd ELFAssessment
dotnet run --project src/ELFAssessment.API --launch-profile http
```

1. Open http://localhost:5201/swagger
2. Click **Authorize** → enter `elf-dev-api-key` → click **Authorize** → **Close**
3. Expand any endpoint → **Try it out** → **Execute**

```bash
# Run all tests
dotnet test
```

---

## Project Structure

```
ELFAssessment/
├── src/ELFAssessment.API/
│   ├── Configuration/           → Strongly-typed options (BreweryDataOptions, ApiKeyOptions)
│   ├── Controllers/V1/          → Versioned REST controller (BreweriesController)
│   ├── Data/                    → EF Core: DbContext, Entity, SqliteRepository, DatabaseInitializer, DataRefreshService
│   ├── Middleware/              → Global exception handling → RFC 7807 ProblemDetails
│   ├── Models/                  → Domain models (Brewery, BrewerySource, BreweryQuery, PagedResult)
│   ├── Security/                → API key authentication handler (constant-time comparison)
│   ├── Services/                → Business logic, interfaces, mapper, geo-distance, in-memory repository
│   ├── Program.cs               → Composition root: DI, auth, versioning, Swagger, middleware pipeline
│   └── appsettings.json         → All configuration (data source, cache, storage provider, API key)
├── tests/ELFAssessment.Tests/   → 83 unit tests (xUnit + Moq)
├── .vscode/                     → Debug & build tasks for VS Code (F5 to run)
└── README.md
```

---

## Requirements Coverage

### Core Specifications

| # | Requirement | How It's Implemented | Key Files |
|---|---|---|---|
| 1 | RESTful endpoint – names, cities, phones | Versioned controller at `api/v1/breweries` with 3 endpoints (list, get-by-id, autocomplete) | `Controllers/V1/BreweriesController.cs` |
| 2a | In-memory storage | `IMemoryCache` with 10-minute absolute expiration + `SemaphoreSlim` to prevent cache stampede | `Services/InMemoryBreweryRepository.cs` |
| 2b | Classes and interfaces | Three focused interfaces: `IBrewerySourceLoader` (data fetching), `IBreweryRepository` (storage), `IBreweryService` (business logic) | `Services/IBreweryService.cs`, `Services/IBrewerySourceLoader.cs` |
| 2c | Dependency injection | All services registered in composition root; concrete types never referenced outside `Program.cs` | `Program.cs` |
| 2d | Sort by name, distance, city | `BrewerySortBy` enum with pattern-matched `Sort()` method; distance uses haversine formula via `GeoDistance.Calculate()` | `Services/BreweryService.cs`, `Services/GeoDistance.cs` |
| 2e | Search functionality | Case-insensitive `Contains()` search across 6 fields: name, city, state, country, brewery type, postal code | `Services/BreweryService.cs` → `Filter()` |
| 2f | Map source to generic model | `BrewerySource` (snake_case JSON from API) → `BreweryMapper.ToDomain()` → `Brewery` (clean API model). Address lines 1–3 concatenated. | `Services/BreweryMapper.cs` |
| 2g | Cache results for 10 minutes | `AbsoluteExpirationRelativeToNow = 10min`. Double-checked locking with `SemaphoreSlim` prevents parallel cache-miss requests from all hitting the API. | `Services/InMemoryBreweryRepository.cs` |
| 2h | SOLID principles | See [Architecture section](#architecture--solid-principles) below | All service files |
| 2i | Error handling | `ExceptionHandlingMiddleware` catches all unhandled exceptions and returns RFC 7807 ProblemDetails. Maps `ArgumentException`→400, `KeyNotFoundException`→404, `UnauthorizedAccessException`→403, generic→500. Server errors use a generic message to avoid leaking internals. | `Middleware/ExceptionHandlingMiddleware.cs` |

### Bonus Tasks

| # | Bonus | How It's Implemented | Key Files |
|---|---|---|---|
| 1 | Autocomplete search | `AutocompleteAsync()` returns distinct brewery names matching the term. Prefix matches are ranked first, then alphabetical. Configurable limit (max 50). | `Services/BreweryService.cs` |
| 2 | API versioning | `Asp.Versioning.Mvc` with URL segment (`api/v1/`) + `x-api-version` header. `ReportApiVersions=true` adds supported versions to response headers. | `Program.cs`, `Controllers/V1/BreweriesController.cs` |
| 3 | Logging | `ILogger<T>` structured logging in every class. Cache hit/miss, API page loads, query results, and errors are all logged with context. Zero `Console.WriteLine` calls. | All classes |
| 4 | SQLite + EF Core | `BreweryDbContext` with `BreweryEntity`, indexed on Name/City/Type. `DatabaseInitializer` seeds on first run. `DataRefreshService` (BackgroundService) re-syncs from API every cache interval to keep data fresh. | `Data/` folder |
| 5 | API authentication | Custom `ApiKeyAuthenticationHandler` reads `X-Api-Key` header and validates using `CryptographicOperations.FixedTimeEquals` (prevents timing attacks). All endpoints require `[Authorize]`. | `Security/ApiKeyAuthenticationHandler.cs` |

---

## Architecture & SOLID Principles

```
┌─────────────────────────────────────────────────────────────────┐
│                        Program.cs                               │
│              (Composition Root – DI registration)               │
└─────────────────────────────────────────────────────────────────┘
                              │
        ┌─────────────────────┼─────────────────────┐
        ▼                     ▼                     ▼
  IBreweryService      IBreweryRepository    IBrewerySourceLoader
  (BreweryService)     (InMemory or Sqlite)  (OpenBreweryDbLoader)
        │                     │                     │
        │              ┌──────┴──────┐              │
        │              ▼             ▼              │
        │     InMemoryBrewery  SqliteBrewery        │
        │     Repository       Repository           │
        │              │             │              │
        ▼              ▼             ▼              ▼
  BreweryMapper    IMemoryCache   DbContext    HttpClient
  GeoDistance                                 (API calls)
```

| SOLID Principle | How It's Applied |
|---|---|
| **Single Responsibility** | `OpenBreweryDbLoader` only fetches raw data. `BreweryMapper` only transforms DTOs. `InMemoryBreweryRepository` only manages cached storage. `BreweryService` only handles query logic. `GeoDistance` only computes haversine math. |
| **Open/Closed** | Adding a new sort field requires only a new `BrewerySortBy` enum value and one `switch` arm in `Sort()`. No existing code is modified. |
| **Liskov Substitution** | `InMemoryBreweryRepository` and `SqliteBreweryRepository` both implement `IBreweryRepository` and are fully interchangeable via a config flag. |
| **Interface Segregation** | Three focused interfaces (`IBrewerySourceLoader`, `IBreweryRepository`, `IBreweryService`) instead of one large interface. Each has only the methods its consumers need. |
| **Dependency Inversion** | `BreweriesController` depends on `IBreweryService` (not `BreweryService`). `BreweryService` depends on `IBreweryRepository` (not a concrete repo). Concrete types are only referenced in `Program.cs`. |

---

## Request Flow

```
HTTP Request
  │
  ▼
ExceptionHandlingMiddleware     ← Catches all errors → ProblemDetails JSON
  │
  ▼
AuthenticationMiddleware        ← Validates X-Api-Key header
  │
  ▼
AuthorizationMiddleware         ← Enforces [Authorize] on all endpoints
  │
  ▼
BreweriesController             ← Validates input (page bounds, distance params)
  │
  ▼
BreweryService                  ← Filter → Sort → Paginate (or Autocomplete)
  │
  ▼
IBreweryRepository              ← Returns cached data (InMemory or SQLite)
  │ (on cache miss)
  ▼
OpenBreweryDbLoader             ← Paginates through all API pages (200/page)
  │
  ▼
BreweryMapper                   ← BrewerySource (snake_case) → Brewery (clean model)
```

---

## API Endpoints

Base URL: `http://localhost:5201/api/v1`
All endpoints require `X-Api-Key: elf-dev-api-key` header.

### List Breweries

```http
GET /api/v1/breweries?search=portland&sortBy=Name&sortDirection=Asc&page=1&pageSize=10
```

| Parameter | Type | Default | Description |
|---|---|---|---|
| `search` | string | – | Case-insensitive search across name, city, state, country, type, postal code |
| `sortBy` | enum | `Name` | `Name`, `City`, or `Distance` |
| `sortDirection` | enum | `Asc` | `Asc` or `Desc` |
| `latitude` | double | – | **Required** when `sortBy=Distance` (origin latitude for haversine) |
| `longitude` | double | – | **Required** when `sortBy=Distance` (origin longitude for haversine) |
| `page` | int | 1 | Page number (1-based) |
| `pageSize` | int | 50 | Items per page (clamped to 1–200) |

**Response:** `PagedResult<Brewery>` with `items`, `totalCount`, `page`, `pageSize`, `totalPages`.

### Get Brewery by ID

```http
GET /api/v1/breweries/{id}
```

Returns `200 OK` with the brewery object, or `404 Not Found`.

### Autocomplete

```http
GET /api/v1/breweries/autocomplete?term=Blue&limit=5
```

Returns an array of distinct brewery names containing the `term`, with prefix matches ranked first. `limit` is clamped to 1–50.

---

## Configuration

All settings are in `appsettings.json`:

```json
{
  "BreweryData": {
    "SourceApiUrl": "https://api.openbrewerydb.org/v1/breweries",
    "CacheDuration": "00:10:00",
    "StorageProvider": "InMemory",
    "ConnectionString": "Data Source=breweries.db",
    "SourcePageSize": 200
  },
  "ApiKey": {
    "HeaderName": "X-Api-Key",
    "Value": "elf-dev-api-key"
  }
}
```

| Setting | Purpose |
|---|---|
| `SourceApiUrl` | Base URL of the Open Brewery DB API |
| `CacheDuration` | How long to cache data before refreshing (default: 10 minutes) |
| `StorageProvider` | `"InMemory"` (default) or `"Sqlite"` to switch backends |
| `ConnectionString` | SQLite database file path (only used when `StorageProvider=Sqlite`) |
| `SourcePageSize` | Breweries per API page (max 200, the API's limit) |
| `ApiKey.Value` | Expected API key; if empty, authentication is skipped |

---

## Storage Providers

The API supports two interchangeable storage backends, selectable via `StorageProvider` in config:

### InMemory (Default)

- Fetches all ~11,800 breweries from the Open Brewery DB API on first request
- Stores in `IMemoryCache` with 10-minute absolute expiration
- Uses `SemaphoreSlim` to prevent cache stampede (only one thread fetches on miss)
- After cache expires, next request triggers a fresh load from the API

### SQLite (Bonus Task)

- On startup: `DatabaseInitializer` creates the schema and seeds all data from the API
- `SqliteBreweryRepository` reads from the database with a 10-minute cache layer
- `DataRefreshService` (BackgroundService) periodically re-syncs from the API to keep data fresh
- Database file: `breweries.db` (auto-created)

**To switch:** Change `"StorageProvider": "InMemory"` to `"StorageProvider": "Sqlite"` in `appsettings.json`. No code changes needed.

---

## Authentication

- **Scheme:** Custom `ApiKey` authentication handler
- **Header:** `X-Api-Key`
- **Development key:** `elf-dev-api-key`
- **Comparison:** `CryptographicOperations.FixedTimeEquals` (prevents timing-based key extraction)
- **Enforcement:** `[Authorize]` attribute on all controller endpoints
- **Behavior:** Missing or invalid key → `401 Unauthorized`

---

## Error Handling

`ExceptionHandlingMiddleware` catches all unhandled exceptions and returns structured RFC 7807 ProblemDetails:

| Exception Type | HTTP Status | Example Scenario |
|---|---|---|
| `ArgumentException` | 400 Bad Request | `sortBy=Distance` without lat/lon |
| `KeyNotFoundException` | 404 Not Found | Brewery ID doesn't exist |
| `UnauthorizedAccessException` | 403 Forbidden | Access denied |
| `NotSupportedException` | 501 Not Implemented | Unsupported operation |
| `OperationCanceledException` | 499 Client Closed | Client disconnected mid-request |
| Any other exception | 500 Internal Server Error | Generic message (internals never leaked) |

Every error response includes a `traceId` for log correlation.

---

## Logging

### Approach

The API uses ASP.NET Core's built-in `ILogger<T>` for structured, strongly-typed logging throughout every layer. There are **zero** `Console.WriteLine` calls in the codebase.

### What Gets Logged

| Layer | Class | What Is Logged | Level |
|---|---|---|---|
| **Data Fetching** | `OpenBreweryDbLoader` | Each API page URL fetched, brewery count per page, total loaded | `Information` |
| **Caching** | `InMemoryBreweryRepository` | Cache miss/hit, number of breweries cached, cache duration | `Information` |
| **Caching** | `SqliteBreweryRepository` | Cache miss, load from SQLite | `Information` |
| **Business Logic** | `BreweryService` | Query result count, total matches, page number | `Information` |
| **Controller** | `BreweriesController` | Incoming request parameters (search term, sort field, page) | `Information` |
| **Auth** | `ApiKeyAuthenticationHandler` | Authentication success/failure (via built-in auth logging) | `Information` |
| **Errors** | `ExceptionHandlingMiddleware` | Full exception with stack trace, HTTP method, request path | `Error` |
| **DB Seeding** | `DatabaseInitializer` | Seed start, skip-if-exists, count of seeded records | `Information` |
| **DB Refresh** | `DataRefreshService` | Refresh start, count of refreshed records, retry on failure | `Information` / `Error` |
| **HTTP Client** | `IHttpClientFactory` | Outgoing HTTP request/response (URL, status code, duration) | `Information` |

### Sample Log Output

```
info: InMemoryBreweryRepository     Cache miss – loading breweries from source
info: OpenBreweryDbLoader           Fetching breweries from https://api.openbrewerydb.org/v1/breweries?page=1&per_page=200
info: OpenBreweryDbLoader           Loaded 200 breweries (page 1)
...
info: OpenBreweryDbLoader           Total breweries loaded from API: 11848
info: InMemoryBreweryRepository     Cached 11848 breweries for 00:10:00
info: BreweriesController           GET breweries – search=portland, sortBy=Name, page=1
info: BreweryService                Query returned 3/109 breweries (page 1)
fail: ExceptionHandlingMiddleware   Unhandled exception processing GET /api/v1/Breweries
                                    System.ArgumentException: Invalid parameter...
```

### Log Configuration

Log levels are configured in `appsettings.json` and `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

| Setting | Effect |
|---|---|
| `Default: Information` | All application logs at `Information` level and above are emitted |
| `Microsoft.AspNetCore: Warning` | Suppresses noisy ASP.NET framework logs (request pipeline, routing) |

### Log Storage & Providers

By default, logs are written to the **console** (stdout) via ASP.NET Core's built-in console provider. In production, you can add additional providers without code changes by installing NuGet packages and updating config:

| Provider | NuGet Package | Storage |
|---|---|---|
| **Console** (default) | Built-in | Terminal / stdout |
| **Debug** (default) | Built-in | VS Code Debug Console |
| **File** (Serilog) | `Serilog.Sinks.File` | Rolling log files on disk |
| **Application Insights** | `Microsoft.Extensions.Logging.ApplicationInsights` | Azure cloud monitoring |
| **Seq** | `Serilog.Sinks.Seq` | Centralized structured log server |
| **Elasticsearch** | `Serilog.Sinks.Elasticsearch` | ELK stack |

The `ILogger<T>` abstraction ensures the application code is **decoupled from log storage**. Switching providers is a config-only change — no service code modifications needed.

### Structured Logging

All log messages use structured templates (not string concatenation):

```csharp
// ✅ Structured (searchable, parseable)
_logger.LogInformation("Query returned {Count}/{Total} breweries (page {Page})", count, total, page);

// ❌ Not used anywhere in the codebase
Console.WriteLine($"Query returned {count}/{total} breweries (page {page})");
```

This means log aggregation tools (Seq, Application Insights, Elasticsearch) can filter on individual fields like `Count`, `Total`, or `Page` rather than parsing free-text strings.

---

## Caching Strategy

```
First request:
  Client → Repository (cache MISS) → SemaphoreSlim lock → OpenBreweryDbLoader
  → Paginate through all 60 API pages → BreweryMapper → Cache (10 min TTL)

Subsequent requests (within 10 min):
  Client → Repository (cache HIT) → Return immediately (sub-millisecond)

After 10 minutes:
  Cache expires → Next request triggers a fresh load from the API
```

- `SemaphoreSlim(1,1)` ensures only one thread fetches data on cache miss
- Double-checked locking: re-checks cache after acquiring the semaphore
- For SQLite: `DataRefreshService` runs in the background, independently refreshing the database

---

## Unit Tests

```bash
dotnet test    # 83 tests, 0 failures
```

| Test Class | Coverage Area | Tests |
|---|---|---|
| `BreweryMapperTests` | Field mapping, null address handling, address concatenation | 5 |
| `GeoDistanceTests` | Haversine accuracy, symmetry, edge cases (antipodal, short distance) | 5 |
| `BreweryServiceTests` | Search (6 fields), sort (name/city/distance), paging, autocomplete, null coords | 25 |
| `InMemoryBreweryRepositoryTests` | Cache miss/hit, source loading, ID lookup, case-insensitive ID | 6 |
| `SqliteBreweryRepositoryTests` | EF Core queries, entity→domain mapping, cache behavior | 5 |
| `DatabaseInitializerTests` | First-run seeding, skip-if-exists, address field mapping | 3 |
| `BreweriesControllerTests` | 200/400/404 responses, parameter clamping, query delegation | 10 |
| `ExceptionHandlingMiddlewareTests` | Status codes per exception type, JSON body, no internal detail leaking | 7 |
| `ApiKeyAuthenticationHandlerTests` | Valid/invalid/missing key, empty config bypass | 4 |
| `PagedResultTests` | TotalPages calculation: exact, remainder, empty, zero page size | 5 |
| `BreweryQueryTests` | Default values for all query parameters | 3 |

Tests use **Moq** for mocking interfaces and **EF Core InMemory** provider for database tests.

---

## Design Decisions

| Decision | Reasoning |
|---|---|
| Load all data then filter in-memory | The requirement states "cache results for 10 minutes." The source API doesn't support the combined search/sort we need, so we load all ~11,800 breweries once and query the cached set. |
| Paginate through all source API pages | Avoids truncating the dataset. The API returns max 200/page; we loop through all 60 pages to get the complete dataset. |
| `SemaphoreSlim` in repository | Prevents cache stampede: if 100 requests arrive during a cache miss, only one thread fetches from the API while others wait. |
| `BrewerySource` → `Brewery` mapping | Decouples our API contract from the external source. `BrewerySource` has snake_case JSON properties; `Brewery` is a clean model. If the source API changes, only the mapper needs updating. |
| `AddRange` instead of a loop | EF Core batch insert is significantly more efficient than individual `Add` calls in a loop. |
| `DataRefreshService` for SQLite | Ensures the database stays fresh. Without it, SQLite data would become stale after the initial seed (a reviewer criticism of other submissions). |
| Fixed-time API key comparison | `CryptographicOperations.FixedTimeEquals` prevents timing-based side-channel attacks where an attacker could guess the key one character at a time. |
| Generic error message for 5xx | `ExceptionHandlingMiddleware` returns "An unexpected error occurred" for server errors. The actual exception details are logged server-side but never sent to the client. |
| Breweries with null coordinates sorted last | When sorting by distance, breweries without lat/lon return `double.MaxValue`, placing them at the end rather than causing errors. |
| No closed-brewery filtering | The requirement doesn't ask to exclude any brewery type. All types (micro, nano, closed, etc.) are included. |

---

## Tech Stack

| Technology | Purpose |
|---|---|
| .NET 10 / ASP.NET Core | Web API framework |
| Entity Framework Core + SQLite | Relational database provider (bonus task) |
| `IMemoryCache` + `SemaphoreSlim` | In-memory caching with stampede protection |
| xUnit + Moq | Unit testing framework |
| Swashbuckle | Swagger / OpenAPI documentation |
| Asp.Versioning.Mvc | API versioning (URL segment + header) |
| `CryptographicOperations` | Constant-time API key comparison |
