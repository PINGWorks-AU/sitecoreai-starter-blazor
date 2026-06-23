using System.Text;
using System.Text.Json;
using Azure;
using Azure.Communication.Email;
using Azure.Identity;
using Microsoft.Extensions.Options;

namespace BlazorStarter.Processors;

// Example form processor: sends an HTML summary of a form submission via Azure
// Communication Services. Authenticate either with an Endpoint (using DefaultAzureCredential,
// recommended) or a full ConnectionString. Configure under the "Email" section in appsettings
// (or User Secrets).
public class SendToEmailFormProcessor( IOptions<EmailProcessorOptions> options ) : IFormProcessor<object>
{
	public string FormName { get; } = "Contact Us";
	public string ApiKey { get; } = "[YOUR-EMAIL-PROCESSOR-API-KEY]";   // replace with a GUID unique to this processor
	public Type ModelType { get; } = typeof( object );

	public Task Process( string formId, string formName, string requestId, string? userAgent, object? data )
		=> Process( new FormData<object>( data ) { FormId = formId, FormName = formName, RequestId = requestId, UserAgent = userAgent } );

	public async Task Process( FormData<object> model )
	{
		var cfg = options.Value;

		if ( (string.IsNullOrWhiteSpace( cfg.ConnectionString ) && string.IsNullOrWhiteSpace( cfg.Endpoint ))
				|| string.IsNullOrWhiteSpace( cfg.FromAddress )
				|| string.IsNullOrWhiteSpace( cfg.ToAddress ) )
			return;

		var body = BuildBody( model );

		var emailClient = !string.IsNullOrWhiteSpace( cfg.Endpoint )
			? new EmailClient( new Uri( cfg.Endpoint! ), new DefaultAzureCredential() )
			: new EmailClient( cfg.ConnectionString );

		var emailMessage = new EmailMessage(
				senderAddress: cfg.FromAddress,
				content: new EmailContent( $"New Form Submission: {model.FormName}" ) { Html = body },
				recipients: new EmailRecipients( [ new EmailAddress( cfg.ToAddress! ) ] )
			);

		try
		{
			await emailClient.SendAsync( WaitUntil.Started, emailMessage );
		}
		catch ( RequestFailedException ex )
		{
			Console.WriteLine( $"Failed to send email: {ex.Message}" );
		}
	}

	private static string BuildBody( FormData<object> model )
	{
		var rows = new StringBuilder();

		if ( model.Data is not null )
		{
			var dataJson = JsonSerializer.Serialize( model.Data );
			using var doc = JsonDocument.Parse( dataJson );
			foreach ( var prop in doc.RootElement.EnumerateObject() )
			{
				if ( prop.Name == "g-recaptcha-response" )
					continue;

				var value = prop.Value.ValueKind == JsonValueKind.String
					? prop.Value.GetString()
					: prop.Value.ToString();

				rows.AppendLine( $"<tr><td><strong>{prop.Name}</strong></td><td>{value ?? "N/A"}</td></tr>" );
			}
		}

		return $"""
			<html>
			<body>
			<h2>New Form Submission: {model.FormName}</h2>
			<table border="0" cellpadding="4" cellspacing="0">
			{rows}
			</table>
			</body>
			</html>
			""";
	}
}

public class EmailProcessorOptions
{
	public string? ConnectionString { get; set; }
	public string? Endpoint { get; set; }
	public string? FromAddress { get; set; }
	public string? ToAddress { get; set; }
}
