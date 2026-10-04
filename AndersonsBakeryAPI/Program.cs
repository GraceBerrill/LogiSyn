using AndersonsBakeryAPI.Services;
using MongoDB.Driver;
using SharedLibrary.Interface;

var builder = WebApplication.CreateBuilder(args);

var mongoConnection = builder.Configuration.GetConnectionString("MongoConnection");

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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
