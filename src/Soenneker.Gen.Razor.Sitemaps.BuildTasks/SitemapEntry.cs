namespace Soenneker.Gen.Razor.Sitemaps.BuildTasks;

internal sealed record SitemapEntry(string Route, SitemapMetadata Metadata, string ComponentName, string? SourceLastModified);
