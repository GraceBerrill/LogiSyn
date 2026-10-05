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
    builder.Services.AddScoped<UserServiceRouter>();
    builder.Services.AddScoped<LoginServiceRouter>();
    builder.Services.AddScoped<SyncService>();

    // --- Jwt Authentication (reads settings from configuration) ---
    var jwtSection = builder.Configuration.GetSection("Jwt");
    var jwtKey = jwtSection["Key"] ?? string.Empty;
    if (!string.IsNullOrWhiteSpace(jwtKey))
    {
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ValidateIssuer = !string.IsNullOrWhiteSpace(jwtSection["Issuer"]),
                    ValidIssuer = jwtSection["Issuer"],
                    ValidateAudience = !string.IsNullOrWhiteSpace(jwtSection["Audience"]),
                    ValidAudience = jwtSection["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(2)
                };
            });
    }

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

// --- Adriaan: Ensure local SQL Server database schema exists ---
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var db = scope.ServiceProvider.GetRequiredService<LogiSynDbContext>();
    // Prefer migrations to ensure schema correctness. Fail fast if migrations cannot be applied.
    try
    {
        db.Database.Migrate();
        logger.LogInformation("SQL database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Failed to apply EF Core migrations. Startup cannot continue.");
        // Rethrow to stop application startup so deployment can detect and correct schema issues.
        throw;
    }
}

// Ensure Product table columns are sized to accept long text created by the model
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
            // If Method or Ingredients columns are not nvarchar(max), alter them to nvarchar(max)
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
            var logger2 = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger2.LogWarning(ex, "Could not alter Product columns to nvarchar(max). Continuing startup, but schema should be reviewed.");
        }
        finally
        {
            try { conn.Close(); } catch { }
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Ensure Product column sizing step failed.");
    }
}

// Expose OpenAPI endpoint only in development to avoid leaking schema in production
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Root discovery endpoint for health check and API documentation
app.MapGet("/", () => Results.Ok(new
{
    service = "Anderson's Bakery Operations API",
    version = "1.0.0",
    status = "Online",
    health = "/health",
    documentation = "/openapi/v1.json"
}));

// Expose a simple health check endpoint for monitoring and keep-alive probes
app.MapHealthChecks("/health");

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();