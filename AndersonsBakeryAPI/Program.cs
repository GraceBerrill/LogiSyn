<<<<<<< HEAD
using AndersonsBakeryAPI.Services;
using MongoDB.Driver;
using SharedLibrary.Interface;

var builder = WebApplication.CreateBuilder(args);

var mongoConnection = builder.Configuration.GetConnectionString("MongoConnection");
=======
// Adriaan
using MongoDB.Driver;
using AndersonsBakeryAPI.Data;
using AndersonsBakeryAPI.Repositories;
using LogiSyn.Interface;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("MongoDb");
var sqlConnectionString = builder.Configuration.GetConnectionString("SqlServer");

var mongoSettings = MongoClientSettings.FromConnectionString(connectionString);
mongoSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(3);
mongoSettings.ConnectTimeout = TimeSpan.FromSeconds(3);
builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoSettings));

builder.Services.AddScoped<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase("LogiSynDb");
});

// Add DbContext for SQL Server
builder.Services.AddDbContext<LogiSynDbContext>(options =>
    options.UseSqlServer(sqlConnectionString));

// Adriaan - Orders Repositories and Services Registration
// Register repositories
builder.Services.AddScoped<MongoOrderRepository>();
builder.Services.AddScoped<SqlOrderRepository>();

// Register services
builder.Services.AddScoped<LogiSyn.Services.ProductService>();
builder.Services.AddScoped<IOrderService>(sp =>
{
    var productService = sp.GetRequiredService<LogiSyn.Services.ProductService>();
    var mongoRepo = sp.GetRequiredService<MongoOrderRepository>();
    var sqlRepo = sp.GetRequiredService<SqlOrderRepository>();
    return new LogiSyn.Services.OrderService(productService, mongoRepo, sqlRepo);
});

// Add services to the container.
>>>>>>> Adriaan

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<LoginService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<TempRecipeService>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

if (!string.IsNullOrWhiteSpace(mongoConnection))
{
    builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnection));
    builder.Services.AddScoped<MongoUserService>();
    builder.Services.AddScoped<MongoLoginService>();
    builder.Services.AddScoped<UserServiceRouter>();
    builder.Services.AddScoped<LoginServiceRouter>();
    builder.Services.AddScoped<SyncService>();
}

var app = builder.Build();

<<<<<<< HEAD
=======
// Ensure local SQL Server database schema exists
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

// Configure the HTTP request pipeline.
>>>>>>> Adriaan
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
