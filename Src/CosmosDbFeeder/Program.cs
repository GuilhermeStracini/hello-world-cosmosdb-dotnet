using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace CosmosDbFeeder;

public static class Program
{
    /// <summary>
    /// Defines the entry point of the application.
    /// </summary>
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
    /// Creates the default builder.
    /// </summary>
    /// <returns>IHostBuilder.</returns>
    private static IHostBuilder CreateDefaultBuilder() =>
        Host.CreateDefaultBuilder()
            .ConfigureWorkerHostBuilder()
            .ConfigureServices(services => services.AddTransient<Worker>());
}