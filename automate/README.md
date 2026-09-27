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
dotnet test Umbraco.Community.Automate.DevTo.slnx
```

## Moving DevTo to the community repo

1. Copy `DevTo/` to the root of Umbraco.Community.Automate.
2. Add `<PackageVersion Include="ReverseMarkdown" Version="6.2.1" />` to its `Directory.Packages.props`.
3. Add the two projects to `Umbraco.Community.Automate.slnx` under a `/DevTo/` folder, and a
   row to its README's package table.
4. Reference the package from `Umbraco.Community.Automate.Demo` to try it end to end.
5. Release by pushing a `devto-v1.0.0` tag.
