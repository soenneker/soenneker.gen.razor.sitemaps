using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Gen.Razor.Sitemaps.Tests;

public sealed class PackageAttributeCompilationTests
{
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Razor_consumer_compiles_with_package_imports(bool disableAttributeCompile, CancellationToken cancellationToken)
    {
        DirectoryInfo? repository = new(AppContext.BaseDirectory);
        while (repository != null && !Directory.Exists(Path.Combine(repository.FullName, "src", "Soenneker.Gen.Razor.Sitemaps")))
            repository = repository.Parent;

        if (repository == null)
            throw new InvalidOperationException("Could not locate sitemap package sources.");

        string source = Path.Combine(repository.FullName, "src", "Soenneker.Gen.Razor.Sitemaps");
        string directory = Path.Combine(Path.GetTempPath(), "sitemap-consumer-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        try
        {
            // Reproduce both NuGet import locations, including the packaged generated-source filename.
            foreach (string folder in new[] { "build", "buildTransitive" })
            {
                string destination = Path.Combine(directory, folder);
                Directory.CreateDirectory(destination);
                foreach (string extension in new[] { "props", "targets" })
                    File.Copy(Path.Combine(source, "Soenneker.Gen.Razor.Sitemaps." + extension),
                        Path.Combine(destination, "Soenneker.Gen.Razor.Sitemaps." + extension));

                File.Copy(Path.Combine(source, "SitemapAttribute.cs"),
                    Path.Combine(destination, "Soenneker.Gen.Razor.Sitemaps.SitemapAttribute.g.cs"));
            }

            string project = $$"""
                <Project>
                  <Import Project="Sdk.props" Sdk="Microsoft.NET.Sdk.Razor" />
                  <Import Project="build/Soenneker.Gen.Razor.Sitemaps.props" />
                  <Import Project="buildTransitive/Soenneker.Gen.Razor.Sitemaps.props" />
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <Nullable>enable</Nullable>
                    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
                    <DefaultItemExcludes>$(DefaultItemExcludes);build/**;buildTransitive/**</DefaultItemExcludes>
                    <RazorSitemapDisableAttributeCompile>{{disableAttributeCompile.ToString().ToLowerInvariant()}}</RazorSitemapDisableAttributeCompile>
                  </PropertyGroup>
                  <ItemGroup>
                    <FrameworkReference Include="Microsoft.AspNetCore.App" />
                  </ItemGroup>
                  <Import Project="Sdk.targets" Sdk="Microsoft.NET.Sdk.Razor" />
                  <Import Project="build/Soenneker.Gen.Razor.Sitemaps.targets" />
                  <Import Project="buildTransitive/Soenneker.Gen.Razor.Sitemaps.targets" />
                </Project>
                """;
            await File.WriteAllTextAsync(Path.Combine(directory, "Consumer.csproj"), project, cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(directory, "Page.razor"), """
                @page "/example"
                @attribute [Soenneker.Razor.Sitemap.Sitemap(Url = "/canonical/")]
                <p>Example</p>
                """, cancellationToken);
            if (disableAttributeCompile)
                File.Copy(Path.Combine(source, "SitemapAttribute.cs"), Path.Combine(directory, "SitemapAttribute.cs"));

            var startInfo = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = directory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("build");
            startInfo.ArgumentList.Add("Consumer.csproj");
            startInfo.ArgumentList.Add("--nologo");
            using Process process = Process.Start(startInfo)!;
            Task<string> output = process.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> error = process.StandardError.ReadToEndAsync(cancellationToken);
            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            finally
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    await process.WaitForExitAsync(CancellationToken.None);
                }
            }

            string diagnostics = await output + await error;
            if (process.ExitCode != 0)
                throw new InvalidOperationException($"Razor package consumer failed to compile: {diagnostics}");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
