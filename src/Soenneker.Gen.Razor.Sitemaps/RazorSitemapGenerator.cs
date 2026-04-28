using Microsoft.CodeAnalysis;

namespace Soenneker.Gen.Razor.Sitemaps;

[Generator]
public sealed class RazorSitemapGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Generator runs only on build; no incremental output. BuildTasks handle Razor analysis and sitemap writing.
    }
}
