using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Soenneker.Gen.Razor.Sitemaps.BuildTasks;
using Soenneker.Utils.Directory.Abstract;
using Soenneker.Utils.Directory.Registrars;
using Soenneker.Utils.File.Abstract;
using Soenneker.Utils.File.Registrars;
using Soenneker.Tests.Unit;

namespace Soenneker.Gen.Razor.Sitemaps.Tests;

public sealed class RazorSitemapGeneratorTests : UnitTest
{
    [Test]
    public async ValueTask Generates_sitemap_from_razor_pages()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "soenneker-razor-sitemap-" + Guid.NewGuid().ToString("N"));
        string outputPath = Path.Combine(tempDir, "sitemap.xml");

        await using ServiceProvider serviceProvider = new ServiceCollection()
            .AddLogging(builder => builder.AddConsole())
            .AddFileUtilAsSingleton()
            .AddDirectoryUtilAsSingleton()
            .AddSingleton<RazorSitemapGeneratorWriteRunner>()
            .BuildServiceProvider();

        IFileUtil fileUtil = serviceProvider.GetRequiredService<IFileUtil>();
        IDirectoryUtil directoryUtil = serviceProvider.GetRequiredService<IDirectoryUtil>();
        string testProjectDir = await FindTestProjectDir(directoryUtil, CancellationToken.None);

        try
        {
            await directoryUtil.Create(tempDir, log: false, cancellationToken: CancellationToken.None);

            var runner = serviceProvider.GetRequiredService<RazorSitemapGeneratorWriteRunner>();
            int exitCode = await runner.Run(new[]
            {
                "--projectDir", testProjectDir,
                "--baseUrl", "https://example.com",
                "--outputPath", outputPath,
                "--includeUnannotatedPages", "true"
            }, CancellationToken.None);

            if (exitCode != 0)
                throw new InvalidOperationException($"Runner exited with {exitCode}");

            string sitemap = await fileUtil.Read(outputPath, log: false, cancellationToken: CancellationToken.None);

            if (!sitemap.StartsWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>", StringComparison.Ordinal))
                throw new InvalidOperationException("Sitemap was not written as UTF-8 XML.");

            XDocument document = XDocument.Parse(sitemap);
            string[] locations = document.Descendants()
                                         .Where(element => element.Name.LocalName == "loc")
                                         .Select(element => element.Value)
                                         .ToArray();

            if (!locations.Contains("https://example.com/", StringComparer.Ordinal))
                throw new InvalidOperationException($"Root route was not generated. Sitemap:{Environment.NewLine}{sitemap}");

            if (!ContainsElementValue(document, "changefreq", "daily") ||
                !ContainsElementValue(document, "priority", "1.0") ||
                !ContainsElementValue(document, "lastmod", "2026-04-28"))
            {
                throw new InvalidOperationException($"Annotated metadata was not generated. Sitemap:{Environment.NewLine}{sitemap}");
            }

            if (!locations.Contains("https://example.com/about", StringComparer.Ordinal))
                throw new InvalidOperationException($"Unannotated page was not generated. Sitemap:{Environment.NewLine}{sitemap}");

            if (!locations.Contains("https://example.com/search?q=test&page=1", StringComparer.Ordinal) ||
                !sitemap.Contains("https://example.com/search?q=test&amp;page=1", StringComparison.Ordinal))
                throw new InvalidOperationException($"URL values were not XML escaped. Sitemap:{Environment.NewLine}{sitemap}");

            if (!sitemap.Contains("<lastmod>", StringComparison.Ordinal))
                throw new InvalidOperationException("File-derived lastmod was not generated.");

            if (sitemap.Contains("<changefreq>weekly</changefreq>", StringComparison.Ordinal) || sitemap.Contains("<priority>0.7</priority>", StringComparison.Ordinal))
                throw new InvalidOperationException("Default changefreq or priority was generated.");

            if (sitemap.Contains("hidden", StringComparison.OrdinalIgnoreCase) || sitemap.Contains("products", StringComparison.OrdinalIgnoreCase) ||
                sitemap.Contains("not-found", StringComparison.OrdinalIgnoreCase) || sitemap.Contains("error", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Excluded, default-excluded, or dynamic route was generated.");
        }
        finally
        {
            await directoryUtil.DeleteIfExists(tempDir, CancellationToken.None);
        }
    }

    private static async ValueTask<string> FindTestProjectDir(IDirectoryUtil directoryUtil, CancellationToken cancellationToken)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, "TestRazorPages");
            if (await directoryUtil.Exists(candidate, cancellationToken))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate TestRazorPages fixture directory.");
    }

    private static bool ContainsElementValue(XContainer document, string localName, string value)
    {
        return document.Descendants()
                       .Any(element => element.Name.LocalName == localName && string.Equals(element.Value, value, StringComparison.Ordinal));
    }
}
