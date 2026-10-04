using AndersonsBakeryAPI.Services;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

var mongoConn = builder.Configuration.GetConnectionString("MongoConnection")
    ?? throw new InvalidOperationException("Missing 'MongoConnection' connection string.");

_ = builder.Configuration["MongoDatabase"]
    ?? throw new InvalidOperationException("Missing 'MongoDatabase' value.");

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConn));
builder.Services.AddScoped<MongoUserService>();
builder.Services.AddScoped<MongoLoginService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<LoginService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<TempRecipeService>();
builder.Services.AddScoped<UserServiceRouter>();
builder.Services.AddScoped<LoginServiceRouter>();
builder.Services.AddScoped<SyncService>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();