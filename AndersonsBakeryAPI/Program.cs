// Adriaan
using AndersonsBakeryAPI.Services;
using AndersonsBakeryAPI.Data;
using AndersonsBakeryAPI.Repositories;
using MongoDB.Driver;
using SharedLibrary.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

    // --- Connection strings ---
    var mongoConnection = builder.Configuration.GetConnectionString("MongoConnection");
    var sqlConnectionString = builder.Configuration.GetConnectionString("SqlServer");
    // --- MongoDB client (with short timeouts so fallback is fast) ---
    if (!string.IsNullOrWhiteSpace(mongoConnection))
    {
        var mongoSettings = MongoClientSettings.FromConnectionString(mongoConnection);
        mongoSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(3);
        mongoSettings.ConnectTimeout = TimeSpan.FromSeconds(3);
        builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoSettings));
        builder.Services.AddScoped<IMongoDatabase>(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(builder.Configuration["MongoDatabase"] ?? "LogiSynDb");
        });
        builder.Services.AddScoped<MongoUserService>();
        builder.Services.AddScoped<MongoLoginService>();
        builder.Services.AddScoped<MongoOrderRepository>();
        builder.Services.AddScoped<MongoProductRepository>(sp => new MongoProductRepository(sp.GetRequiredService<IMongoDatabase>()));
    }
    // --- SQL Server DbContext (EF Core) ---
    if (!string.IsNullOrWhiteSpace(sqlConnectionString))
    {
        builder.Services.AddDbContext<LogiSynDbContext>(options =>
            options.UseSqlServer(sqlConnectionString));
    }
    else
    {
        builder.Services.AddDbContext<LogiSynDbContext>(options =>
            options.UseSqlServer(@"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=LogiSynDb;Integrated Security=True;TrustServerCertificate=True;"));
    }

    // --- User & Login Service Routers (resilient to missing Mongo) ---
    // Use explicit factories to avoid constructor ambiguity when both Mongo and SQL services are registered.
    builder.Services.AddScoped<UserServiceRouter>(sp =>
    {
        var logger = sp.GetService<ILogger<UserServiceRouter>>();
        var mongo  = sp.GetService<MongoUserService>();
        var sql    = sp.GetRequiredService<UserService>();
        return mongo != null
            ? new UserServiceRouter(mongo, sql, logger)
            : new UserServiceRouter(logger);
    });
    builder.Services.AddScoped<LoginServiceRouter>(sp =>
    {
        var logger = sp.GetService<ILogger<LoginServiceRouter>>();
        var mongo  = sp.GetService<MongoLoginService>();
        var sql    = sp.GetRequiredService<LoginService>();
        return mongo != null
            ? new LoginServiceRouter(mongo, sql)
            : new LoginServiceRouter(logger);
    });
    builder.Services.AddScoped<SyncService>();

    // --- Jwt Authentication (reads settings from configuration) ---
    var jwtSection = builder.Configuration.GetSection("Jwt");
    var jwtKey = jwtSection["Key"] ?? string.Empty;
    if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 16)
    {
        jwtKey = "AndersonsBakeryLogiSynSecureFallbackSigningKey2026!#*";
    }

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateIssuer = true,
                ValidIssuer = !string.IsNullOrWhiteSpace(jwtSection["Issuer"]) ? jwtSection["Issuer"] : "LogiSyn",
                ValidateAudience = true,
                ValidAudience = !string.IsNullOrWhiteSpace(jwtSection["Audience"]) ? jwtSection["Audience"] : "LogiSynClients",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2)
            };
        });

    // --- Orders & Product Services Registration ---
    builder.Services.AddScoped<SqlOrderRepository>();
    builder.Services.AddScoped<IProductService>(sp =>
    {
        var mongoRepo = sp.GetService<MongoProductRepository>();
        return new ProductService(mongoRepo);
    });
    builder.Services.AddScoped<ProductService>(sp => (ProductService)sp.GetRequiredService<IProductService>());
    builder.Services.AddScoped<ITempRecipeService, TempRecipeService>();
    builder.Services.AddScoped<TempRecipeService>();
    builder.Services.AddScoped<UserService>();
    builder.Services.AddScoped<LoginService>();
    builder.Services.AddScoped<OrderService>();
    builder.Services.AddScoped<IOrderService>(sp =>
    {
        var productService = sp.GetRequiredService<IProductService>();
        var mongoRepo = sp.GetService<MongoOrderRepository>();
        var sqlRepo = sp.GetService<SqlOrderRepository>();
        var recipeService = sp.GetService<ITempRecipeService>();
        return new OrderService(productService, mongoRepo, sqlRepo, recipeService);
    });

    builder.Services.AddControllers();
    // Register ApiClient via IHttpClientFactory and allow configuration-driven base URLs
    builder.Services.AddHttpClient<ApiClient>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(60);
    });
    builder.Services.AddOpenApi();
    // Lightweight Health Checks to support cloud probes and keep-alive pings
    builder.Services.AddHealthChecks();

    var app = builder.Build();

