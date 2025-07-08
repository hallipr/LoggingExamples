using System.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyLibrary;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

namespace LoggingExamples
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Create service collection
            var services = new ServiceCollection();

            // Configure logging
            services
                .AddTransient<MyLibraryService>()
                .AddLogging(ConfigureLogging);


            // Build the service provider
            using var serviceProvider = services.BuildServiceProvider();

            // Get the logger for Program class
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();


            logger.LogInformation("Starting application");

            // Simple loop to run work periodically
            var stopRunning = new CancellationTokenSource();

            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                stopRunning.Cancel();
                logger.LogInformation("Stopping application");
            };

            logger.LogInformation("Press Ctrl+C to exit");

            while (!stopRunning.IsCancellationRequested)
            {
                try
                {
                    // Create a logger scope for each work cycle
                    using var loggerScope = logger.BeginScope("WorkCycleScope");

                    // Log the start of the work cycle
                    logger.LogInformation("Starting work cycle at {Time}", DateTimeOffset.Now);

                    // Create a service scope for each work cycle
                    using var serviceScope = serviceProvider.CreateScope();

                    // Create an Activity for tracing

                    

                    // Get the service from the scope
                    var myLibraryService = serviceScope.ServiceProvider.GetRequiredService<MyLibraryService>();

                    logger.LogInformation("Running work cycle at {Time}", DateTimeOffset.Now);
                    await myLibraryService.DoWorkAsync();

                    // Wait for 5 seconds before next cycle
                    await Task.Delay(5000);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error during work cycle");
                }
            }

            // Allow time for telemetry to be flushed
            logger.LogInformation("Shutting down");
            await Task.Delay(1000);
        }

        private static void ConfigureLogging(ILoggingBuilder builder)
        {
            // Add console logging
            builder.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss ";
            });

            // Add OpenTelemetry
            builder.AddOpenTelemetry(options =>
            {
                var resourceBuilder = ResourceBuilder.CreateDefault()
                    .AddService(serviceName: "LoggingExamples", serviceVersion: "1.0.0");

                options
                    .SetResourceBuilder(resourceBuilder)
                    .AddOtlpExporter(otlpOptions =>
                    {
                        otlpOptions.Endpoint = new Uri("http://localhost:4318");
                    });
            });
        }
    }
}
