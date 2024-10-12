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
    /// <param name="hostBuilder">The host builder.</param>
    /// <returns>IHostBuilder.</returns>
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
