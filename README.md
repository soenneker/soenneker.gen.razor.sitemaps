[![](https://img.shields.io/nuget/v/soenneker.gen.razor.sitemaps.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.gen.razor.sitemaps/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.gen.razor.sitemaps/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.gen.razor.sitemaps/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.gen.razor.sitemaps.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.gen.razor.sitemaps/)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Gen.Razor.Sitemaps
### Automatic sitemap generation for Razor apps at build time. Fast, deterministic, and zero-runtime overhead.

## Installation

```
dotnet add package Soenneker.Gen.Razor.Sitemaps
```

## Usage

Enable sitemap generation in the app project:

```xml
<PropertyGroup>
  <RazorSitemapEnabled>true</RazorSitemapEnabled>
  <RazorSitemapBaseUrl>https://example.com</RazorSitemapBaseUrl>
  <RazorSitemapOutputPath>wwwroot/sitemap.xml</RazorSitemapOutputPath>
  <RazorSitemapIncludeUnannotatedPages>true</RazorSitemapIncludeUnannotatedPages>
</PropertyGroup>
```

Routes are discovered from `.razor` files with `@page`. Dynamic routes such as `/products/{id}` are skipped because they cannot be represented as concrete sitemap URLs.

Optional per-page metadata:

```razor
@page "/about"
@using Soenneker.Razor.Sitemap
@attribute [Sitemap(ChangeFrequency = "monthly", Priority = 0.8, LastModified = "2026-04-28")]
```

Use `Exclude = true` to omit a page, or `Url = "/custom-url"` to override the generated route URL. `lastmod` is emitted for every URL from the `.razor` file timestamp unless `LastModified` is set. `ChangeFrequency` and `Priority` are supported but omitted by default. The package injects `Soenneker.Razor.Sitemap.SitemapAttribute` at compile time, so no runtime dependency is required.
