using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Core.Connections;
using Umbraco.Community.Automate.DevTo.Actions;
using Umbraco.Community.Automate.DevTo.Composers;
using Umbraco.Community.Automate.DevTo.ConnectionTypes;
using Umbraco.Community.Automate.DevTo.Tests.Helpers;
using Xunit;

namespace Umbraco.Community.Automate.DevTo.Tests;

/// <summary>
/// The icon name is declared in the C# attributes and in wwwroot/icons.js with nothing linking
/// them, and the manifest path must match StaticWebAssetBasePath. A mismatch fails silently in
/// the backoffice (no icon), so these tests are that link.
/// </summary>
public class DevToIconTests
{
    private static string[] RegisteredIconNames()
        => Regex.Matches(File.ReadAllText(Path.Combine(DevToPackagePaths.Wwwroot, "icons.js")), @"name:\s*""([^""]+)""")
            .Select(m => m.Groups[1].Value)
            .ToArray();

    public static TheoryData<Type> ActionTypes => new() { typeof(CreateOrUpdateArticleAction), typeof(PublishContentAction) };

    [Fact]
    public void Connection_type_uses_a_registered_icon()
        => Assert.Contains(typeof(DevToConnectionType).GetCustomAttribute<ConnectionTypeAttribute>()?.Icon, RegisteredIconNames());

    [Theory]
    [MemberData(nameof(ActionTypes))]
    public void Actions_use_a_registered_icon(Type actionType)
        => Assert.Contains(actionType.GetCustomAttribute<ActionAttribute>()?.Icon, RegisteredIconNames());

    [Fact]
    public async Task Manifest_points_at_the_static_web_asset_path_and_the_files_exist()
    {
        var manifest = Assert.Single(await new DevToPackageManifestReader().ReadPackageManifestsAsync());
        var icons = Assert.Single(JsonSerializer.SerializeToElement(manifest.Extensions).EnumerateArray(),
            e => e.GetProperty("type").GetString() == "icons");

        var js = icons.GetProperty("js").GetString()!;
        Assert.Equal("/App_Plugins/UmbracoCommunityAutomateDevTo/icons.js", js);
        Assert.True(File.Exists(Path.Combine(DevToPackagePaths.Wwwroot, Path.GetFileName(js))));
        Assert.True(File.Exists(Path.Combine(DevToPackagePaths.Wwwroot, "devto.icon.js")));
    }
}
