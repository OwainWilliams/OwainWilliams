using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Infrastructure.Manifest;

namespace Umbraco.Community.Automate.DevTo.Composers;

public class DevToPackageManifestReader : IPackageManifestReader
{
    /// <summary>
    /// Where this package's static web assets are served from, set by
    /// StaticWebAssetBasePath in the csproj.
    /// </summary>
    private const string AppPluginPath = "/App_Plugins/UmbracoCommunityAutomateDevTo";

    public Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync()
    {
        var version = typeof(DevToPackageManifestReader).Assembly.GetName().Version?.ToString() ?? "1.0.0";
        return Task.FromResult<IEnumerable<PackageManifest>>(new[]
        {
            new PackageManifest
            {
                Id = "Umbraco.Community.Automate.DevTo",
                Name = "Umbraco Community Automate DEV",
                Version = version,
                AllowTelemetry = true,

                // Registered from C# rather than umbraco-package.json so the package needs
                // no Client build step — the icon is its only front-end code.
                Extensions =
                [
                    new
                    {
                        type = "icons",
                        alias = "UmbracoCommunityAutomateDevTo.Icons",
                        name = "DEV Icons",
                        js = $"{AppPluginPath}/icons.js",
                    },
                ]
            }
        });
    }
}
