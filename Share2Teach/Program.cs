using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MongoDB.Driver;
using Microsoft.AspNetCore.Hosting; //for logging 
using Microsoft.Extensions.Hosting; 
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Creating Serilog for file logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

// Add Serilog to logging pipeline
builder.Host.UseSerilog();

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

// JWT Configuration
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"]);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(secretKey),
        ClockSkew = TimeSpan.Zero // Optional: Set clock skew to zero for precise expiration handling
    };
});

// Add services to the container
builder.Services.AddControllers();

// Register GoogleAnalyticsService as a singleton service
builder.Services.AddSingleton<GoogleAnalyticsService>(); // This line adds your Google Analytics service

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Exception handling
try
{
    Log.Information("Starting web host");

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

    // Add the authentication middleware
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.Run();
    app.UseStaticFiles();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed. Program.cs file");
}
finally
{
    Log.CloseAndFlush();
}

app.UseStaticFiles();
