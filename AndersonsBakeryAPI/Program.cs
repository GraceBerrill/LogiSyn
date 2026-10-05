// Adriaan
using AndersonsBakeryAPI.Services;
using AndersonsBakeryAPI.Data;
using AndersonsBakeryAPI.Repositories;
using MongoDB.Driver;
using SharedLibrary.Interface;
using Microsoft.EntityFrameworkCore;

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
    builder.Services.AddOpenApi();
    // Lightweight Health Checks to support cloud probes and keep-alive pings
    builder.Services.AddHealthChecks();

    var app = builder.Build();

// --- Adriaan: Ensure local SQL Server database schema exists ---
using (var scope = app.Services.CreateScope())
{
	try
	{
        var db = scope.ServiceProvider.GetRequiredService<LogiSynDbContext>();
        // Use EF Core migrations if available; fall back to EnsureCreated for very early setups
        try
        {
            db.Database.Migrate();
        }
        catch (Exception)
        {
            db.Database.EnsureCreated();
        }
	}
	catch (Exception ex)
	{
		Console.WriteLine($"[SQL] Warning: Unable to ensure SQL database created: {ex.Message}");
	}
}

// Expose OpenAPI endpoint in all environments (Development & Render Cloud) for examiner inspection
app.MapOpenApi();

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
app.UseAuthorization();
app.MapControllers();

app.Run();