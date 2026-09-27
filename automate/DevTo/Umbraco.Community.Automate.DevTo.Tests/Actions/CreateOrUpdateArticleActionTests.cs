using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Testing;
using Umbraco.Community.Automate.DevTo.Actions;
using Umbraco.Community.Automate.DevTo.Articles;
using Umbraco.Community.Automate.DevTo.Client;
using Umbraco.Community.Automate.DevTo.Settings;
using Umbraco.Community.Automate.DevTo.Tests.Helpers;
using Xunit;

namespace Umbraco.Community.Automate.DevTo.Tests.Actions;

public class CreateOrUpdateArticleActionTests
{
    private const string Canonical = "https://owain.codes/blog/my-post/";

    private const string CreatedArticle = """
        {"id":101,"title":"My Post","url":"https://dev.to/owain/my-post-1a2b","slug":"my-post-1a2b","published":false,"canonical_url":"https://owain.codes/blog/my-post/"}
        """;

    private static CreateOrUpdateArticleSettings ValidSettings(Action<CreateOrUpdateArticleSettings>? configure = null)
    {
        var settings = new CreateOrUpdateArticleSettings
        {
            Title = "My Post",
            BodyMarkdown = "# Hello\n\nWorld",
            CanonicalUrl = Canonical,
            Tags = "Umbraco, .NET, csharp, webdev, blog",
        };
        configure?.Invoke(settings);
        return settings;
    }

    private static Task<ActionResult> Execute(FakeDevToApi api, CreateOrUpdateArticleSettings settings, DevToConnectionSettings? connection = null)
        => ActionTestHarness.For<CreateOrUpdateArticleAction>()
            .WithService(new DevToArticlePublisher(new DevToClient(api.CreateFactory())))
            .WithService<ILogger<CreateOrUpdateArticleAction>>(NullLogger<CreateOrUpdateArticleAction>.Instance)
            .WithSettings(settings)
            .WithConnection("devto", connection ?? new DevToConnectionSettings { ApiKey = "secret-key" })
            .ExecuteAsync();

    [Fact]
    public async Task Creates_a_draft_when_no_article_has_the_canonical_url()
    {
        var api = new FakeDevToApi()
            .RespondWithArticles(new { id = 7, canonical_url = "https://owain.codes/blog/something-else" })
            .Respond(CreatedArticle, HttpStatusCode.Created);

        var result = await Execute(api, ValidSettings());

        result.Status.ShouldBe(ActionResultStatus.Success);
        result.Outcome.ShouldBe(DevToArticlePublisher.OutcomeCreated);

        api.Requests.Count.ShouldBe(2);
        api.Requests[0].Method.ShouldBe(HttpMethod.Get);
        api.Requests[0].Uri.ToString().ShouldBe("https://dev.to/api/articles/me/all?page=1&per_page=1000");

        var create = api.Requests[1];
        create.Method.ShouldBe(HttpMethod.Post);
        create.Uri.ToString().ShouldBe("https://dev.to/api/articles");
        create.Headers["api-key"].ShouldBe("secret-key");
        create.Headers["Accept"].ShouldBe("application/vnd.forem.api-v1+json");
        create.Headers["User-Agent"].ShouldStartWith("Umbraco.Community.Automate.DevTo/");

        var article = create.Article;
        article.GetProperty("title").GetString().ShouldBe("My Post");
        article.GetProperty("body_markdown").GetString().ShouldBe("# Hello\n\nWorld");
        article.GetProperty("published").GetBoolean().ShouldBeFalse();
        article.GetProperty("canonical_url").GetString().ShouldBe(Canonical);
        article.GetProperty("tags").EnumerateArray().Select(t => t.GetString()).ShouldBe(["umbraco", "net", "csharp", "webdev"]);
        article.TryGetProperty("series", out _).ShouldBeFalse("blank optional fields are omitted");

        var output = (DevToArticleOutput)result.OutputData!;
        output.ArticleId.ShouldBe(101);
        output.Url.ShouldBe("https://dev.to/owain/my-post-1a2b");
        output.Published.ShouldBeFalse();
    }

    [Fact]
    public async Task Updates_the_article_with_a_matching_canonical_url_without_unpublishing_it()
    {
        var api = new FakeDevToApi()
            .RespondWithArticles(new { id = 55, canonical_url = "http://OWAIN.codes/blog/my-post" })
            .Respond("""{"id":55,"url":"https://dev.to/owain/my-post","published":true}""");

        var result = await Execute(api, ValidSettings());

        result.Outcome.ShouldBe(DevToArticlePublisher.OutcomeUpdated);
        api.Requests[1].Method.ShouldBe(HttpMethod.Put);
        api.Requests[1].Uri.ToString().ShouldBe("https://dev.to/api/articles/55");
        api.Requests[1].Article.TryGetProperty("published", out _).ShouldBeFalse();

        var output = (DevToArticleOutput)result.OutputData!;
        output.ArticleId.ShouldBe(55);
        output.Published.ShouldBeTrue();
    }

