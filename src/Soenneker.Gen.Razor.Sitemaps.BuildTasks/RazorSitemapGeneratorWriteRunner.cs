using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Soenneker.Utils.Directory.Abstract;
using Soenneker.Utils.File.Abstract;

namespace Soenneker.Gen.Razor.Sitemaps.BuildTasks;

///<inheritdoc cref="Abstract.IRazorSitemapGeneratorWriteRunner"/>
public sealed partial class RazorSitemapGeneratorWriteRunner : Abstract.IRazorSitemapGeneratorWriteRunner
{
    private const string _sitemapAttributeName = "Soenneker.Razor.Sitemap.SitemapAttribute";
    private const string _sitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";

    private readonly IFileUtil _fileUtil;
    private readonly IDirectoryUtil _directoryUtil;

    public RazorSitemapGeneratorWriteRunner(IFileUtil fileUtil, IDirectoryUtil directoryUtil)
    {
        _fileUtil = fileUtil;
        _directoryUtil = directoryUtil;
    }

    public async ValueTask<int> Run(string[] args, CancellationToken cancellationToken)
    {
        Dictionary<string, string> map = ParseArgs(args);
        if (!map.TryGetValue("--projectDir", out string? projectDir) || string.IsNullOrWhiteSpace(projectDir))
            return Fail("Missing required --projectDir");

        projectDir = Path.GetFullPath(projectDir.Trim().Trim('"'));

        string? targetPath = null;
        if (map.TryGetValue("--targetPath", out string? suppliedTargetPath) && !string.IsNullOrWhiteSpace(suppliedTargetPath))
            targetPath = Path.GetFullPath(suppliedTargetPath.Trim().Trim('"'));

        string baseUrl = GetOptional(map, "--baseUrl") ?? "";
        string outputPath = GetOptional(map, "--outputPath") ?? Path.Combine("wwwroot", "sitemap.xml");
        string? defaultChangeFrequency = GetOptional(map, "--defaultChangeFrequency");
        double? defaultPriority = TryParseNullableDouble(GetOptional(map, "--defaultPriority"));
        bool includeUnannotatedPages = TryParseBoolean(GetOptional(map, "--includeUnannotatedPages"), defaultValue: true);

        string outputFullPath = Path.IsPathRooted(outputPath) ? outputPath : Path.Combine(projectDir, outputPath);

        try
        {
            List<SitemapEntry> entries;
            if (!string.IsNullOrWhiteSpace(targetPath) && await _fileUtil.Exists(targetPath, cancellationToken) &&
                TryDiscoverFromCompiledAssembly(targetPath, includeUnannotatedPages, out List<SitemapEntry>? compiledEntries))
            {
                entries = compiledEntries;
                await AddSourceLastModified(entries, projectDir, cancellationToken);
            }
            else
            {
                entries = await DiscoverFromRazorFiles(projectDir, includeUnannotatedPages, cancellationToken);
            }

            entries = entries.Where(entry => !entry.Metadata.Exclude)
                             .GroupBy(entry => NormalizeUrl(entry.Metadata.Url ?? entry.Route), StringComparer.OrdinalIgnoreCase)
                             .Select(group =>
                             {
                                 SitemapEntry selected = group.OrderByDescending(entry => entry.Metadata.HasAnyValue).First();
                                 return selected with { Route = NormalizeUrl(selected.Metadata.Url ?? selected.Route) };
                             })
                             .OrderBy(entry => entry.Route, StringComparer.OrdinalIgnoreCase)
                             .ToList();

            bool written = await WriteSitemap(outputFullPath, baseUrl, entries, defaultChangeFrequency, defaultPriority, cancellationToken);
            Console.WriteLine(written
                ? $"Generated Razor sitemap with {entries.Count} URLs at {outputFullPath}"
                : $"Razor sitemap is unchanged with {entries.Count} URLs at {outputFullPath}");
        }
        catch (Exception e)
        {
            return Fail($"Failed to generate Razor sitemap: {e.Message}");
        }

        return 0;
    }

