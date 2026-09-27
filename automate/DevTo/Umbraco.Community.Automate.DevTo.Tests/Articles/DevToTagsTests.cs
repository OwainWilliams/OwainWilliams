using Shouldly;
using Umbraco.Community.Automate.DevTo.Articles;
using Xunit;

namespace Umbraco.Community.Automate.DevTo.Tests.Articles;

public class DevToTagsTests
{
    [Theory]
    [InlineData("umbraco, dotnet", new[] { "umbraco", "dotnet" })]
    [InlineData("#umbraco #dotnet", new[] { "umbraco", "dotnet" })]
    [InlineData("umbraco;dotnet", new[] { "umbraco", "dotnet" })]
    [InlineData("Umbraco CMS", new[] { "Umbraco CMS" })]
    [InlineData("", new string[0])]
    public void Split_handles_common_separators_but_not_spaces(string input, string[] expected)
        => DevToTags.Split(input).ShouldBe(expected);

    [Fact]
    public void Normalise_lowercases_and_strips_to_alphanumerics()
        => DevToTags.Normalise(["Umbraco CMS", ".NET", "C#", "Héllo-World"]).ShouldBe(["umbracocms", "net", "c", "helloworld"]);

    [Fact]
    public void Normalise_removes_duplicates_and_keeps_the_first_four()
        => DevToTags.Normalise(["umbraco", "Umbraco", "dotnet", "csharp", "", "webdev", "blog"])
            .ShouldBe(["umbraco", "dotnet", "csharp", "webdev"]);
}
