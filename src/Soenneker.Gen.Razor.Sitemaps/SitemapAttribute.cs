namespace Soenneker.Razor.Sitemap;

/// <summary>
/// Controls how a Razor component's routes are represented in the generated sitemap.
/// </summary>
[global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class SitemapAttribute : global::System.Attribute
{
    /// <summary>
    /// Gets or sets whether all routes declared by the component are excluded.
    /// </summary>
    public bool Exclude { get; init; }

    /// <summary>
    /// Gets or sets a static URL override, commonly used to represent an otherwise dynamic route.
    /// </summary>
    public string? Url { get; init; }

    /// <summary>
    /// Gets or sets the sitemap change frequency: always, hourly, daily, weekly, monthly, yearly, or never.
    /// </summary>
    public string? ChangeFrequency { get; init; }

    /// <summary>
    /// Gets or sets the sitemap priority from 0 through 1.
    /// </summary>
    public double Priority { get; init; } = double.NaN;

    /// <summary>
    /// Gets or sets the W3C date or date-time written to <c>lastmod</c>.
    /// </summary>
    public string? LastModified { get; init; }
}
