# Forms
[&lt; Docs index](index.md)

Forms can be authored in SitecoreAI and added to pages using the Pages editor. The Blazor SDK renders those forms in your app, and the Starter Kit adds a small, extensible pipeline for doing something useful with the data when a visitor submits one - saving it, emailing it, posting it to Teams, or handing it to your own code.

This page covers the whole journey: rendering a form, receiving a submission, and processing it.

## Rendering a form

A Sitecore Forms rendering is mapped to the Starter Kit's `Form.razor` component (`UI/Components/Form.razor`). The component takes a `FormId` (supplied by Sitecore as a rendering parameter), asks the SDK's forms service for the form markup, and renders it:

```razor
@inherits SitecoreComponent<Form.ViewModel>
@inject ISitecoreFormsSdk Forms

@if ( HasRecapcha )
{
    <ComponentScript Src="./UI/Components/Form.razor.js" Arg="@Id" />
}

<div class="@Model.Styles" id="@Id">
    @if ( FormContent is not null )
    {
        @FormContent
    }
</div>

@code {
    public sealed class ViewModel : ViewModelBase
    {
        [SitecoreComponentParam]
        public string? FormId { get; set; }
    }

    private MarkupString? FormContent;
    private bool HasRecapcha => FormContent?.Value.Contains("grecaptcha") ?? false;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        if ( !string.IsNullOrEmpty( Model.FormId ) )
        {
            var formResp = await Forms.GetForm( Model.FormId );
            if ( formResp.IsSuccessful && !string.IsNullOrEmpty( formResp.Result ) )
                FormContent = new MarkupString( formResp.Result );
        }
    }
}
```

The rendered markup is a standard HTML form that posts to your submission endpoint. If the form includes a reCAPTCHA, `Form.razor` loads `Form.razor.js`, which re-initialises the captcha across Blazor re-renders.

## Receiving a submission

Submitted form data arrives at a webhook endpoint registered in `Program.cs`:

```csharp
app.MapPost( "/api/webhooks/form-data", FormReceiver.ReceiveData );
```

`FormReceiver.ReceiveData` reads a small set of headers from the request and dispatches the body to the matching processors:

| Header | Purpose |
| - | - |
| `x-formname` | The form's name. Used to match processors. Required. |
| `x-formid` | The form's id. Passed through to processors. Required. |
| `apikey` | Used to match processors that declare an `ApiKey`. |
| `x-request-id` | Correlation id. Falls back to `traceparent`, then `X-ARR-LOG-ID`, then a new GUID. |
| `User-Agent` | Passed through to processors. |

The endpoint deserialises the JSON body into each matching processor's declared `ModelType` and invokes the processor. It returns `202 Accepted` if at least one processor ran, `422 Unprocessable Content` if none matched, and `400 Bad Request` if the form name or id is missing.

## Processing submissions: `IFormProcessor`

A processor is any class that implements `IFormProcessor` and is registered in DI. The interface is small:

```csharp
public interface IFormProcessor
{
    string FormName { get; }   // the form this processor handles, or "*" for all forms
    string? ApiKey { get; }    // required api key, or null to accept any
    Type ModelType { get; }    // the type the JSON body is deserialised into

    Task Process( string formId, string formName, string requestId, string? userAgent, object? data );
}
```

There is also a generic convenience interface, `IFormProcessor<TModel>`, that adds a strongly-typed `Process( FormData<TModel> data )` overload. `FormData<TModel>` wraps the submitted `Data` along with `FormId`, `FormName`, `RequestId` and `UserAgent`.

A processor matches a submission when **both**:

- its `FormName` equals the submitted `x-formname` (case-insensitive), or is `"*"` (all forms), and
- its `ApiKey` is `null` (accepts any), or equals the submitted `apikey` (case-insensitive).

Every matching processor runs, so a single form can fan out to several processors (e.g. save to disk *and* email *and* post to Teams).

### Registering processors

Register processors in DI in your `Program.cs` (or in an extension method):

