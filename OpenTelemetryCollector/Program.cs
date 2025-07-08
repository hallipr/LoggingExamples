using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:4318");
var app = builder.Build();

app.MapPost("/v1/traces", async context =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    System.Console.WriteLine("Received traces:");
    System.Console.WriteLine(body);
    context.Response.StatusCode = 200;
});

app.MapPost("/v1/metrics", async context =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    System.Console.WriteLine("Received metrics:");
    System.Console.WriteLine(body);
    context.Response.StatusCode = 200;
});

app.MapPost("/v1/logs", async context =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    System.Console.WriteLine("Received logs:");
    System.Console.WriteLine(body);
    context.Response.StatusCode = 200;
});

app.Run();
