using Microsoft.CodeAnalysis;

namespace Soenneker.Gen.Razor.Sitemaps;

/// <summary>
/// Represents the razor sitemap generator.
/// </summary>
[Generator]
public sealed class RazorSitemapGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Executes the initialize operation.
    /// </summary>
    /// <param name="context">The context.</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Generator runs only on build; no incremental output. BuildTasks handle Razor analysis and sitemap writing.
    }
}
