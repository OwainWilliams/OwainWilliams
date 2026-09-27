using Microsoft.Extensions.Logging;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Core.Security;
using Umbraco.Cms.Core.Actions;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;
using Umbraco.Community.Automate.DevTo.Articles;
using Umbraco.Community.Automate.DevTo.Client;
using Umbraco.Community.Automate.DevTo.ConnectionTypes;
using Umbraco.Community.Automate.DevTo.Content;
using Umbraco.Extensions;
using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.Community.Automate.DevTo.Actions;

/// <summary>
/// Posts a published content item to DEV: converts its body to Markdown, points the
/// canonical URL back at the site, and creates or updates the article.
/// Produces "created", "updated" or "notFound" outcomes.
/// </summary>
[Action("devto.publishContent", "Publish Content to DEV",
    ConnectionTypeAlias = DevToConnectionType.ConnectionTypeAlias,
    Description = "Cross-posts a content item to DEV, converting Markdown, Rich Text, Block List and Block Grid content to Markdown.",
    Icon = "icon-automate-devto",
    Group = "Social Networks",
    RequiredSections = [UmbracoConstants.Applications.Content],
    RequiredPermissions = [ActionBrowse.ActionLetter])]
public sealed class PublishContentAction : ActionBase<PublishContentSettings, DevToArticleOutput>
{
    /// <summary>
    /// Outcome emitted when the item isn't in the published cache — e.g. it was unpublished
    /// or deleted between the trigger firing and this step running.
    /// </summary>
    public const string OutcomeNotFound = "notFound";

    private readonly IPublishedContentCache _publishedContentCache;
    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly IPublishedUrlProvider _urlProvider;
    private readonly IAutomationActionAuthorizer _authorizer;
    private readonly ContentMarkdownConverter _converter;
    private readonly DevToArticlePublisher _publisher;
    private readonly ILogger<PublishContentAction> _logger;

    public PublishContentAction(
        ActionInfrastructure infrastructure,
        IPublishedContentCache publishedContentCache,
        IUmbracoContextFactory umbracoContextFactory,
        IPublishedUrlProvider urlProvider,
        IAutomationActionAuthorizer authorizer,
        ContentMarkdownConverter converter,
        DevToArticlePublisher publisher,
        ILogger<PublishContentAction> logger)
        : base(infrastructure)
    {
        _publishedContentCache = publishedContentCache;
        _umbracoContextFactory = umbracoContextFactory;
        _urlProvider = urlProvider;
        _authorizer = authorizer;
        _converter = converter;
        _publisher = publisher;
        _logger = logger;
    }

    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        var (connection, connectionFailure) = DevToActionHelpers.GetConnection(context);
        if (connectionFailure is not null)
            return connectionFailure;

        var settings = context.GetSettings<PublishContentSettings>();

        if (string.IsNullOrWhiteSpace(settings.ContentKey) || !Guid.TryParse(settings.ContentKey, out var contentKey))
            return Invalid($"Invalid or missing content key: '{settings.ContentKey}'.");

        var bodyAliases = SplitAliases(settings.BodyProperties);
        if (bodyAliases.Length == 0)
            return Invalid("At least one body property alias is required.");

        var (articleId, idFailure) = DevToActionHelpers.ParseArticleId(settings.ExistingArticleId);
        if (idFailure is not null)
            return idFailure;

        // Section access is checked by middleware; this applies the run identity's start
        // nodes and granular permissions to this specific item.
        if (await _authorizer.AuthorizeContentOrFailAsync(contentKey, RequiredPermissions, cancellationToken) is { } denied)
            return denied;

        // Required when running from the outbox dispatcher, which has no HTTP request scope.
        using var contextReference = _umbracoContextFactory.EnsureUmbracoContext();

        var content = await _publishedContentCache.GetByIdAsync(contentKey);
        if (content is null)
        {
            _logger.LogDebug(
                "Automation {AutomationId} / Run {RunId}: Content {ContentKey} not found in published cache.",
                context.AutomationId, context.RunId, contentKey);

            return SuccessWithOutcome(OutcomeNotFound, new DevToArticleOutput());
        }

        var missing = new[] { settings.TitleProperty, settings.TagsProperty, settings.DescriptionProperty, settings.CoverImageProperty }
            .Where(alias => !string.IsNullOrWhiteSpace(alias))
            .Select(alias => alias!.Trim())
            .Concat(bodyAliases)
            .Where(alias => content.GetProperty(alias) is null)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (missing.Length > 0)
            return Invalid($"'{content.ContentType.Alias}' has no propert{(missing.Length == 1 ? "y" : "ies")} named {string.Join(", ", missing.Select(m => $"'{m}'"))}.");

        var culture = ResolveCulture(settings.Culture, content);

        var (siteBaseUri, canonicalUrl, urlError) = ResolveUrls(content, culture, settings);
        if (urlError is not null)
            return ActionResult.Failed(new InvalidOperationException(urlError), StepRunErrorCategory.ConfigurationError);

