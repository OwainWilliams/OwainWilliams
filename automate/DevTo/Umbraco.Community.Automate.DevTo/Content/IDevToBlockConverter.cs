using Umbraco.Cms.Core.Models.PublishedContent;

namespace Umbraco.Community.Automate.DevTo.Content;

/// <summary>
/// Converts a Block List, Block Grid or Rich Text block to Markdown. Register implementations
/// in DI to control how your own blocks appear on DEV:
/// <code>builder.Services.AddSingleton&lt;IDevToBlockConverter, MyCodeBlockConverter&gt;();</code>
/// Converters run in registration order, before the built-in conversion. They are resolved
/// once and shared, so register them as singletons (use <c>IServiceScopeFactory</c> if you
/// need a scoped service).
/// </summary>
public interface IDevToBlockConverter
{
    /// <summary>
    /// Returns the block as Markdown, an empty string to leave the block out, or <c>null</c>
    /// to let the next converter (and finally the built-in conversion) handle it.
    /// </summary>
    /// <param name="content">The block's content element.</param>
    /// <param name="settings">The block's settings element, if it has one.</param>
    /// <param name="context">
    /// Helpers for converting nested properties, HTML and URLs the same way the built-in
    /// conversion does.
    /// </param>
    string? Convert(IPublishedElement content, IPublishedElement? settings, DevToConversionContext context);
}
