using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace CosmosDbFeeder;

/// <summary>
/// Class Bootstrap.
/// </summary>
[ExcludeFromCodeCoverage]
public static class Bootstrap
{
    /// <summary>
    /// Gets or sets the configuration.
    /// </summary>
    /// <value>The configuration.</value>
    public static IConfiguration Configuration { get; set; } =
        new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile(
                $"appsettings.{Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT") ?? "Development"}.json",
                optional: true,
                reloadOnChange: true
            )
            .AddEnvironmentVariables()
            .Build();

    /// <summary>
    /// Gets or sets the startup logger provider.
    /// </summary>
    /// <value>The startup logger provider.</value>
    public static Func<IConfiguration, ILogger> StartupLoggerProvider { get; set; } =
        (configuration) =>
            new LoggerConfiguration().ReadFrom
                .Configuration(configuration)
                .Enrich.FromLogContext()
                .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
                .CreateLogger();
}
