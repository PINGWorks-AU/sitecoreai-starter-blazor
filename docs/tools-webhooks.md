# Edge Webhooks
[&lt; Docs index](index.md)

The **Edge Webhooks** tool on the [Tools page](tools.md) lists the publish webhooks registered against your Sitecore environment and lets you inspect and remove them. These are the webhooks Sitecore Experience Edge calls when a publish completes — the same mechanism the SDK uses to keep its [cache in sync](caching-sync.md). The SDK can register one automatically for each host (`SyncSettings.ChangeFeed.AutoCreateEdgeWebhook`); over time, across developer machines and redeploys, an environment can accumulate stale entries, and this tool is how you see and tidy them.

It talks to Sitecore through the Experience Edge **Admin SDK**.

## Credentials required

Managing webhooks needs Edge **administration credentials** — `SyncSettings.ChangeFeed.ClientId` and `ClientSecret` (set via [User Secrets](settings.md)). When they aren't configured, the tab shows a **403 “Credentials required”** state instead of the webhook list. These are the same credentials the SDK uses to auto-register its publish webhook; see [Caching and content sync](caching-sync.md).

## The webhook list (left)

Every webhook registered for the environment is listed with its **label** and **callback URL**. The webhook the SDK auto-creates for the current host is tagged with a **“This machine”** badge — it's the one whose label matches this host's identifier (`SyncSettings.ChangeFeed.UniqueId`, falling back to the machine name), so you can tell your own environment's webhook apart from those belonging to other developers or deployments.

When the Tools page is **not** read-only, each row has a **Delete** button (behind a confirmation) that removes the webhook via the Admin SDK — Sitecore then stops posting publish notifications to it. Set `SitecoreSettings.Tools.CacheToolsReadOnly` to `true` to hide the Delete actions and make the tool view-only. See [The Tools page](tools.md).

## Webhook detail (right)

Selecting a webhook fetches its full record and shows it on the right. This pane is **always read-only**, regardless of the read-only setting:

- **Id**, **Label**, **Tenant**, **URL**, **Method**, **Execution mode**, **Created by** and **Created**.
- **Headers** sent with each call — secret-looking values such as an `x-apikey` are partially masked.
- **Body** / body-include payload, when the webhook defines one.
- **Recent runs** — the last execution results Sitecore recorded, each marked **OK** or **Failed** with its timestamp and any error message. A quick way to confirm Edge is actually reaching your host (handy when a Dev Tunnel URL has gone stale).

## Access

The Edge Webhooks tool is part of the Tools page and inherits its protection — keep `/.tools` restricted to trusted IPs via [IP and path access control](ip-path-security.md).

## Related

- [Caching and content sync](caching-sync.md) — why these webhooks exist and how the SDK registers one.
- [The Tools page](tools.md) — the container and how `/.tools` is secured.
- [Configuration settings](settings.md) — the `SyncSettings.ChangeFeed` credentials and options.
