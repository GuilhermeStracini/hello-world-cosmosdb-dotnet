using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace CosmosDbFeeder;

/// <summary>
/// Class HostBuilderExtensions.
/// </summary>
[ExcludeFromCodeCoverage]
public static class HostBuilderExtensions
{
    /// <summary>
    /// Configures the worker host builder.
    /// </summary>
    /// <param name="hostBuilder">The host builder to be configured.</param>
    /// <returns>An instance of <see cref="IHostBuilder"/> that has been configured with the specified settings.</returns>
    /// <remarks>
    /// This method sets up the environment for the host builder by retrieving the current environment variable 
    /// "NETCORE_ENVIRONMENT". If the variable is not set, it defaults to "Development". Additionally, it configures 
    /// Serilog for logging by reading from the application's configuration and services, and enriching the log 
    /// context with additional information. This setup is essential for ensuring that the application runs 
    /// in the correct environment and has proper logging capabilities.
    /// </remarks>
    public static IHostBuilder ConfigureWorkerHostBuilder(this IHostBuilder hostBuilder)
    {
        return hostBuilder
            .UseEnvironment(
                Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT") ?? "Development"
            )
            .UseSerilog(
                (context, services, configuration) =>
                {
                    configuration.ReadFrom
                        .Configuration(context.Configuration)
                        .ReadFrom.Services(services)
                        .Enrich.FromLogContext();
                }
            );
    }
}
