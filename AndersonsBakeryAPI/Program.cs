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
        builder.Services.AddScoped<UserServiceRouter>();
        builder.Services.AddScoped<LoginServiceRouter>();
        builder.Services.AddScoped<SyncService>();
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
    // --- Orders & Product Services Registration ---
    builder.Services.AddScoped<MongoOrderRepository>();
    builder.Services.AddScoped<SqlOrderRepository>();
    builder.Services.AddScoped<ProductService>();
    builder.Services.AddScoped<TempRecipeService>();
    builder.Services.AddScoped<UserService>();
    builder.Services.AddScoped<LoginService>();
    builder.Services.AddScoped<OrderService>();
    builder.Services.AddScoped<IOrderService>(sp =>
    {
        var productService = sp.GetRequiredService<ProductService>();
        var mongoRepo = sp.GetService<MongoOrderRepository>();
        var sqlRepo = sp.GetService<SqlOrderRepository>();
        return new OrderService(productService, mongoRepo, sqlRepo);
    });

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    var app = builder.Build();

// --- Adriaan: Ensure local SQL Server database schema exists ---
using (var scope = app.Services.CreateScope())
{
	try
	{
		var db = scope.ServiceProvider.GetRequiredService<LogiSynDbContext>();
		db.Database.EnsureCreated();
	}
	catch (Exception ex)
	{
		Console.WriteLine($"[SQL] Warning: Unable to ensure SQL database created: {ex.Message}");
	}
}

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();