using System.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyLibrary;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

namespace LoggingExamples
{
    internal class Program
    {
        // Define an ActivitySource for the application
        // This is the .NET idiomatic way to create activities
        private static readonly ActivitySource AppActivitySource = new("LoggingExamples.App", "1.0.0");

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

                    // Create an Activity for the main work cycle
                    // This becomes the parent activity
                    using var mainActivity = AppActivitySource.StartActivity("WorkCycle");
                    mainActivity?.SetTag("startTime", DateTimeOffset.Now.ToString("o"));

                    // Get the service from the scope
                    var myLibraryService = serviceScope.ServiceProvider.GetRequiredService<MyLibraryService>();

                    logger.LogInformation("Running work cycle at {Time}", DateTimeOffset.Now);

                    // Create a child activity for the library service work
                    // This will automatically become a child of the current activity (mainActivity)
                    using (var libraryActivity = AppActivitySource.StartActivity("LibraryServiceWork"))
                    {
                        libraryActivity?.SetTag("serviceType", "MyLibraryService");
                        await myLibraryService.DoWorkAsync();
                    }

                    // Create another child activity for post-processing
                    using (var postProcessingActivity = AppActivitySource.StartActivity("PostProcessing"))
                    {
                        postProcessingActivity?.SetTag("processingTime", DateTimeOffset.Now.ToString("o"));
                        logger.LogInformation("Post-processing work");
                        await Task.Delay(100); // Simulate some post-processing work
                    }

                    mainActivity?.SetTag("endTime", DateTimeOffset.Now.ToString("o"));

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

                options.IncludeFormattedMessage = true;
                options.IncludeScopes = true;

                options
                    .SetResourceBuilder(resourceBuilder)                   
                    .AddOtlpExporter(otlpOptions =>
                    {
                        otlpOptions.Endpoint = new Uri("http://localhost:4318");
                        otlpOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf; // Changed from gRPC to HTTP
                        otlpOptions.ExportProcessorType = OpenTelemetry.ExportProcessorType.Simple;
                    });
            });
        }
    }
}
