using Umbraco.Automate.Core.Actions;
using Umbraco.Community.Automate.DevTo.ConnectionTypes;
using Umbraco.Community.Automate.DevTo.Settings;

namespace Umbraco.Community.Automate.DevTo.Actions;

internal static class DevToActionHelpers
{
    /// <summary>
    /// Reads and validates the step's DEV connection, or returns the failure to hand back.
    /// </summary>
    public static (DevToConnectionSettings? Settings, ActionResult? Failure) GetConnection(ActionContext context)
    {
        var settings = context.Connection?.GetSettings<DevToConnectionSettings>();

        if (settings is null)
            return (null, ActionResult.Failed(
                new InvalidOperationException($"No {DevToConnectionType.Alias} connection is configured for this step."),
                StepRunErrorCategory.ConfigurationError));

        if (DevToConnectionSettingsValidator.Validate(settings) is { } error)
            return (null, ActionResult.Failed(new InvalidOperationException(error), StepRunErrorCategory.ConfigurationError));

        return (settings, null);
    }

    /// <summary>Parses an optional article ID setting, or returns the validation failure.</summary>
    public static (long? Id, ActionResult? Failure) ParseArticleId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return (null, null);

        return long.TryParse(value.Trim(), out var id) && id > 0
            ? (id, null)
            : (null, ActionResult.Failed(
                new ArgumentException($"'{value}' is not a valid DEV article ID."), StepRunErrorCategory.Validation));
    }
}
