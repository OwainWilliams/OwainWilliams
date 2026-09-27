using Umbraco.Automate.Core.Settings;
using Umbraco.Community.Automate.DevTo.Articles;

namespace Umbraco.Community.Automate.DevTo.Actions;

public sealed class PublishContentSettings
{
    [Field(Label = "Content Key",
        Description = "The content item to post. With a Content Published trigger, use ${ trigger.contentKey }.",
        SortOrder = 0,
        SupportsBindings = true)]
    public string ContentKey { get; set; } = "${ trigger.contentKey }";

    [Field(Label = "Body Properties",
        Description = "Alias(es) of the properties holding the article body, comma separated and in order (e.g. \"intro, blocks\"). Markdown, Rich Text, Block List and Block Grid are converted to Markdown.",
        SortOrder = 1)]
    public string BodyProperties { get; set; } = string.Empty;

    [Field(Label = "Title Property",
        Description = "Alias of the property holding the title. Leave blank to use the content name.",
        SortOrder = 2)]
    public string? TitleProperty { get; set; }

    [Field(Label = "Tags Property",
        Description = "Alias of a Tags, text (comma separated) or content picker property. The first 4 tags are used.",
        SortOrder = 3)]
    public string? TagsProperty { get; set; }

    [Field(Label = "Publish Mode",
        Description = "\"Save as draft\" creates new articles as drafts and never unpublishes an existing article.",
        SortOrder = 4,
        EditorUiAlias = "Umb.PropertyEditorUi.Dropdown",
        EditorConfig = DevToPublishMode.DropdownConfig)]
    public string PublishMode { get; set; } = DevToPublishMode.Draft;

    [Field(Label = "Additional Tags",
        Description = "Tags added to every article, comma separated (e.g. \"umbraco, dotnet\"). Added after the content's own tags.",
        SortOrder = 5,
        SupportsBindings = true,
        Group = "Optional")]
    public string? AdditionalTags { get; set; }

    [Field(Label = "Description Property",
        Description = "Alias of the property holding a summary for feeds and link previews.",
        SortOrder = 6,
        Group = "Optional")]
    public string? DescriptionProperty { get; set; }

    [Field(Label = "Cover Image Property",
        Description = "Alias of a media picker (or URL text) property for the cover image.",
        SortOrder = 7,
        Group = "Optional")]
    public string? CoverImageProperty { get; set; }

    [Field(Label = "Series",
        Description = "Optional series name. Articles with the same series are linked together on DEV.",
        SortOrder = 8,
        SupportsBindings = true,
        Group = "Optional")]
    public string? Series { get; set; }

    [Field(Label = "Culture",
        Description = "Culture to post for variant content (e.g. en-US). Leave blank for invariant content or the default culture.",
        SortOrder = 9,
        SupportsBindings = true,
        Group = "Advanced")]
    public string? Culture { get; set; }

    [Field(Label = "Site URL",
        Description = "Your site's public base URL, used for the canonical URL, links and images. Leave blank to use the URL Umbraco generates (requires a domain or UmbracoApplicationUrl). Can reference configuration, e.g. $Umbraco:Automate:Variables:SiteUrl",
        SortOrder = 10,
        SupportsBindings = true,
        Group = "Advanced")]
    public string? SiteUrl { get; set; }

    [Field(Label = "Canonical URL",
        Description = "Override the canonical URL. Leave blank to use the content's own URL.",
        SortOrder = 11,
        SupportsBindings = true,
        Group = "Advanced")]
    public string? CanonicalUrl { get; set; }

    [Field(Label = "Existing Article ID",
        Description = "Optional. Update this DEV article instead of looking it up by canonical URL — useful if the content's URL has changed.",
        SortOrder = 12,
        SupportsBindings = true,
        Group = "Advanced")]
    public string? ExistingArticleId { get; set; }
}
