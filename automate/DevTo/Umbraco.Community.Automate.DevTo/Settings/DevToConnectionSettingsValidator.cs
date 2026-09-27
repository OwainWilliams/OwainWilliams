namespace Umbraco.Community.Automate.DevTo.Settings;

/// <summary>
/// Shared validation for <see cref="DevToConnectionSettings"/>, used both when testing the
/// connection and before an action calls the API.
/// </summary>
public static class DevToConnectionSettingsValidator
{
    /// <summary>
    /// Returns an error message, or <c>null</c> when the settings are usable.
    /// </summary>
    public static string? Validate(DevToConnectionSettings? settings)
    {
        if (settings is null)
            return "No DEV connection settings were provided.";

        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            return "An API key is required.";

        if (settings.ApiKey.StartsWith('$'))
            return $"The API key reference '{settings.ApiKey}' could not be resolved. Check the key exists under Umbraco:Automate:Secrets.";

        if (!Uri.TryCreate(settings.InstanceUrl?.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            return $"'{settings.InstanceUrl}' is not a valid instance URL. Use a full URL such as {DevToConnectionSettings.DefaultInstanceUrl}.";

        return null;
    }
}
