using System.Text;

namespace Umbraco.Community.Automate.DevTo.Articles;

/// <summary>
/// DEV tags are lowercase alphanumeric, and an article can have at most four.
/// </summary>
public static class DevToTags
{
    public const int MaxTags = 4;

    private static readonly char[] Separators = [',', ';', '#', '\n', '\r'];

    /// <summary>
    /// Splits free text such as <c>"umbraco, dotnet"</c> or <c>"#umbraco #dotnet"</c> into tags.
    /// Whitespace inside a tag is not a separator: "Umbraco CMS" becomes <c>umbracocms</c>.
    /// </summary>
    public static IEnumerable<string> Split(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>
    /// Normalises tags to DEV's rules, removes duplicates, and keeps the first <see cref="MaxTags"/>.
    /// </summary>
    public static IReadOnlyList<string> Normalise(IEnumerable<string?> tags)
        => tags
            .Select(NormaliseTag)
            .Where(t => t.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Take(MaxTags)
            .ToArray();

    private static string NormaliseTag(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return string.Empty;

        var sb = new StringBuilder(tag.Length);
        foreach (var c in tag.Normalize(NormalizationForm.FormD))
        {
            var lower = char.ToLowerInvariant(c);
            if (lower is >= 'a' and <= 'z' or >= '0' and <= '9')
                sb.Append(lower);
        }

        return sb.ToString();
    }
}
