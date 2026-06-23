using System.Text.Json;
using Microsoft.Extensions.Options;

namespace BlazorStarter.Processors;

// Example form processor: posts submitted form data to a Microsoft Teams channel via an
// incoming webhook (e.g. a Power Automate "When a webhook request is received" flow).
//
// Form submissions matching FormName + ApiKey are formatted as an Adaptive Card with one
// fact per form field, plus the request id and user agent, and POSTed to the webhook URL
// configured under the "Teams" section in appsettings (or User Secrets).
public class SendToTeamsFormProcessor( HttpClient httpClient, IOptions<TeamsWebhookOptions> options ) : IFormProcessor<object>
{
	public string FormName { get; } = "Contact Us";
	public string ApiKey { get; } = "[YOUR-TEAMS-PROCESSOR-API-KEY]";   // replace with a GUID unique to this processor
	public Type ModelType { get; } = typeof( object );

	public Task Process( string formId, string formName, string requestId, string? userAgent, object? data )
		=> Process( new FormData<object>( data ) { FormId = formId, FormName = formName, RequestId = requestId, UserAgent = userAgent } );

	public async Task Process( FormData<object> model )
	{
		if ( string.IsNullOrWhiteSpace( options.Value.WebhookUrl ) )
			return;

		var facts = new List<object> {
			new { title = "Request ID", value = model.RequestId ?? "N/A" },
			new { title = "Form ID", value = model.FormId ?? "N/A" }
		};

		if ( model.Data is not null )
		{
			var dataJson = JsonSerializer.Serialize( model.Data );
			using var doc = JsonDocument.Parse( dataJson );
			foreach ( var prop in doc.RootElement.EnumerateObject() )
			{
				var value = prop.Value.ValueKind == JsonValueKind.String
					? prop.Value.GetString()
					: prop.Value.ToString();
				facts.Add( new { title = prop.Name, value = value ?? "N/A" } );
			}
		}

		facts.Add( new { title = "User Agent", value = model.UserAgent ?? "N/A" } );

		var card = new {
			type = "message",
			attachments = new[] {
				new {
					contentType = "application/vnd.microsoft.card.adaptive",
					content = new {
						type = "AdaptiveCard",
						version = "1.4",
						body = new object[] {
							new {
								type = "TextBlock",
								text = $"New Form Submission: {model.FormName}",
								weight = "Bolder",
								size = "Medium"
							},
							new {
								type = "FactSet",
								facts
							}
						}
					}
				}
			}
		};

		var json = JsonSerializer.Serialize( card );
		var content = new StringContent( json, System.Text.Encoding.UTF8, "application/json" );

		try
		{
			var response = await httpClient.PostAsync( options.Value.WebhookUrl, content );
			response.EnsureSuccessStatusCode();
		}
		catch ( Exception ex )
		{
			Console.WriteLine( $"Failed to send to Teams: {ex.Message}" );
		}
	}
}

public class TeamsWebhookOptions
{
	public string? WebhookUrl { get; set; }
}
