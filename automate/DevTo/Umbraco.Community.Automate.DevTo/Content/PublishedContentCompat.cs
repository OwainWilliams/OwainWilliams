using System.Linq.Expressions;
using System.Reflection;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace Umbraco.Community.Automate.DevTo.Content;

/// <summary>
/// Reads members that Umbraco 18 moved from <see cref="IPublishedContent"/> up to
/// <see cref="IPublishedElement"/>.
/// </summary>
/// <remarks>
/// A direct <c>content.Name</c> compiles to a call on whichever interface declared it in the
/// Umbraco version the package was built against, and throws <see cref="MissingMethodException"/>
/// on the other major. This package ships one build for Umbraco 17 and 18, so these reads bind
/// to the declaring interface at runtime instead (once, as compiled delegates).
/// Everything else this package uses is declared in the same place in both majors; the
/// compatibility test in CI runs the 17 build against 18 to keep it that way.
/// </remarks>
internal static class PublishedContentCompat
{
    private static readonly Func<IPublishedContent, string?> NameGetter = CreateGetter<string?>("Name");

    private static readonly Func<IPublishedContent, IReadOnlyDictionary<string, PublishedCultureInfo>> CulturesGetter =
        CreateGetter<IReadOnlyDictionary<string, PublishedCultureInfo>>("Cultures");

    /// <summary>Gets the name in the given culture, falling back to the invariant name.</summary>
    public static string? GetName(IPublishedContent content, string? culture = null)
        => culture is not null && GetCultures(content).TryGetValue(culture, out var info) ? info.Name : NameGetter(content);

    public static IReadOnlyDictionary<string, PublishedCultureInfo> GetCultures(IPublishedContent content)
        => CulturesGetter(content);

    private static Func<IPublishedContent, T> CreateGetter<T>(string propertyName)
    {
        // Type.GetProperty on an interface only sees members it declares itself, so this finds
        // IPublishedContent's on 17 and falls through to IPublishedElement's on 18.
        var property = typeof(IPublishedContent).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
                       ?? typeof(IPublishedElement).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
                       ?? throw new MissingMemberException(nameof(IPublishedContent), propertyName);

        var content = Expression.Parameter(typeof(IPublishedContent), "content");
        var read = Expression.Property(Expression.Convert(content, property.DeclaringType!), property);

        return Expression.Lambda<Func<IPublishedContent, T>>(Expression.Convert(read, typeof(T)), content).Compile();
    }
}