    private async ValueTask<List<SitemapEntry>> DiscoverFromRazorFiles(string projectDir, bool includeUnannotatedPages, CancellationToken cancellationToken)
    {
        var entries = new List<SitemapEntry>();
        List<string> files = await _directoryUtil.GetFilesByExtension(projectDir, ".razor", recursive: true, cancellationToken);

        foreach (string file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsExcludedPath(file))
                continue;

            string content = await _fileUtil.Read(file, log: false, cancellationToken);
            MatchCollection routeMatches = PageRegex().Matches(content);
            if (routeMatches.Count == 0)
                continue;

            SitemapMetadata metadata = ParseRazorSitemapAttribute(content);
            if (!includeUnannotatedPages && !metadata.HasAnyValue)
                continue;

            string? sourceLastModified = await GetSourceLastModified(file, cancellationToken);
            string componentName = GetComponentName(projectDir, file);
            foreach (Match routeMatch in routeMatches)
            {
                string route = routeMatch.Groups["route"].Value.Trim();
                if (ShouldIncludeRoute(route, componentName, metadata))
                    entries.Add(new SitemapEntry(route, metadata, componentName, sourceLastModified));
            }
        }

        return entries;
    }

    private async ValueTask<string?> GetSourceLastModified(string file, CancellationToken cancellationToken)
    {
        DateTimeOffset? lastModified = await _fileUtil.GetLastModified(file, cancellationToken);
        return lastModified?.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static SitemapMetadata ParseRazorSitemapAttribute(string content)
    {
        Match match = SitemapAttributeRegex().Match(content);
        if (!match.Success)
            return SitemapMetadata.Empty;

        return ParseAttributeText(match.Groups["attribute"].Value);
    }

    private static SitemapMetadata ParseAttributeText(string attributeText)
    {
        string arguments = attributeText;
        int open = arguments.IndexOf('(');
        int close = arguments.LastIndexOf(')');
        if (open >= 0 && close > open)
            arguments = arguments.Substring(open + 1, close - open - 1);

        var metadata = new SitemapMetadata();

        foreach (string assignment in SplitAttributeArguments(arguments))
        {
            int equalsIndex = assignment.IndexOf('=');
            if (equalsIndex <= 0)
                continue;

            string key = assignment.Substring(0, equalsIndex).Trim();
            string value = assignment.Substring(equalsIndex + 1).Trim();

            if (key.Equals("Exclude", StringComparison.OrdinalIgnoreCase))
                metadata.Exclude = TryParseBoolean(value, false);
            else if (key.Equals("Url", StringComparison.OrdinalIgnoreCase))
                metadata.Url = Unquote(value);
            else if (key.Equals("ChangeFrequency", StringComparison.OrdinalIgnoreCase))
                metadata.ChangeFrequency = Unquote(value);
            else if (key.Equals("Priority", StringComparison.OrdinalIgnoreCase))
                metadata.Priority = TryParseNullableDouble(value);
            else if (key.Equals("LastModified", StringComparison.OrdinalIgnoreCase))
                metadata.LastModified = Unquote(value);
        }

        return metadata;
    }

    private static IEnumerable<string> SplitAttributeArguments(string arguments)
    {
        var result = new List<string>();
        var builder = new StringBuilder();
        bool inString = false;

        foreach (char c in arguments)
        {
            if (c == '"')
                inString = !inString;

            if (c == ',' && !inString)
            {
                AddCurrent();
                continue;
            }

            builder.Append(c);
        }

        AddCurrent();
        return result;

        void AddCurrent()
        {
            string value = builder.ToString().Trim();
            if (value.Length > 0)
                result.Add(value);
            builder.Clear();
        }
    }

    private async ValueTask AddSourceLastModified(List<SitemapEntry> entries, string projectDir, CancellationToken cancellationToken)
    {
        var componentNames = entries.Where(entry => entry.Metadata.LastModified is null)
                                    .Select(entry => entry.ComponentName)
                                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (componentNames.Count == 0)
            return;

        List<string> files = await _directoryUtil.GetFilesByExtension(projectDir, ".razor", recursive: true, cancellationToken);
        var lastModifiedByComponent = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (string file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsExcludedPath(file))
                continue;

            string componentName = GetComponentName(projectDir, file);
            if (!componentNames.Contains(componentName))
                continue;

            string? lastModified = await GetSourceLastModified(file, cancellationToken);
            if (lastModified is not null)
                lastModifiedByComponent[componentName] = lastModified;
        }

        for (var i = 0; i < entries.Count; i++)
        {
            SitemapEntry entry = entries[i];
            if (lastModifiedByComponent.TryGetValue(entry.ComponentName, out string? lastModified))
                entries[i] = entry with { SourceLastModified = lastModified };
        }
    }

    private static bool TryDiscoverFromCompiledAssembly(string targetPath, bool includeUnannotatedPages, out List<SitemapEntry> entries)
    {
        entries = [];
        AssemblyLoadContext? context = null;

        try
        {
            string? targetDir = Path.GetDirectoryName(targetPath);
            var dependencyResolver = new AssemblyDependencyResolver(targetPath);
            context = new AssemblyLoadContext("SoennekerRazorSitemap", isCollectible: true);
            context.Resolving += (_, assemblyName) =>
            {
                string? resolvedPath = dependencyResolver.ResolveAssemblyToPath(assemblyName);
                if (resolvedPath is not null)
                    return context.LoadFromAssemblyPath(resolvedPath);

                if (!string.IsNullOrWhiteSpace(targetDir))
                {
                    string dependencyPath = Path.Combine(targetDir, assemblyName.Name + ".dll");
                    if (new FileInfo(dependencyPath).Exists)
                        return context.LoadFromAssemblyPath(dependencyPath);
                }

                try
                {
                    return AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);
                }
                catch
                {
                    return null;
                }
            };

            Assembly assembly = context.LoadFromAssemblyPath(targetPath);

            foreach (Type type in assembly.GetTypes())
            {
                IList<CustomAttributeData> attributes = type.GetCustomAttributesData();
                SitemapMetadata metadata = ReadSitemapMetadata(attributes);
                if (!includeUnannotatedPages && !metadata.HasAnyValue)
                    continue;

                string[] routes = attributes.Where(attribute => attribute.AttributeType.FullName == "Microsoft.AspNetCore.Components.RouteAttribute")
                                            .Select(attribute => attribute.ConstructorArguments.Count > 0 ? attribute.ConstructorArguments[0].Value as string : null)
                                            .Where(route => ShouldIncludeRoute(route, type.Name, metadata))
                                            .Cast<string>()
                                            .ToArray();

                if (routes.Length == 0)
                    continue;

                string componentName = type.Name;

                foreach (string route in routes)
                    entries.Add(new SitemapEntry(route, metadata, componentName, null));
            }

            return true;
        }
        catch
        {
            entries = [];
            return false;
        }
        finally
        {
            context?.Unload();
        }
    }

    private static SitemapMetadata ReadSitemapMetadata(IList<CustomAttributeData> attributes)
    {
        CustomAttributeData? attribute = attributes.FirstOrDefault(a => a.AttributeType.FullName == _sitemapAttributeName);
        if (attribute is null)
            return SitemapMetadata.Empty;

        var metadata = new SitemapMetadata();
        foreach (CustomAttributeNamedArgument argument in attribute.NamedArguments)
        {
            if (argument.MemberName == "Exclude")
                metadata.Exclude = argument.TypedValue.Value is true;
            else if (argument.MemberName == "Url")
                metadata.Url = argument.TypedValue.Value as string;
            else if (argument.MemberName == "ChangeFrequency")
                metadata.ChangeFrequency = argument.TypedValue.Value as string;
            else if (argument.MemberName == "Priority" && argument.TypedValue.Value is double value && !double.IsNaN(value))
                metadata.Priority = value;
            else if (argument.MemberName == "LastModified")
                metadata.LastModified = argument.TypedValue.Value as string;
        }

        return metadata;
    }

    private async ValueTask<bool> WriteSitemap(string outputPath, string baseUrl, IReadOnlyCollection<SitemapEntry> entries, string? defaultChangeFrequency,
        double? defaultPriority, CancellationToken cancellationToken)
    {
        string? outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(outputDir))
            await _directoryUtil.Create(outputDir, log: false, cancellationToken);

        var settings = new XmlWriterSettings
        {
            Async = false,
            Encoding = new UTF8Encoding(false),
            Indent = true
        };

        using var stream = new MemoryStream();
        using XmlWriter writer = XmlWriter.Create(stream, settings);
        writer.WriteStartDocument();
        writer.WriteStartElement("urlset", _sitemapNamespace);

        foreach (SitemapEntry entry in entries)
        {
            string location = BuildLocation(baseUrl, entry.Route);

            writer.WriteStartElement("url", _sitemapNamespace);
            writer.WriteElementString("loc", _sitemapNamespace, location);

            string? lastModified = entry.Metadata.LastModified ?? entry.SourceLastModified;
            if (!string.IsNullOrWhiteSpace(lastModified))
                writer.WriteElementString("lastmod", _sitemapNamespace, lastModified);

            string? changeFrequency = entry.Metadata.ChangeFrequency ?? defaultChangeFrequency;
            if (!string.IsNullOrWhiteSpace(changeFrequency))
                writer.WriteElementString("changefreq", _sitemapNamespace, changeFrequency);

            double? priority = entry.Metadata.Priority ?? defaultPriority;
            if (priority is not null)
                writer.WriteElementString("priority", _sitemapNamespace, priority.Value.ToString("0.0##", CultureInfo.InvariantCulture));

            writer.WriteEndElement();
        }

        writer.WriteEndElement();
        writer.WriteEndDocument();
        writer.Flush();

        byte[] content = stream.ToArray();
        if (await _fileUtil.Exists(outputPath, cancellationToken))
        {
            byte[] existingContent = await global::System.IO.File.ReadAllBytesAsync(outputPath, cancellationToken);
            if (existingContent.AsSpan().SequenceEqual(content))
                return false;
        }

        await _fileUtil.Write(outputPath, content, log: false, cancellationToken);
        return true;
    }

    private static string BuildLocation(string baseUrl, string route)
    {
        route = NormalizeUrl(route);

        if (!route.StartsWith("/", StringComparison.Ordinal) && Uri.TryCreate(route, UriKind.Absolute, out Uri? absolute))
            return absolute.ToString();

        if (string.IsNullOrWhiteSpace(baseUrl))
            return route;

        return baseUrl.TrimEnd('/') + "/" + route.TrimStart('/');
    }

    private static string NormalizeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "/";

        url = url.Trim();
        if (Uri.TryCreate(url, UriKind.Absolute, out _))
            return url;

        return url.StartsWith("/", StringComparison.Ordinal) ? url : "/" + url;
    }

    private static bool ShouldIncludeRoute(string? route, string componentName, SitemapMetadata metadata)
    {
        if (string.IsNullOrWhiteSpace(route))
            return false;

        if (metadata.Url is null && (route.Contains('{', StringComparison.Ordinal) || route.Contains('*', StringComparison.Ordinal)))
            return false;

        return metadata.HasAnyValue || (!IsDefaultExcludedComponent(componentName) && !IsDefaultExcludedRoute(route));
    }

    private static bool IsDefaultExcludedComponent(string componentName)
    {
        string name = componentName.TrimStart('_');
        return name.Equals("Error", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("NotFound", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("404", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("Host", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("Layout", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("Imports", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDefaultExcludedRoute(string route)
    {
        string normalized = NormalizeUrl(route).Trim('/').ToLowerInvariant();
        return normalized.Length == 0
            ? false
            : normalized == "error" || normalized == "not-found" || normalized == "notfound" || normalized == "404";
    }

    private static bool IsExcludedPath(string path)
    {
        string normalized = path.Replace('\\', '/');

        return IsPathSegmentExcluded(normalized, "obj") ||
               IsPathSegmentExcluded(normalized, "bin") ||
               IsPathSegmentExcluded(normalized, "node_modules") ||
               IsPathSegmentExcluded(normalized, ".git");
    }

    private static bool IsPathSegmentExcluded(string path, string segment)
    {
        return path.Equals(segment, StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith(segment + "/", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith("/" + segment, StringComparison.OrdinalIgnoreCase) ||
               path.Contains("/" + segment + "/", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetComponentName(string projectDir, string file)
    {
        string relative = Path.GetRelativePath(projectDir, file);
        string name = Path.GetFileNameWithoutExtension(relative);
        return name.Replace(".", "_", StringComparison.Ordinal);
    }

    private static string? GetOptional(IReadOnlyDictionary<string, string> map, string key)
    {
        return map.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value) ? value.Trim().Trim('"') : null;
    }

    private static string? Unquote(string value)
    {
        value = value.Trim();
        if (value.StartsWith("@\"", StringComparison.Ordinal))
            value = value.Substring(1);
        if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
            value = value.Substring(1, value.Length - 2);
        return value.Replace("\\\"", "\"", StringComparison.Ordinal);
    }

    private static bool TryParseBoolean(string? value, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        value = value.Trim().Trim('"');
        return bool.TryParse(value, out bool result) ? result : defaultValue;
    }

    private static double? TryParseNullableDouble(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        value = value.Trim().Trim('"');
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result) ? result : null;
    }

    private static Dictionary<string, string> ParseArgs(string[] args)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("--", StringComparison.Ordinal) && i + 1 < args.Length)
            {
                map[args[i]] = args[i + 1];
                i++;
            }
        }
        return map;
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }

    [GeneratedRegex(@"^\s*@page\s+""(?<route>[^""]+)""", RegexOptions.Multiline)]
    private static partial Regex PageRegex();

    [GeneratedRegex(@"^\s*@attribute\s+\[(?<attribute>[^\]]*Sitemap(?:Attribute)?[^\]]*)\]", RegexOptions.Multiline)]
    private static partial Regex SitemapAttributeRegex();
}
