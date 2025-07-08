using Microsoft.Extensions.Logging;

namespace MyLibrary
{
    public class MyLibraryService
    {
        private readonly ILogger _logger;

        public MyLibraryService(ILogger<MyLibraryService> logger)
        {
            _logger = logger;
        }

        public Task DoWorkAsync()
        {
            _logger.LogInformation("Doing work in MyLibraryService.");
            _logger.LogWarning("This is a warning from MyLibraryService.");
            _logger.LogError("This is an error from MyLibraryService.");
            return Task.CompletedTask;
        }
    }
}
