[![](https://img.shields.io/nuget/v/soenneker.gen.razor.sitemaps.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.gen.razor.sitemaps/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.gen.razor.sitemaps/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.gen.razor.sitemaps/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.gen.razor.sitemaps.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.gen.razor.sitemaps/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.gen.razor.sitemaps/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.gen.razor.sitemaps/actions/workflows/codeql.yml)

# Soenneker.Gen.Razor.Sitemaps

Build-time sitemap generation for Razor and Blazor applications. It discovers component routes, applies optional per-component metadata, and writes a deterministic XML sitemap after the application builds.

## Install

```bash
dotnet add package Soenneker.Gen.Razor.Sitemaps
```

## Configure the project

Generation is opt-in. Set an absolute public base URL because sitemap locations must be absolute HTTP or HTTPS URLs:

```xml
<PropertyGroup>
  <RazorSitemapEnabled>true</RazorSitemapEnabled>
  <RazorSitemapBaseUrl>https://www.example.com</RazorSitemapBaseUrl>
  <RazorSitemapOutputPath>wwwroot/sitemap.xml</RazorSitemapOutputPath>
  <RazorSitemapIncludeUnannotatedPages>true</RazorSitemapIncludeUnannotatedPages>
</PropertyGroup>
```

`RazorSitemapOutputPath` may be absolute or relative to the consuming project. Optional defaults can be supplied with `RazorSitemapDefaultChangeFrequency` and `RazorSitemapDefaultPriority`.

No service registration or runtime call is required. The package runs from its MSBuild target after a successful application build.

## Add route metadata

The package makes `Soenneker.Razor.Sitemap.SitemapAttribute` available to the consuming project. Apply it to a routable component with Razor's `@attribute` directive:

```razor
@page "/about"
@using Soenneker.Razor.Sitemap
@attribute [Sitemap(ChangeFrequency = "monthly", Priority = 0.7)]

<PageTitle>About</PageTitle>
```

Available metadata:

- `Exclude = true` omits every route declared by that component.
- `Url` replaces the declared route with a static relative or absolute URL. This is how a dynamic route can be represented by a crawlable URL.
- `ChangeFrequency` accepts `always`, `hourly`, `daily`, `weekly`, `monthly`, `yearly`, or `never`.
- `Priority` accepts a value from `0` through `1`.
- `LastModified` accepts a W3C date or date-time. When omitted, the component file's UTC modification date is used.

For example, exclude a private page:

```razor
@page "/preview"
@using Soenneker.Razor.Sitemap
@attribute [Sitemap(Exclude = true)]
```

Or give a parameterized component a concrete canonical URL:

```razor
@page "/products/{id:int}"
@using Soenneker.Razor.Sitemap
@attribute [Sitemap(Url = "/products/featured")]
```

## Discovery behavior

- Unannotated pages are included by default. Set `RazorSitemapIncludeUnannotatedPages=false` to require `[Sitemap]` metadata.
- Parameterized and catch-all routes are omitted unless `Url` supplies a static replacement.
- Components and routes conventionally used for errors, not-found pages, layouts, hosts, and imports are excluded unless explicitly annotated.
- `bin`, `obj`, `node_modules`, and `.git` content is ignored, as are directives inside Razor comments.
- Exact duplicate URLs are collapsed. Case-distinct URLs remain distinct and output is sorted ordinally.
- XML values are escaped, UTF-8 is written without a byte-order mark, and an unchanged sitemap is not rewritten.
- A completed temporary file replaces the prior sitemap, so cancellation or a write failure does not truncate valid output. Invalid URLs or metadata fail the build instead of producing a malformed sitemap.
