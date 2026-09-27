using Microsoft.Extensions.Logging;
using Umbraco.Automate.Core.Actions;
using Umbraco.Community.Automate.DevTo.Articles;
using Umbraco.Community.Automate.DevTo.Client;
using Umbraco.Community.Automate.DevTo.ConnectionTypes;

namespace Umbraco.Community.Automate.DevTo.Actions;

/// <summary>
/// Creates a DEV article from Markdown, or updates the one previously created for the same
/// canonical URL. Produces "created" or "updated" outcomes.
/// </summary>
[Action("devto.createOrUpdateArticle", "Create or Update DEV Article",
    ConnectionTypeAlias = DevToConnectionType.ConnectionTypeAlias,
    Description = "Creates a DEV article from Markdown, or updates the existing one with the same canonical URL.",
    Icon = "icon-automate-devto",
    Group = "Social Networks")]
public sealed class CreateOrUpdateArticleAction : ActionBase<CreateOrUpdateArticleSettings, DevToArticleOutput>
{
    private readonly DevToArticlePublisher _publisher;
    private readonly ILogger<CreateOrUpdateArticleAction> _logger;

    public CreateOrUpdateArticleAction(
        ActionInfrastructure infrastructure,
        DevToArticlePublisher publisher,
        ILogger<CreateOrUpdateArticleAction> logger)
        : base(infrastructure)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        var (connection, connectionFailure) = DevToActionHelpers.GetConnection(context);
        if (connectionFailure is not null)
            return connectionFailure;

        var settings = context.GetSettings<CreateOrUpdateArticleSettings>();

        if (string.IsNullOrWhiteSpace(settings.Title))
            return ActionResult.Failed(new ArgumentException("A title is required."), StepRunErrorCategory.Validation);

        if (string.IsNullOrWhiteSpace(settings.BodyMarkdown))
            return ActionResult.Failed(new ArgumentException("A body is required."), StepRunErrorCategory.Validation);

        if (!string.IsNullOrWhiteSpace(settings.CanonicalUrl) && CanonicalUrl.Normalise(settings.CanonicalUrl) is null)
            return ActionResult.Failed(
                new ArgumentException($"'{settings.CanonicalUrl}' is not an absolute http(s) URL."), StepRunErrorCategory.Validation);

        var (articleId, idFailure) = DevToActionHelpers.ParseArticleId(settings.ExistingArticleId);
        if (idFailure is not null)
            return idFailure;

        var draft = new DevToArticleDraft
        {
            Title = settings.Title,
            BodyMarkdown = settings.BodyMarkdown,
            Tags = DevToTags.Normalise(DevToTags.Split(settings.Tags)),
            CanonicalUrl = settings.CanonicalUrl,
            Description = settings.Description,
            CoverImageUrl = settings.CoverImageUrl,
            Series = settings.Series,
            PublishMode = settings.PublishMode,
            ExistingArticleId = articleId,
        };

        try
        {
            var (outcome, output) = await _publisher.PublishAsync(connection!, draft, cancellationToken);

            _logger.LogInformation(
                "Automation {AutomationId} / Run {RunId}: DEV article {ArticleId} {Outcome}.",
                context.AutomationId, context.RunId, output.ArticleId, outcome);

            return SuccessWithOutcome(outcome, output);
        }
        catch (DevToApiException ex)
        {
            return ActionResult.Failed(ex, ex.Category);
        }
    }
}
