# Umbraco Automate packages (staging)

A temporary home for Umbraco Automate packages before they move to
[umbraco-community/Umbraco.Community.Automate](https://github.com/umbraco-community/Umbraco.Community.Automate).

This folder mirrors that repo's layout and conventions (central package management, a
`<Provider>/` folder holding the package and its tests, MinVer tag prefixes), so moving a
package across is a copy.

| Package | Description |
|---|---|
| [Umbraco.Community.Automate.DevTo](DevTo/Umbraco.Community.Automate.DevTo/README.md) | DEV (dev.to) connection and actions: cross-post Markdown, Rich Text, Block List and Block Grid content as DEV articles |

## Building and testing

```bash
dotnet build Umbraco.Community.Automate.DevTo.slnx
dotnet test Umbraco.Community.Automate.DevTo.slnx                      # Umbraco 17
dotnet test Umbraco.Community.Automate.DevTo.slnx -p:UmbracoMajor=18   # Umbraco 18
```

The package supports Umbraco 17 and 18 from one build (see `DevTo/UmbracoVersions.props`).
`DevTo/test-umbraco-compat.sh` runs the full check CI runs: tests on both majors, then the
17 build (what ships) running on 18, and a scan confirming every Umbraco API it references
still exists in 18.

## Moving DevTo to the community repo

1. Copy `DevTo/` to the root of Umbraco.Community.Automate.
2. Add `<PackageVersion Include="ReverseMarkdown" Version="6.2.1" />` to its `Directory.Packages.props`.
3. Add the two projects to `Umbraco.Community.Automate.slnx` under a `/DevTo/` folder, and a
   row to its README's package table.
4. Add a step running `DevTo/test-umbraco-compat.sh` to the repo's `ci.yml` (see
   `.github/workflows/automate-devto.yml` in this repo).
5. Reference the package from `Umbraco.Community.Automate.Demo` to try it end to end.
6. Release by pushing a `devto-v1.0.0` tag.
