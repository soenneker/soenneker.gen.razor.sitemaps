using Microsoft.Extensions.DependencyInjection;
using Soenneker.Gen.Razor.Sitemaps.BuildTasks.Abstract;
using Soenneker.Utils.Directory.Registrars;
using Soenneker.Utils.File.Registrars;
using Soenneker.Utils.MemoryStream.Registrars;

namespace Soenneker.Gen.Razor.Sitemaps.BuildTasks;

/// <summary>
/// Represents the startup.
/// </summary>
public static class Startup
{
    /// <summary>
    /// Configures services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddFileUtilAsSingleton()
                .AddDirectoryUtilAsSingleton()
                .AddMemoryStreamUtilAsSingleton()
                .AddSingleton<IRazorSitemapGeneratorWriteRunner, RazorSitemapGeneratorWriteRunner>();
        services.AddHostedService<ConsoleHostedService>();
    }
}
