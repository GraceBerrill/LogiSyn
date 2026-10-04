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

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

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
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
