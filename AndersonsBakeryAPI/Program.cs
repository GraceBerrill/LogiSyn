using AndersonsBakeryAPI.Services;
using LogiSyn.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<LoginService>();

var mongoConn = builder.Configuration.GetConnectionString("MongoConnection")!;
var mongoDb = builder.Configuration.GetConnectionString("MongoDatabase")!;

builder.Services.AddScoped<MongoUserService>(_ => new MongoUserService(mongoConn, mongoDb));
builder.Services.AddScoped<MongoLoginService>(_ => new MongoLoginService(mongoConn, mongoDb));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
