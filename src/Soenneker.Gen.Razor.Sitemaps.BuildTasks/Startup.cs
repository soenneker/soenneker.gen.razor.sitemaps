using Microsoft.Extensions.DependencyInjection;
using Soenneker.Gen.Razor.Sitemaps.BuildTasks.Abstract;
using Soenneker.Utils.Directory.Registrars;
using Soenneker.Utils.File.Registrars;

namespace Soenneker.Gen.Razor.Sitemaps.BuildTasks;

public static class Startup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddFileUtilAsSingleton().AddDirectoryUtilAsSingleton().AddSingleton<IRazorSitemapGeneratorWriteRunner, RazorSitemapGeneratorWriteRunner>();
        services.AddHostedService<ConsoleHostedService>();
    }
}
