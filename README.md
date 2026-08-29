[![](https://img.shields.io/nuget/v/soenneker.gen.razor.sitemaps.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.gen.razor.sitemaps/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.gen.razor.sitemaps/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.gen.razor.sitemaps/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.gen.razor.sitemaps.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.gen.razor.sitemaps/)

# Soenneker.Gen.Razor.Sitemaps

Defines the razor sitemap generator write runner contract.

## Install

```bash
dotnet add package Soenneker.Gen.Razor.Sitemaps
```

## Quick start

```csharp
using Soenneker.Gen.Razor.Sitemaps.BuildTasks.Abstract;

IRazorSitemapGeneratorWriteRunner razorSitemapGeneratorWriteRunner = /* resolve from DI */;
var result = await razorSitemapGeneratorWriteRunner.Run("value", default);
```

Runs razor sitemap generator write runner for the razor sitemap generator write runner.

## What you get

- `IRazorSitemapGeneratorWriteRunner` — Defines the razor sitemap generator write runner contract.
- `Startup` — Represents the startup.
- `BuildTasksCommandLineArgs` — Represents the build tasks command line args.
- `ConsoleHostedService` — Represents the console hosted service.
- `Program` — Represents the program.
- `RazorSitemapGenerator` — Represents the razor sitemap generator.
- `SitemapAttribute` — Represents the sitemap attribute.

## API at a glance

| API | What it does | Result / important behavior |
| --- | --- | --- |
| `BuildTasksCommandLineArgs.Args` | Gets args. | Gets args. |
| `ConsoleHostedService.StartAsync(cancellationToken)` | Starts the console hosted service and begins its background work. | A task that completes after the console hosted service has started. |
| `ConsoleHostedService.StopAsync(cancellationToken)` | Stops the console hosted service and waits for its background work to finish. | A task that completes after the console hosted service has stopped. |
| `Program.Main(args)` | Runs the application using the supplied command-line arguments. | A task that completes when the application exits. |
| `RazorSitemapGenerator.Initialize(context)` | Initializes the razor sitemap generator so it is ready for use. | Returns no value; the requested change is complete when the method returns. |
| `SitemapAttribute.Exclude` | Gets or sets a value indicating whether exclude. | Gets or sets a value indicating whether exclude. |
| `SitemapAttribute.Url` | Gets or sets url. | Gets or sets url. |
| `SitemapAttribute.ChangeFrequency` | Gets or sets change frequency. | Gets or sets change frequency. |
| `SitemapAttribute.Priority` | Gets or sets priority. | Gets or sets priority. |
| `SitemapAttribute.LastModified` | Gets or sets last modified. | Gets or sets last modified. |

## Practical notes

- Cancellation stops pending work; it does not undo work that has already completed.
