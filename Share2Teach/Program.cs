using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using DatabaseConnection; // Ensure this namespace is included

var builder = WebApplication.CreateBuilder(args);

// Configure MongoDB settings
builder.Services.AddSingleton<IMongoClient>(s =>
{
    var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDb");
    return new MongoClient(mongoConnectionString);
});

builder.Services.AddSingleton<IMongoDatabase>(s =>
{
    var client = s.GetRequiredService<IMongoClient>();
    var databaseName = builder.Configuration.GetValue<string>("DatabaseName");
    return client.GetDatabase(databaseName);
});

// Add services to the container
builder.Services.AddControllers();

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Share2Teach API", Version = "v1" });
    c.OperationFilter<FileUploadOperationFilter>();
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Share2Teach API v1"));
    app.MapGet("/", async context =>
    {
        context.Response.Redirect("/swagger");
    });
}

app.UseAuthorization();
app.MapControllers();

app.Run();
