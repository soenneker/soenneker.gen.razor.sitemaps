using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Gen.Razor.Sitemaps.BuildTasks.Abstract;

public interface IRazorSitemapGeneratorWriteRunner
{
    ValueTask<int> Run(string[] args, CancellationToken cancellationToken);
}
