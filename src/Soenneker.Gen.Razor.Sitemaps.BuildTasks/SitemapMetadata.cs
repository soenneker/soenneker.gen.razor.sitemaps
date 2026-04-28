namespace Soenneker.Gen.Razor.Sitemaps.BuildTasks;

internal sealed class SitemapMetadata
{
    public static SitemapMetadata Empty { get; } = new();

    public bool Exclude { get; set; }

    public string? Url { get; set; }

    public string? ChangeFrequency { get; set; }

    public double? Priority { get; set; }

    public string? LastModified { get; set; }

    public bool HasAnyValue => Exclude || Url is not null || ChangeFrequency is not null || Priority is not null || LastModified is not null;
}
