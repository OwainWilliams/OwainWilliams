namespace Umbraco.Community.Automate.DevTo.Articles;

/// <summary>
/// Values for the "Publish Mode" dropdown shared by both article actions.
/// </summary>
public static class DevToPublishMode
{
    /// <summary>New articles are created as drafts; existing articles keep their published state.</summary>
    public const string Draft = "draft";

    /// <summary>Articles are published, including drafts created by an earlier run.</summary>
    public const string Publish = "publish";

    internal const string DropdownConfig = """
        [{ "alias": "items", "value": [
            { "name": "Save as draft", "value": "draft" },
            { "name": "Publish", "value": "publish" }
        ] }]
        """;

    public static bool IsPublish(string? mode) => string.Equals(mode?.Trim(), Publish, StringComparison.OrdinalIgnoreCase);
}
