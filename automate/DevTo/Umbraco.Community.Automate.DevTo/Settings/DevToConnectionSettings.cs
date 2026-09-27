using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.DevTo.Settings;

/// <summary>
/// Settings for a DEV (or any other Forem) connection.
/// </summary>
public sealed class DevToConnectionSettings
{
    /// <summary>The default Forem instance.</summary>
    public const string DefaultInstanceUrl = "https://dev.to";

    [Field(Label = "API Key",
        Description = "Your DEV API key, from Settings → Extensions → DEV Community API Keys. " +
                      "Recommended: reference a secret instead of pasting it, e.g. $Umbraco:Automate:Secrets:DevToApiKey",
        SortOrder = 0,
        IsSensitive = true)]
    public string ApiKey { get; set; } = string.Empty;

    [Field(Label = "Instance URL",
        Description = "Base URL of the Forem instance. Leave as https://dev.to unless you post to another Forem community.",
        SortOrder = 1,
        Group = "Advanced")]
    public string InstanceUrl { get; set; } = DefaultInstanceUrl;
}