        var title = string.IsNullOrWhiteSpace(settings.TitleProperty)
            ? null
            : _converter.ReadPlainText(content, settings.TitleProperty.Trim(), culture);
        title ??= PublishedContentCompat.GetName(content, culture);

        if (string.IsNullOrWhiteSpace(title))
            return Invalid("The article title is empty.");

        var body = _converter.Convert(content, bodyAliases, culture, siteBaseUri!);
        if (string.IsNullOrWhiteSpace(body))
            return Invalid($"The body propert{(bodyAliases.Length == 1 ? "y" : "ies")} {string.Join(", ", bodyAliases)} produced no content.");

        var contentTags = string.IsNullOrWhiteSpace(settings.TagsProperty)
            ? []
            : _converter.ReadTags(content, settings.TagsProperty.Trim(), culture);

        var draft = new DevToArticleDraft
        {
            Title = title,
            BodyMarkdown = body,
            Tags = DevToTags.Normalise(contentTags.Concat(DevToTags.Split(settings.AdditionalTags))),
            CanonicalUrl = canonicalUrl,
            Description = string.IsNullOrWhiteSpace(settings.DescriptionProperty)
                ? null
                : _converter.ReadPlainText(content, settings.DescriptionProperty.Trim(), culture),
            CoverImageUrl = string.IsNullOrWhiteSpace(settings.CoverImageProperty)
                ? null
                : _converter.ReadImageUrl(content, settings.CoverImageProperty.Trim(), culture, siteBaseUri!),
            Series = settings.Series,
            PublishMode = settings.PublishMode,
            ExistingArticleId = articleId,
        };

        try
        {
            var (outcome, output) = await _publisher.PublishAsync(connection!, draft, cancellationToken);

            _logger.LogInformation(
                "Automation {AutomationId} / Run {RunId}: Content {ContentKey} posted to DEV as article {ArticleId} ({Outcome}).",
                context.AutomationId, context.RunId, contentKey, output.ArticleId, outcome);

            return SuccessWithOutcome(outcome, output);
        }
        catch (DevToApiException ex)
        {
            return ActionResult.Failed(ex, ex.Category);
        }
    }

    /// <summary>
    /// Works out the absolute base URL (for links and images) and the canonical URL. An
    /// explicit Site URL wins; otherwise Umbraco's absolute URL for the content is used.
    /// </summary>
    internal (Uri? SiteBaseUri, string? CanonicalUrl, string? Error) ResolveUrls(
        IPublishedContent content, string? culture, PublishContentSettings settings)
    {
        Uri? siteBaseUri;
        string? contentUrl;

        if (!string.IsNullOrWhiteSpace(settings.SiteUrl))
        {
            if (!TryGetHttpUri(settings.SiteUrl.Trim().TrimEnd('/') + "/", out siteBaseUri))
                return (null, null, $"Site URL '{settings.SiteUrl}' is not an absolute http(s) URL.");

            var relative = content.Url(_urlProvider, culture, UrlMode.Relative);
            contentUrl = IsRoutable(relative) ? UrlResolver.Resolve(relative, siteBaseUri!) : null;
        }
        else
        {
            var absolute = content.Url(_urlProvider, culture, UrlMode.Absolute);
            if (!TryGetHttpUri(absolute, out var absoluteUri))
                return (null, null,
                    "Umbraco could not produce an absolute URL for this content. Assign a domain, set " +
                    "Umbraco:CMS:WebRouting:UmbracoApplicationUrl, or fill in the step's Site URL setting.");

            siteBaseUri = new Uri(absoluteUri!.GetLeftPart(UriPartial.Authority) + "/");
            contentUrl = absoluteUri.ToString();
        }

        var canonicalUrl = string.IsNullOrWhiteSpace(settings.CanonicalUrl) ? contentUrl : settings.CanonicalUrl.Trim();

        if (canonicalUrl is null)
            return (null, null, "This content has no public URL to use as the canonical URL. Set the step's Canonical URL setting.");

        if (CanonicalUrl.Normalise(canonicalUrl) is null)
            return (null, null, $"Canonical URL '{canonicalUrl}' is not an absolute http(s) URL.");

        return (siteBaseUri, canonicalUrl, null);
    }

    private static bool IsRoutable(string? url) => !string.IsNullOrWhiteSpace(url) && url != "#";

    private static bool TryGetHttpUri(string? value, out Uri? uri)
    {
        uri = null;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var parsed) ||
            (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
            return false;

        uri = parsed;
        return true;
    }

    private static string? ResolveCulture(string? requested, IPublishedContent content)
    {
        if (!content.ContentType.VariesByCulture())
            return null;

        return !string.IsNullOrWhiteSpace(requested) ? requested.Trim() : PublishedContentCompat.GetCultures(content).Keys.FirstOrDefault();
    }

    private static string[] SplitAliases(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split([',', ';', ' ', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static ActionResult Invalid(string message)
        => ActionResult.Failed(new ArgumentException(message), StepRunErrorCategory.Validation);
}
