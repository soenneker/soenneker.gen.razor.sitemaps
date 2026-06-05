namespace Soenneker.Razor.Sitemap;

/// <summary>
/// Represents the sitemap attribute.
/// </summary>
[global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class SitemapAttribute : global::System.Attribute
{
    /// <summary>
    /// Gets or sets a value indicating whether exclude.
    /// </summary>
    public bool Exclude { get; init; }

    /// <summary>
    /// Gets or sets url.
    /// </summary>
    public string? Url { get; init; }

    /// <summary>
    /// Gets or sets change frequency.
    /// </summary>
    public string? ChangeFrequency { get; init; }

    /// <summary>
    /// Gets or sets priority.
    /// </summary>
    public double Priority { get; init; } = double.NaN;

    /// <summary>
    /// Gets or sets last modified.
    /// </summary>
    public string? LastModified { get; init; }
}
