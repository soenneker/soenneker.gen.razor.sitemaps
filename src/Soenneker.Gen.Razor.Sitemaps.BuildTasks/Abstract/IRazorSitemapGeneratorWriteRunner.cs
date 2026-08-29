using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Gen.Razor.Sitemaps.BuildTasks.Abstract;

/// <summary>
/// Defines the razor sitemap generator write runner contract.
/// </summary>
public interface IRazorSitemapGeneratorWriteRunner
{
    /// <summary>
    /// Runs razor sitemap generator write runner for the razor sitemap generator write runner.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task whose result is the requested value.</returns>
    ValueTask<int> Run(string[] args, CancellationToken cancellationToken);
}