    [Fact]
    public async Task Publish_mode_publishes_on_create_and_update()
    {
        var api = new FakeDevToApi().RespondWithArticles().Respond(CreatedArticle);

        await Execute(api, ValidSettings(s => s.PublishMode = DevToPublishMode.Publish));

        api.Requests[1].Article.GetProperty("published").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task Existing_article_id_skips_the_canonical_url_lookup()
    {
        var api = new FakeDevToApi().Respond("""{"id":999,"published":false}""");

        var result = await Execute(api, ValidSettings(s => s.ExistingArticleId = "999"));

        result.Outcome.ShouldBe(DevToArticlePublisher.OutcomeUpdated);
        api.Requests.ShouldHaveSingleItem().Uri.ToString().ShouldBe("https://dev.to/api/articles/999");
    }

    [Fact]
    public async Task Pages_through_articles_until_a_short_page()
    {
        var fullPage = Enumerable.Range(1, DevToClient.PageSize)
            .Select(i => (object)new { id = i, canonical_url = $"https://owain.codes/blog/{i}" })
            .ToArray();

        var api = new FakeDevToApi()
            .RespondWithArticles(fullPage)
            .RespondWithArticles(new { id = 5000, canonical_url = Canonical })
            .Respond("""{"id":5000,"published":true}""");

        var result = await Execute(api, ValidSettings());

        result.Outcome.ShouldBe(DevToArticlePublisher.OutcomeUpdated);
        api.Requests[1].Uri.Query.ShouldContain("page=2");
        api.Requests[2].Uri.ToString().ShouldEndWith("/api/articles/5000");
    }

    [Fact]
    public async Task Without_a_canonical_url_it_always_creates()
    {
        var api = new FakeDevToApi().Respond(CreatedArticle);

        var result = await Execute(api, ValidSettings(s => s.CanonicalUrl = null));

        result.Outcome.ShouldBe(DevToArticlePublisher.OutcomeCreated);
        api.Requests.ShouldHaveSingleItem().Method.ShouldBe(HttpMethod.Post);
    }

    [Fact]
    public async Task Uses_the_configured_forem_instance()
    {
        var api = new FakeDevToApi().RespondWithArticles().Respond(CreatedArticle);

        await Execute(api, ValidSettings(), new DevToConnectionSettings { ApiKey = "k", InstanceUrl = "https://community.example.org/" });

        api.Requests[0].Uri.Host.ShouldBe("community.example.org");
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, StepRunErrorCategory.RateLimiting)]
    [InlineData(HttpStatusCode.Unauthorized, StepRunErrorCategory.Authentication)]
    [InlineData(HttpStatusCode.UnprocessableEntity, StepRunErrorCategory.Validation)]
    [InlineData(HttpStatusCode.BadGateway, StepRunErrorCategory.ServiceUnavailable)]
    public async Task Api_errors_are_classified_so_automate_can_decide_whether_to_retry(HttpStatusCode status, StepRunErrorCategory expected)
    {
        var api = new FakeDevToApi().RespondWithArticles().Respond("""{"error":"Something went wrong","status":0}""", status);

        var result = await Execute(api, ValidSettings());

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(expected);
        result.Exception!.Message.ShouldContain("Something went wrong");
    }

    [Theory]
    [InlineData("", "# body", null, "title")]
    [InlineData("Title", " ", null, "body")]
    [InlineData("Title", "# body", "/relative/url", "absolute")]
    public async Task Invalid_settings_fail_validation_without_calling_the_api(string title, string body, string? canonical, string messageFragment)
    {
        var api = new FakeDevToApi();

        var result = await Execute(api, ValidSettings(s => { s.Title = title; s.BodyMarkdown = body; s.CanonicalUrl = canonical; }));

        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
        result.Exception!.Message.ShouldContain(messageFragment, Case.Insensitive);
        api.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task Unresolved_api_key_reference_is_a_configuration_error()
    {
        var result = await Execute(new FakeDevToApi(), ValidSettings(), new DevToConnectionSettings { ApiKey = "$Umbraco:Automate:Secrets:Missing" });

        result.ErrorCategory.ShouldBe(StepRunErrorCategory.ConfigurationError);
    }
}
