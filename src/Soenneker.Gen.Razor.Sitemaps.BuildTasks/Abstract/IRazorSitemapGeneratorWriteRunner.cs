using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Gen.Razor.Sitemaps.BuildTasks.Abstract;

/// <summary>
/// Defines the razor sitemap generator write runner contract.
/// </summary>
public interface IRazorSitemapGeneratorWriteRunner
{
    /// <summary>
    /// Executes the run operation.
    /// </summary>
    /// <param name="args">The args.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the result of the operation.</returns>
    ValueTask<int> Run(string[] args, CancellationToken cancellationToken);
}
