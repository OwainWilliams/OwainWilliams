using Umbraco.Automate.Core.Settings;
using Umbraco.Community.Automate.DevTo.Articles;

namespace Umbraco.Community.Automate.DevTo.Actions;

public sealed class CreateOrUpdateArticleSettings
{
    [Field(Label = "Title",
        Description = "The article title. Supports bindings, e.g. ${ trigger.contentName }.",
        SortOrder = 0,
        SupportsBindings = true)]
    public string Title { get; set; } = string.Empty;

    [Field(Label = "Body (Markdown)",
        Description = "The article body as Markdown. To post Umbraco content (Rich Text, Block List, Block Grid) use the \"Publish Content to DEV\" action instead, which converts it for you.",
        SortOrder = 1,
        SupportsBindings = true,
        EditorUiAlias = "Umb.PropertyEditorUi.TextArea",
        EditorConfig = """[{ "alias": "rows", "value": 10 }]""")]
    public string BodyMarkdown { get; set; } = string.Empty;

    [Field(Label = "Canonical URL",
        Description = "The URL of the original post. Tells search engines your site is the source, and is used to find and update the article on later runs.",
        SortOrder = 2,
        SupportsBindings = true)]
    public string? CanonicalUrl { get; set; }

    [Field(Label = "Tags",
        Description = "Up to 4 tags, comma separated. Tags are lowercased and stripped to letters and numbers, as DEV requires.",
        SortOrder = 3,
        SupportsBindings = true)]
    public string? Tags { get; set; }

    [Field(Label = "Publish Mode",
        Description = "\"Save as draft\" creates new articles as drafts and never unpublishes an existing article.",
        SortOrder = 4,
        EditorUiAlias = "Umb.PropertyEditorUi.Dropdown",
        EditorConfig = DevToPublishMode.DropdownConfig)]
    public string PublishMode { get; set; } = DevToPublishMode.Draft;

    [Field(Label = "Description",
        Description = "Optional summary shown in feeds and link previews.",
        SortOrder = 5,
        SupportsBindings = true,
        Group = "Optional")]
    public string? Description { get; set; }

    [Field(Label = "Cover Image URL",
        Description = "Optional absolute URL of the cover image.",
        SortOrder = 6,
        SupportsBindings = true,
        Group = "Optional")]
    public string? CoverImageUrl { get; set; }

    [Field(Label = "Series",
        Description = "Optional series name. Articles with the same series are linked together on DEV.",
        SortOrder = 7,
        SupportsBindings = true,
        Group = "Optional")]
    public string? Series { get; set; }

    [Field(Label = "Existing Article ID",
        Description = "Optional. Update this DEV article instead of looking it up by canonical URL — useful if the original post's URL has changed.",
        SortOrder = 8,
        SupportsBindings = true,
        Group = "Advanced")]
    public string? ExistingArticleId { get; set; }
}
