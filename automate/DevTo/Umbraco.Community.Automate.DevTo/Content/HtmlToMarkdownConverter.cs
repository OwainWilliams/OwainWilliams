using System.Net;
using System.Text.RegularExpressions;
using ReverseMarkdown;

namespace Umbraco.Community.Automate.DevTo.Content;

/// <summary>
/// Converts Rich Text HTML to the GitHub-flavoured Markdown DEV expects.
/// </summary>
internal static partial class HtmlToMarkdownConverter
{
    public static string Convert(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        html = IframePattern().Replace(html, m => $"<p>{{% embed {ToEmbedUrl(m.Groups["src"].Value)} %}}</p>");

        // A new converter per call: ReverseMarkdown makes no thread-safety promises, and
        // construction is cheap next to the HTTP calls around it.
        var converter = new Converter(new Config
        {
            GithubFlavored = true,
            // Keep the text of tags Markdown has no equivalent for (<span>, <figure>, any
            // leftover <umb-rte-block>) rather than dropping it or leaking raw HTML.
            UnknownTags = Config.UnknownTagsOption.Bypass,
            RemoveComments = true,
            SmartHrefHandling = true,
        });

        return converter.Convert(html).Trim();
    }

    /// <summary>
    /// DEV's <c>{% embed %}</c> tag wants the page URL of a video, not the player URL an
    /// iframe points at, so map the common providers back.
    /// </summary>
    internal static string ToEmbedUrl(string src)
    {
        src = WebUtility.HtmlDecode(src.Trim());
        if (src.StartsWith("//", StringComparison.Ordinal))
            src = "https:" + src;

        if (YouTubePattern().Match(src) is { Success: true } youTube)
            return $"https://www.youtube.com/watch?v={youTube.Groups["id"].Value}";

        if (VimeoPattern().Match(src) is { Success: true } vimeo)
            return $"https://vimeo.com/{vimeo.Groups["id"].Value}";

        return src;
    }

    [GeneratedRegex("""<iframe\b[^>]*?\bsrc=["'](?<src>[^"']+)["'][^>]*>.*?</iframe>""", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex IframePattern();

    [GeneratedRegex("""youtube(?:-nocookie)?\.com/embed/(?<id>[\w-]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex YouTubePattern();

    [GeneratedRegex("""player\.vimeo\.com/video/(?<id>\d+)""", RegexOptions.IgnoreCase)]
    private static partial Regex VimeoPattern();
}
