using Asp.Versioning;
using ELFAssessment.API.Configuration;
using ELFAssessment.API.Data;
using ELFAssessment.API.Middleware;
using ELFAssessment.API.Security;
using ELFAssessment.API.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

// =============================================================================
// ELF Brewery API – Composition Root (Program.cs)
// Configures DI, authentication, API versioning, Swagger, and the middleware pipeline.
// Storage provider (InMemory or Sqlite) is selected via appsettings.json.
// =============================================================================

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ──────────────────────────────────────────────────────
// Bind strongly-typed option classes to their appsettings.json sections
builder.Services.Configure<BreweryDataOptions>(builder.Configuration.GetSection(BreweryDataOptions.SectionName));
builder.Services.Configure<ApiKeyOptions>(builder.Configuration.GetSection(ApiKeyOptions.SectionName));

var breweryOptions = builder.Configuration.GetSection(BreweryDataOptions.SectionName).Get<BreweryDataOptions>() ?? new BreweryDataOptions();

// ── Caching ────────────────────────────────────────────────────────────
// IMemoryCache is used by both InMemory and Sqlite repositories to cache brewery data
builder.Services.AddMemoryCache();

// ── HttpClient for source API ──────────────────────────────────────────
// Typed HttpClient for OpenBreweryDbLoader; manages connection pooling via IHttpClientFactory
builder.Services.AddHttpClient<IBrewerySourceLoader, OpenBreweryDbLoader>();

// ── Storage provider ───────────────────────────────────────────────────
// "Sqlite": EF Core + SQLite database with background refresh via DataRefreshService
// "InMemory" (default): data loaded from API and cached in IMemoryCache with 10-min expiration
if (breweryOptions.StorageProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<BreweryDbContext>(opts =>
        opts.UseSqlite(breweryOptions.ConnectionString));
    builder.Services.AddScoped<IBreweryRepository, SqliteBreweryRepository>();
    builder.Services.AddTransient<DatabaseInitializer>();
    builder.Services.AddHostedService<DataRefreshService>(); // Periodic re-sync from source API
}
else
{
    builder.Services.AddScoped<IBreweryRepository, InMemoryBreweryRepository>();
}

// ── Business logic ─────────────────────────────────────────────────────
builder.Services.AddScoped<IBreweryService, BreweryService>();

// ── Authentication ─────────────────────────────────────────────────────
// Custom API key scheme: validates X-Api-Key header using constant-time comparison
builder.Services.AddAuthentication("ApiKey")
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKey", null);
builder.Services.AddAuthorization();

// ── API Versioning ─────────────────────────────────────────────────────
// Supports URL segment (api/v1/) and x-api-version header for version negotiation
builder.Services.AddApiVersioning(opts =>
{
    opts.DefaultApiVersion = new ApiVersion(1, 0);
    opts.ReportApiVersions = true;
    opts.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("x-api-version"));
}).AddMvc()
.AddApiExplorer(opts =>
{
    opts.GroupNameFormat = "'v'VVV";
    opts.SubstituteApiVersionInUrl = true;
});

// ── Controllers + Swagger ──────────────────────────────────────────────
// JsonStringEnumConverter ensures enums (BrewerySortBy, SortDirection) serialize as strings, not integers
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ELF Brewery API", Version = "v1" });
    c.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.SecuritySchemeType.ApiKey,
        Name = "X-Api-Key",
        In = Microsoft.OpenApi.ParameterLocation.Header,
        Description = "API key for authentication"
    });
    // The scheme reference must be bound to the host document, otherwise it serializes as an empty object.
    c.AddSecurityRequirement(document => new Microsoft.OpenApi.OpenApiSecurityRequirement
    {
        [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("ApiKey", document)] = new List<string>()
    });
});

var app = builder.Build();

// ── Seed SQLite database if needed ─────────────────────────────────────
// On first run: creates the database, fetches all data from the API, and inserts into SQLite
// On subsequent runs: skips seeding if data already exists
if (breweryOptions.StorageProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();
}

// ── Middleware pipeline ────────────────────────────────────────────────
// ExceptionHandlingMiddleware must be first to catch errors from all downstream middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ELF Brewery API v1"));
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Enables WebApplicationFactory<Program> in integration tests
public partial class Program { }
