using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace CosmosDbFeeder;

public static class Program
{
    /// <summary>
    /// Defines the entry point of the application.
    /// </summary>
    /// <remarks>
    /// This asynchronous method serves as the main entry point for the application. It sets up the logging and initializes the application host.
    /// The method begins by configuring the console title and starting the logger. It then creates a host for the application, which is responsible for managing the application's services and lifecycle.
    /// 
    /// Within a try block, the host is started asynchronously, and a scope is created to resolve the necessary services. The main worker service is retrieved and started, allowing it to perform its designated tasks.
    /// After the worker has completed its execution, the host is stopped asynchronously. 
    /// If any exceptions occur during this process, they are caught and logged as fatal errors, indicating that the application has terminated unexpectedly.
    /// Finally, regardless of success or failure, the logging system is closed and flushed to ensure all log entries are written out.
    /// </remarks>
    /// <exception cref="Exception">Thrown when an unexpected error occurs during application startup or execution.</exception>
    public static async Task Main()
    {
        StartupExtensions.SetDebugConsoleTitle(JobConstants.AppName);
        Log.Logger = Bootstrap.StartupLoggerProvider(Bootstrap.Configuration);
        Log.Information("Starting up ({ApplicationName})", JobConstants.AppName);

        try
        {
            var host = CreateDefaultBuilder().Build();
            await host.StartAsync();
            using var serviceScope = host.Services.CreateScope();
            var provider = serviceScope.ServiceProvider;
            var worker = provider.GetRequiredService<Worker>();

            await worker.Start();
            await host.StopAsync();

            Log.Information("Finishing ({ApplicationName})", JobConstants.AppName);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Program terminated unexpectedly ({AppName})!", JobConstants.AppName);
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    /// <summary>
    /// Creates the default builder for the host configuration.
    /// </summary>
    /// <returns>An instance of <see cref="IHostBuilder"/> that is configured with default settings.</returns>
    /// <remarks>
    /// This method initializes a new host builder with default configurations, which includes setting up the necessary services and configurations 
    /// required for the application to run. It calls the <see cref="Host.CreateDefaultBuilder"/> method to establish the foundational settings 
    /// and then further customizes the builder by invoking <see cref="ConfigureWorkerHostBuilder"/> to set up worker-specific configurations. 
    /// Finally, it registers a transient service for the <see cref="Worker"/> class, ensuring that a new instance of the worker is created 
    /// each time it is requested. This setup is essential for applications that utilize background services or worker processes.
    /// </remarks>
    private static IHostBuilder CreateDefaultBuilder() =>
        Host.CreateDefaultBuilder()
            .ConfigureWorkerHostBuilder()
            .ConfigureServices(services => services.AddTransient<Worker>());
}