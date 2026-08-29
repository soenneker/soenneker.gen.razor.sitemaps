using Microsoft.CodeAnalysis;

namespace Soenneker.Gen.Razor.Sitemaps;

/// <summary>
/// Represents the razor sitemap generator.
/// </summary>
[Generator]
public sealed class RazorSitemapGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Initializes the razor sitemap generator so it is ready for use.
    /// </summary>
    /// <param name="context">HTTP context containing the Authorization header.</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Generator runs only on build; no incremental output. BuildTasks handle Razor analysis and sitemap writing.
    }
}
