using System;

namespace Soenneker.Razor.Sitemap;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class SitemapAttribute : Attribute
{
    public bool Exclude { get; init; }

    public string? Url { get; init; }

    public string? ChangeFrequency { get; init; }

    public double Priority { get; init; } = double.NaN;

    public string? LastModified { get; init; }
}
