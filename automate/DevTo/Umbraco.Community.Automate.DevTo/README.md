# Umbraco.Community.Automate.DevTo

A [DEV Community](https://dev.to) (dev.to) connection and actions for [Umbraco Automate](https://github.com/umbraco/Umbraco.Automate).

Cross-post your Umbraco content to DEV automatically when you publish it. Markdown, Rich Text, Block List and Block Grid content is converted to Markdown, relative links and images are made absolute, and the canonical URL points back at your site so search engines treat it as the original.

Works with **Umbraco 17 and 18** (and Umbraco Automate 17 and 18).

## Installation

```bash
dotnet add package Umbraco.Community.Automate.DevTo
```

No further setup required. The composer registers itself automatically via Umbraco's `IComposer` discovery.

## Setup

### 1. Generate a DEV API key

On DEV, go to **Settings → Extensions → DEV Community API Keys**, give the key a description and click **Generate API Key**.

### 2. Store the key as a secret

Add it to Umbraco Automate's built-in **Secrets** section rather than pasting it into the backoffice:

```json
{
  "Umbraco": {
    "Automate": {
      "Secrets": {
        "DevToApiKey": "your-api-key"
      },
      "Variables": {
        "SiteUrl": "https://your-site.com"
      }
    }
  }
}
```

For production, use environment variables instead:

```
Umbraco__Automate__Secrets__DevToApiKey=your-api-key
Umbraco__Automate__Variables__SiteUrl=https://your-site.com
```

`SiteUrl` is only needed if Umbraco can't generate absolute URLs for your content (see [URLs](#urls)).

### 3. Create the connection

1. Go to **Automate → Connections** and create a new **DEV Community** connection.
2. **API Key**: `$Umbraco:Automate:Secrets:DevToApiKey`
3. Click **Test connection**. You should see "Connected as @yourname".

Posting to a different [Forem](https://forem.com) community? Change **Instance URL** under *Advanced*.

## Cross-posting blog posts

Create an automation:

1. **Trigger:** *Content Published*, with **Content Types** set to your blog post type.
2. **Action:** *Publish Content to DEV*, with your DEV connection.

| Setting | Description |
|---|---|
| Content Key | The item to post. Defaults to `${ trigger.contentKey }`. |
| Body Properties | Alias(es) of the body properties, comma separated and in order, e.g. `intro, blocks`. |
| Title Property | Alias of the title property. Blank uses the content name. |
| Tags Property | A Tags, text (`umbraco, dotnet`) or content picker property. |
| Publish Mode | **Save as draft** (default) or **Publish**. |
| Additional Tags | Tags added to every article, e.g. `umbraco, dotnet`. |
| Description Property | A summary for feeds and link previews. HTML is stripped. |
| Cover Image Property | A media picker (or URL text) property. |
| Series | Links articles together as a series on DEV. |
| Culture | For variant content. Blank uses the default culture. |
| Site URL | Your public base URL, e.g. `$Umbraco:Automate:Variables:SiteUrl`. |
| Canonical URL | Override the canonical URL. |
| Existing Article ID | Update this DEV article instead of looking one up. |

Tags are lowercased and stripped to letters and numbers, as DEV requires ("Umbraco CMS" becomes `umbracocms`), and only the first four are used.

### Updates, not duplicates

Before posting, the action looks through your DEV articles (published and drafts) for one with the same canonical URL. If it finds one, it updates it; otherwise it creates a new one. So re-publishing a post in Umbraco updates its copy on DEV, and a retried step never posts twice. No extra property on your document type is needed.

If you change a post's URL, the lookup won't find the old article. Set **Existing Article ID** to point it at the right one.

**Save as draft** never unpublishes. New articles are created as drafts, and an article you've already published on DEV stays published when it's updated.

### Outcomes and outputs

The action produces a `created`, `updated` or `notFound` outcome (`notFound` means the content was unpublished before the step ran), so later steps can branch. For example, announce new posts on Mastodon only on `created`, so edits don't post again.

Its output is available to later steps:

| Output | Example |
|---|---|
| `${ steps.<alias>.url }` | `https://dev.to/you/my-post-1a2b` |
| `${ steps.<alias>.articleId }` | `1234567` |
| `${ steps.<alias>.slug }` | `my-post-1a2b` |
| `${ steps.<alias>.published }` | `false` |
| `${ steps.<alias>.canonicalUrl }` | `https://your-site.com/blog/my-post/` |

### Reviewing before publishing

Leave **Publish Mode** on *Save as draft* and publish on DEV yourself, or add Automate's *Request Approval* step followed by a second *Publish Content to DEV* step set to *Publish*.

## How content is converted

| Content | Becomes |
|---|---|
| Markdown editor | The Markdown you wrote, unchanged. |
| Rich Text | Converted from HTML. Blocks in the editor are rendered by your site's partial views first. Video embeds (YouTube, Vimeo, …) become DEV `{% embed %}` tags. |
| Block List / Block Grid | Each block's properties in order, including nested blocks and grid areas. |
| Textstring / Textarea (in a block) | Text. A Textstring whose alias ends in `heading`, `headline` or `title` becomes a `##` heading. |
| Media picker (in a block) | An image, using the media's `altText` property or its name as alt text. |
| Multi URL picker (in a block) | Links. |
| Code blocks | A block with a `code` (or `codeSnippet`, `snippet`, `sourceCode`, `codeBlock`) property becomes a fenced code block, highlighted using a `language`, `lang`, `codeLanguage` or `syntax` property. |
| Anything else | Left out: toggles, colours, content pickers and so on have no place in an article. |

Relative links and image URLs are made absolute. Code samples are left untouched.

### Custom blocks

If the built-in conversion doesn't suit a block, implement `IDevToBlockConverter` and register it:

```csharp
public class CalloutConverter : IDevToBlockConverter
{
    public string? Convert(IPublishedElement content, IPublishedElement? settings, DevToConversionContext context)
        => content.ContentType.Alias switch
        {
            "callout" => $"> **Note:** {context.ConvertProperty(content, "text")}",
            "newsletterSignup" => "",   // leave this block out
            _ => null,                  // not mine: use the next converter / built-in conversion
        };
}

public class DevToConvertersComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddSingleton<IDevToBlockConverter, CalloutConverter>();
}
```

`DevToConversionContext` gives you the same helpers the built-in conversion uses: `ConvertProperty`, `ConvertElement` (for nested blocks), `HtmlToMarkdown`, `ResolveUrl` and `GetMediaUrl`.

## URLs

DEV needs absolute URLs for the canonical link, links and images. By default the action uses the absolute URL Umbraco generates for the content, which works when the site has a domain assigned (**Culture and Hostnames**) or `Umbraco:CMS:WebRouting:UmbracoApplicationUrl` is set. Otherwise, set **Site URL** on the step, e.g. to `$Umbraco:Automate:Variables:SiteUrl`.

## Posting Markdown from anywhere

The *Create or Update DEV Article* action takes a title, Markdown body, canonical URL and tags directly (all support bindings), for automations whose content doesn't come from an Umbraco document. It updates rather than duplicates in the same way.

## Errors and retries

API failures are classified so Automate can decide what to do: rate limiting (429), timeouts and DEV being unavailable (5xx) are transient and retried according to the step's error behaviour; an invalid API key, a rejected article (422) or missing settings fail straight away with the DEV error message.

## Compatibility

One build of the package supports Umbraco 17 and 18. It's compiled against 17 and every change is tested on both, including running the 17 build on 18 and checking every Umbraco API it calls still exists there. Umbraco 19 isn't supported until it has been tested.
