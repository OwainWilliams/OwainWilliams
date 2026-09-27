namespace Umbraco.Community.Automate.DevTo.Articles;

/// <summary>
/// Everything needed to create or update one DEV article, independent of where it came from.
/// </summary>
public sealed class DevToArticleDraft
{
    public required string Title { get; init; }

    public required string BodyMarkdown { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>
    /// The URL of the original post. Also the key used to find an article created by an
    /// earlier run, so the same post is updated rather than duplicated.
    /// </summary>
    public string? CanonicalUrl { get; init; }

    public string? Description { get; init; }

    public string? CoverImageUrl { get; init; }

    public string? Series { get; init; }

    public string? PublishMode { get; init; }

    /// <summary>
    /// Updates this article directly, skipping the canonical URL lookup. Useful when the
    /// original post's URL has changed.
    /// </summary>
    public long? ExistingArticleId { get; init; }
}
