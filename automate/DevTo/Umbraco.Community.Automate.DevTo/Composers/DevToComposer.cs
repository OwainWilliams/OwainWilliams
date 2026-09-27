using Microsoft.Extensions.DependencyInjection;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Core.Connections;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Manifest;
using Umbraco.Community.Automate.DevTo.Actions;
using Umbraco.Community.Automate.DevTo.Articles;
using Umbraco.Community.Automate.DevTo.Client;
using Umbraco.Community.Automate.DevTo.ConnectionTypes;
using Umbraco.Community.Automate.DevTo.Content;

namespace Umbraco.Community.Automate.DevTo.Composers;

public class DevToComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddHttpClient(DevToClient.HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(30));
        builder.Services.AddSingleton<DevToClient>();
        builder.Services.AddSingleton<DevToArticlePublisher>();
        // Singleton because actions are: IDevToBlockConverter implementations are resolved
        // once, so they must be singletons too.
        builder.Services.AddSingleton<ContentMarkdownConverter>();

        builder.WithCollectionBuilder<ActionCollectionBuilder>()
            .Add<CreateOrUpdateArticleAction>()
            .Add<PublishContentAction>();

        builder.WithCollectionBuilder<ConnectionTypeCollectionBuilder>()
            .Add<DevToConnectionType>();

        builder.Services.AddSingleton<IPackageManifestReader, DevToPackageManifestReader>();
    }
}
