using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Gen.Razor.Sitemaps.BuildTasks.Abstract;

/// <summary>
/// Runs the Razor sitemap build task from its command-line arguments.
/// </summary>
public interface IRazorSitemapGeneratorWriteRunner
{
    /// <summary>
    /// Discovers Razor routes and writes the configured sitemap document.
    /// </summary>
    /// <param name="args">Generator command-line arguments supplied by the MSBuild target.</param>
    /// <param name="cancellationToken">Cancels discovery or output.</param>
    /// <returns>Zero when generation succeeds; otherwise a nonzero process exit code.</returns>
    ValueTask<int> Run(string[] args, CancellationToken cancellationToken);
}
