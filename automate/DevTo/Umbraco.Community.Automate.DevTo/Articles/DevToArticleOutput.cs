namespace Umbraco.Community.Automate.DevTo.Articles;

/// <summary>
/// Output of the DEV article actions, available to later steps as
/// <c>${ steps.&lt;alias&gt;.url }</c> and so on.
/// </summary>
public sealed class DevToArticleOutput
{
    /// <summary>Gets the DEV article ID.</summary>
    public long ArticleId { get; init; }

    /// <summary>Gets the public URL of the article on DEV.</summary>
    public string? Url { get; init; }

    /// <summary>Gets the article's slug.</summary>
    public string? Slug { get; init; }

    /// <summary>Gets the article title.</summary>
    public string? Title { get; init; }

    /// <summary>Gets a value indicating whether the article is published (false for a draft).</summary>
    public bool Published { get; init; }

    /// <summary>Gets the canonical URL sent to DEV.</summary>
    public string? CanonicalUrl { get; init; }
}
