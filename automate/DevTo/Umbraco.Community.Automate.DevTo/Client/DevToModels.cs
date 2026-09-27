using System.Text.Json.Serialization;

namespace Umbraco.Community.Automate.DevTo.Client;

/// <summary>
/// Body of <c>POST /api/articles</c> and <c>PUT /api/articles/{id}</c>. Null members are
/// omitted, which on an update means "leave as is" rather than "clear".
/// </summary>
public sealed class DevToArticleRequest
{
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("body_markdown")]
    public string? BodyMarkdown { get; init; }

    [JsonPropertyName("published")]
    public bool? Published { get; init; }

    [JsonPropertyName("tags")]
    public IReadOnlyList<string>? Tags { get; init; }

    [JsonPropertyName("canonical_url")]
    public string? CanonicalUrl { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("main_image")]
    public string? MainImage { get; init; }

    [JsonPropertyName("series")]
    public string? Series { get; init; }
}

/// <summary>An article as returned by the Forem API.</summary>
public sealed class DevToArticle
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("slug")]
    public string? Slug { get; init; }

    [JsonPropertyName("published")]
    public bool Published { get; init; }

    [JsonPropertyName("canonical_url")]
    public string? CanonicalUrl { get; init; }
}

/// <summary>The authenticated user, from <c>GET /api/users/me</c>.</summary>
public sealed class DevToUser
{
    [JsonPropertyName("username")]
    public string? Username { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }
}
