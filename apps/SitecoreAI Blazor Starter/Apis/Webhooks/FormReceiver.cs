using System.Net;
using System.Text.Json;
using BlazorStarter.Processors;

namespace BlazorStarter.Apis.Webhooks;

public static class FormReceiver
{
	public const string AllFormNames = "*";
	private const string FormRequestHeaderKey = "x-request-id";
	// Fallback headers used when x-request-id is absent (e.g. Sitecore Cloud sends traceparent/X-ARR-LOG-ID instead)
	private const string TraceParentHeaderKey = "traceparent";
	private const string ArrLogIdHeaderKey = "X-ARR-LOG-ID";
	private const string FormIdHeaderKey = "x-formid";
	private const string FormNameHeaderKey = "x-formname";
	private const string ApiKeyHeaderKey = "apikey";
	private const string UserAgentHeaderKey = "User-Agent";

	public static async Task ReceiveData( IServiceProvider ioc, HttpContext context )
	{
		context.Request.Headers.TryGetValue( FormNameHeaderKey, out var formName );
		context.Request.Headers.TryGetValue( FormIdHeaderKey, out var formId );
		context.Request.Headers.TryGetValue( ApiKeyHeaderKey, out var apiKey );
		context.Request.Headers.TryGetValue( FormRequestHeaderKey, out var requestId );
		context.Request.Headers.TryGetValue( UserAgentHeaderKey, out var userAgent );

		// Resolve a correlation ID from fallback headers if x-request-id is absent
		var resolvedRequestId = requestId.FirstOrDefault( v => !string.IsNullOrEmpty( v ) )
			?? context.Request.Headers[TraceParentHeaderKey].FirstOrDefault( v => !string.IsNullOrEmpty( v ) )
			?? context.Request.Headers[ArrLogIdHeaderKey].FirstOrDefault( v => !string.IsNullOrEmpty( v ) )
			?? Guid.NewGuid().ToString();

		if ( string.IsNullOrEmpty( formName.ToString() ) || string.IsNullOrEmpty( formId.ToString() ) )
		{
			context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
			return;
		}

		using var sw = new StreamReader( context.Request.Body );
		var json = await sw.ReadToEndAsync().ConfigureAwait( false );

		var processed = false;

		foreach ( var item in (ioc.GetServices<IFormProcessor>() ?? []).Where( fp => IsProcessor( fp, formName, apiKey ) ) )
		{
			processed = true;
			_ = item.Process(
					formId.ToString(),
					formName.ToString(),
					resolvedRequestId,
					userAgent,
					JsonSerializer.Deserialize( json, item.ModelType, JsonSerializerOptions.Web )
				).ConfigureAwait( false );
		}

		context.Response.StatusCode = processed ? (int)HttpStatusCode.Accepted : (int)HttpStatusCode.UnprocessableContent;
	}

	private static bool IsProcessor( IFormProcessor fp, string? formName, string? apiKey )
		=> (fp.ApiKey is null || fp.ApiKey.Equals( apiKey, StringComparison.OrdinalIgnoreCase ))
			&& (fp.FormName == AllFormNames || fp.FormName.Equals( formName, StringComparison.OrdinalIgnoreCase ));
}