// --- Ensure SQL Server database schema exists (skip on Linux if LocalDB configured) ---
var sqlConn = builder.Configuration.GetConnectionString("SqlServer") ?? string.Empty;
bool isLocalDb = sqlConn.Contains("(localdb)", StringComparison.OrdinalIgnoreCase);

if (!OperatingSystem.IsWindows() && (isLocalDb || string.IsNullOrWhiteSpace(sqlConn)))
{
    var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
    startupLogger.LogInformation("Running in a Linux container with LocalDB configured. Skipping local SQL migrations; cloud API operating via MongoDB Atlas.");
}
else
{
    using (var scope = app.Services.CreateScope())
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var db = scope.ServiceProvider.GetRequiredService<LogiSynDbContext>();

        try
        {
            var conn = db.Database.GetDbConnection();
            conn.Open();

            using var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = @"
                SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_NAME IN ('Orders', 'Product', 'User') AND TABLE_SCHEMA = 'dbo'";
            int existingTableCount = (int)(checkCmd.ExecuteScalar() ?? 0);

            if (existingTableCount > 0)
            {
                using var historyCmd = conn.CreateCommand();
                historyCmd.CommandText = @"
                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '__EFMigrationsHistory')
                    BEGIN
                        CREATE TABLE [__EFMigrationsHistory] (
                            [MigrationId]    NVARCHAR(150) NOT NULL,
                            [ProductVersion] NVARCHAR(32)  NOT NULL,
                            CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
                        );
                    END

                    IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20261005191617_InitialCreate')
                    BEGIN
                        INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                        VALUES ('20261005191617_InitialCreate', '10.0.12');
                    END";
                historyCmd.ExecuteNonQuery();
                logger.LogInformation("Pre-existing schema detected; InitialCreate migration marked as applied.");
            }

            conn.Close();

            db.Database.Migrate();
            logger.LogInformation("SQL database migrations applied successfully.");
        }
        catch (PlatformNotSupportedException pex)
        {
            logger.LogWarning("LocalDB is not supported on this platform. Skipping SQL migrations: {Message}", pex.Message);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Could not apply SQL migrations or connect to SQL Server: {Message}. Cloud API will operate using MongoDB Atlas.", ex.Message);
        }
    }

    // Ensure Product table columns are sized to accept long text
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            var db = scope.ServiceProvider.GetRequiredService<LogiSynDbContext>();
            var conn = db.Database.GetDbConnection();
            try
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"IF EXISTS(SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Product' AND COLUMN_NAME='Method' AND (CHARACTER_MAXIMUM_LENGTH IS NOT NULL AND CHARACTER_MAXIMUM_LENGTH <> -1))
                                    BEGIN
                                        ALTER TABLE [Product] ALTER COLUMN [Method] NVARCHAR(MAX) NULL;
                                    END
                                    IF EXISTS(SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Product' AND COLUMN_NAME='Ingredients' AND (CHARACTER_MAXIMUM_LENGTH IS NOT NULL AND CHARACTER_MAXIMUM_LENGTH <> -1))
                                    BEGIN
                                        ALTER TABLE [Product] ALTER COLUMN [Ingredients] NVARCHAR(MAX) NULL;
                                    END";
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                logger.LogWarning("Could not alter Product columns. Continuing startup: {Message}", ex.Message);
            }
            finally
            {
                try { conn.Close(); } catch { }
            }
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("Ensure Product column sizing step skipped: {Message}", ex.Message);
        }
    }
}

// Expose OpenAPI endpoint only in development to avoid leaking schema in production
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Root discovery endpoint for health check and API documentation
app.MapGet("/api/status", () => Results.Ok(new
{
    service = "Anderson's Bakery Operations API",
    version = "1.0.0",
    status = "Online",
    health = "/health",
    documentation = "/openapi/v1.json"
}));

// Expose a simple health check endpoint for monitoring and keep-alive probes
app.MapHealthChecks("/health");

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();