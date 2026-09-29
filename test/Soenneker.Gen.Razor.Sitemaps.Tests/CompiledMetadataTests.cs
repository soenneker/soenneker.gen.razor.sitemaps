using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Soenneker.Gen.Razor.Sitemaps.BuildTasks;
using Soenneker.Razor.Sitemap;
using Soenneker.Utils.Directory.Registrars;
using Soenneker.Utils.File.Registrars;

namespace Soenneker.Gen.Razor.Sitemaps.Tests;

public sealed class CompiledMetadataTests
{
    [Test]
    public async Task Reads_compiled_attributes_without_dependencies(CancellationToken cancellationToken)
    {
        string directory = Path.Combine(Path.GetTempPath(), "sitemap-metadata-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string assembly = Path.Combine(directory, "components.dll");
            File.Copy(typeof(CompiledMetadataTests).Assembly.Location, assembly);
            string output = Path.Combine(directory, "sitemap.xml");
            string[] args = ["--targetPath", assembly, "--projectDir", directory, "--outputPath", output,
                "--baseUrl", "https://example.com", "--includeUnannotatedPages", "true"];
            string? nativeTool = Environment.GetEnvironmentVariable("SITEMAP_NATIVE_TOOL");
            if (!string.IsNullOrEmpty(nativeTool))
            {
                var startInfo = new ProcessStartInfo(nativeTool) { UseShellExecute = false };
                foreach (string arg in args)
                    startInfo.ArgumentList.Add(arg);
                using Process process = Process.Start(startInfo)!;
                await process.WaitForExitAsync(cancellationToken);
                if (process.ExitCode != 0)
                    throw new InvalidOperationException($"Native generator exited with {process.ExitCode}.");
            }
            else
            {
                await using ServiceProvider services = new ServiceCollection().AddLogging()
                    .AddFileUtilAsSingleton().AddDirectoryUtilAsSingleton()
                    .AddSingleton<RazorSitemapGeneratorWriteRunner>().BuildServiceProvider();
                int result = await services.GetRequiredService<RazorSitemapGeneratorWriteRunner>().Run(args, cancellationToken);
                if (result != 0)
                    throw new InvalidOperationException($"Generator exited with {result}.");
            }

            XDocument document = XDocument.Load(output);
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            string[] locations = document.Descendants(ns + "loc").Select(x => x.Value).Order().ToArray();
            string[] expected = ["https://example.com/compiled", "https://example.com/other", "https://example.com/static"];
            if (!locations.SequenceEqual(expected))
                throw new InvalidOperationException($"Unexpected compiled routes: {document}");
            XElement annotated = document.Descendants(ns + "url").Single(x => x.Element(ns + "loc")!.Value.EndsWith("/static", StringComparison.Ordinal));
            if (annotated.Element(ns + "priority")?.Value != "0.8" ||
                annotated.Element(ns + "changefreq")?.Value != "weekly" ||
                annotated.Element(ns + "lastmod")?.Value != "2026-09-29")
                throw new InvalidOperationException($"Compiled sitemap metadata was lost: {document}");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}

[Route("/compiled")]
[Route("/other")]
internal sealed class CompiledPage;

[Route("/dynamic/{id}")]
[Sitemap(Url = "/static", Priority = 0.8, ChangeFrequency = "weekly", LastModified = "2026-09-29")]
internal sealed class AnnotatedPage;

[Route("/hidden")]
[Sitemap(Exclude = true)]
internal sealed class HiddenPage;

[Route("/ignored/{id}")]
internal sealed class DynamicPage;
