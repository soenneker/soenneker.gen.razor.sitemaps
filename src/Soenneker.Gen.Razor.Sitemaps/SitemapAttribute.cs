namespace Soenneker.Razor.Sitemaps;

[global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class SitemapAttribute : global::System.Attribute
{
    public bool Exclude { get; init; }

    public string? Url { get; init; }

    public string? ChangeFrequency { get; init; }

    public double Priority { get; init; } = double.NaN;

    public string? LastModified { get; init; }
}
