# ELF Brewery API

A RESTful .NET Web API that serves brewery data from the [Open Brewery DB](https://www.openbrewerydb.org/) with caching, search, sorting, pagination, authentication, and API versioning.

## Requirements Covered

### Core Specifications

| # | Requirement | Implementation |
|---|---|---|
| 1 | RESTful endpoint – names, cities, phones | `BreweriesController` at `api/v1/breweries` |
| 2a | In-memory storage | `InMemoryBreweryRepository` with `IMemoryCache` |
| 2b | Classes and interfaces | `IBreweryService`, `IBreweryRepository`, `IBrewerySourceLoader` |
| 2c | Dependency injection | `Program.cs` composition root |
| 2d | Sort by name, distance, city | `BrewerySortBy` enum + `BreweryService.Sort` |
| 2e | Search functionality | Multi-field search across name, city, state, country, type, postal code |
| 2f | Map source to generic model | `BrewerySource` → `BreweryMapper` → `Brewery` |
| 2g | Cache results for 10 minutes | `AbsoluteExpirationRelativeToNow` with `SemaphoreSlim` lock |
| 2h | SOLID principles | See architecture section below |
| 2i | Error handling | `ExceptionHandlingMiddleware` with RFC 7807 ProblemDetails |

### Bonus Tasks

| # | Bonus | Implementation |
|---|---|---|
| 1 | Autocomplete search | `AutocompleteAsync` with prefix-first ranking |
| 2 | API versioning | URL segment (`api/v1/`) + `x-api-version` header |
| 3 | Logging | `ILogger<T>` structured logging throughout |
| 4 | SQLite + EF Core | `BreweryDbContext`, `SqliteBreweryRepository`, `DatabaseInitializer` |
| 5 | API authentication | `ApiKeyAuthenticationHandler` with fixed-time comparison |

## Architecture (SOLID)

| Principle | Where |
|---|---|
| **S**RP | Loader fetches, repository caches, service queries, mapper transforms |
| **O**CP | New sort = new enum member + one switch arm |
| **L**SP | `InMemoryBreweryRepository` and `SqliteBreweryRepository` are interchangeable |
| **I**SP | Separate `IBrewerySourceLoader`, `IBreweryRepository`, `IBreweryService` |
| **D**IP | Controller → `IBreweryService` → `IBreweryRepository`; concretes only in `Program.cs` |

## How to Run

```bash
dotnet run --project src/ELFAssessment.API
```

Open `https://localhost:7143/swagger`, click **Authorize**, paste `elf-dev-api-key`, then explore the endpoints.

## API Endpoints

All endpoints require the `X-Api-Key: elf-dev-api-key` header.

```
GET /api/v1/breweries                              # List with search/sort/paging
GET /api/v1/breweries/{id}                          # Single brewery
GET /api/v1/breweries/autocomplete?term=abc&limit=5 # Name autocomplete
```

### Query Parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `search` | string | – | Search across name, city, state, country, type, postal code |
| `sortBy` | enum | `Name` | `Name`, `City`, or `Distance` |
| `sortDirection` | enum | `Asc` | `Asc` or `Desc` |
| `latitude` | double | – | Required when `sortBy=Distance` |
| `longitude` | double | – | Required when `sortBy=Distance` |
| `page` | int | 1 | Page number |
| `pageSize` | int | 50 | Items per page (max 200) |

## Configuration

In `appsettings.json`:

```json
{
  "BreweryData": {
    "SourceApiUrl": "https://api.openbrewerydb.org/v1/breweries",
    "CacheDuration": "00:10:00",
    "StorageProvider": "InMemory",
    "ConnectionString": "Data Source=breweries.db"
  },
  "ApiKey": {
    "HeaderName": "X-Api-Key",
    "Value": "elf-dev-api-key"
  }
}
```

Set `StorageProvider` to `Sqlite` to use EF Core with SQLite instead of in-memory caching.

## Running Tests

```bash
dotnet test
```

81 unit tests covering mapper, service, repository, controller, middleware, authentication, and models.

## Tech Stack

- .NET 10 / ASP.NET Core Web API
- Entity Framework Core + SQLite
- xUnit + Moq for testing
- Swashbuckle for Swagger/OpenAPI
- Asp.Versioning for API versioning
