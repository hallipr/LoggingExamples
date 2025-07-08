using Microsoft.AspNetCore.Builder;
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:4318");
var app = builder.Build();

// Single POST handler for any path
app.MapPost("/{**path}", async context =>
{
    string path = context.Request.Path.Value ?? "/";
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    
    Console.WriteLine($"Received POST {path}:");
    Console.WriteLine(body);
    
    context.Response.StatusCode = 200;
});

// OPTIONS handler for CORS preflight requests
app.MapMethods("/{**path}", new[] { "OPTIONS" }, context =>
{
    string path = context.Request.Path.Value ?? "/";
    
    System.Console.WriteLine($"Received OPTIONS {path}");
    
    // Set CORS headers
    context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
    context.Response.Headers.Append("Access-Control-Allow-Methods", "POST, OPTIONS");
    context.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type");
    
    context.Response.StatusCode = 204; // No Content is the standard response for OPTIONS
    
    return Task.CompletedTask;
});

app.Run();