```csharp
services
    .AddSingleton<IFormProcessor, SaveDataFormProcessor>()
    .AddSingleton<IFormProcessor, SendToTeamsFormProcessor>()
    .AddSingleton<IFormProcessor, SendToEmailFormProcessor>();
```

## The processors shipped with the Starter Kit

| Processor | FormName | Reads config | What it does |
| - | - | - | - |
| `SaveDataFormProcessor` | `*` (all forms) | `SyncSettings.DataPath` | Writes every submission to `{DataPath}/FormsData/{FormName}-{timestamp}.json`. A safety net so no submission is ever lost. |
| `SendToTeamsFormProcessor` | `Contact Us` | `Teams` | Posts an Adaptive Card to a Microsoft Teams channel. See below. |
| `SendToEmailFormProcessor` | `Contact Us` | `Email` | Sends an HTML email of the submission via Azure Communication Services. |

`SaveDataFormProcessor` declares no api key and `FormName = "*"`, so it captures everything. The others declare an api key, so they only run when the submitting form sends the matching key.

## Writing your own processor

1. Create a class implementing `IFormProcessor` (or `IFormProcessor<TModel>` for a typed model).
2. Set `FormName` to the form you want to handle (or `"*"`), `ApiKey` to a key the form will send (or `null`), and `ModelType` to the type the body should deserialise into (use `object` or `JsonElement` if the shape is dynamic).
3. Implement `Process` to do the work.
4. Register it with `.AddSingleton<IFormProcessor, YourProcessor>()`.

Bind any configuration your processor needs with the options pattern, exactly as the Teams and Email processors do.

---

## Microsoft Teams webhook setup (SendToTeamsFormProcessor)

`SendToTeamsFormProcessor` sends form submissions to a Microsoft Teams channel using an incoming webhook. It reads its webhook URL from the `Teams` configuration section.

### 1. Create an Incoming Webhook in Teams

1. Navigate to the Teams channel where you want to receive form notifications
2. Click the `...` (More options) next to the channel name
3. Select **Workflows**
4. Search for and add **Send Webhook Alert to &lt;Channel&gt;**
5. Click **Create**
6. Copy the webhook URL provided

### 2. Configure the Application

Add the webhook URL to your configuration:

**Option A: appsettings.json** (for non-sensitive URLs or development)
```json
"Teams": {
  "WebhookUrl": "https://your-org.webhook.office.com/webhookb2/..."
}
```

**Option B: User Secrets** (recommended for development)
```bash
dotnet user-secrets set "Teams:WebhookUrl" "https://your-org.webhook.office.com/webhookb2/..."
```

**Option C: Environment Variables** (recommended for production)
```bash
Teams__WebhookUrl=https://your-org.webhook.office.com/webhookb2/...
```

### 3. Test the Integration

Submit the Contact Us form and verify that a card appears in your Teams channel with the form data.

### Message Format

The processor sends an Adaptive Card to Teams with the following information:
- Form Name
- Request ID
- Form ID
- Every property on the submitted form's data object (the processor walks the JSON payload and renders one fact per field)
- User Agent

### Troubleshooting

- If messages aren't appearing, check the application logs for errors
- Verify the webhook URL is correctly configured
- Ensure the webhook hasn't been removed or disabled in Teams
- Check that the HttpClient can reach the webhook URL (firewall/network restrictions)
- Confirm the submitting form sends the api key that `SendToTeamsFormProcessor` expects, and that its `x-formname` matches `Contact Us`

---

## Email setup (SendToEmailFormProcessor)

`SendToEmailFormProcessor` sends an HTML summary of the submission using Azure Communication Services. Configure it through the `Email` section:

```json
"Email": {
  "Endpoint": "https://your-acs-resource.communication.azure.com/",
  "FromAddress": "DoNotReply@your-domain.com",
  "ToAddress": "sales@your-domain.com"
}
```

You can authenticate either with an `Endpoint` and `TokenCredential` (using `DefaultAzureCredential` for development) or a full `ConnectionString`. If neither the endpoint/connection string nor a from-address is configured, the processor quietly does nothing, so it's safe to leave unconfigured in environments where you don't want email or remove the DI registration for the processor.
