using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace CosmosDbFeeder;

/// <summary>
/// Class StartupExtensions.
/// </summary>
[ExcludeFromCodeCoverage]
public static class StartupExtensions
{
    /// <summary>
    /// Sets the debug console title.
    /// </summary>
    /// <param name="title">The title.</param>
    [Conditional("DEBUG")]
    public static void SetDebugConsoleTitle(string title)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            Console.Title = title;
        }
    }
}
