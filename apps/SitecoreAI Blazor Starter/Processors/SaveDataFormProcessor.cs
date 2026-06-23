using System.Text.Json;
using BlazorStarter.Apis.Webhooks;
using Microsoft.Extensions.Options;
using PINGWorks.SitecoreAI.BlazorSDK.Settings;

namespace BlazorStarter.Processors;

public class SaveDataFormProcessor : IFormProcessor<JsonElement>
{
	private readonly string BaseDir;

	public string FormName { get; } = FormReceiver.AllFormNames;
	public string? ApiKey { get; } = null;
	public Type ModelType
		=> typeof( JsonElement );

	public SaveDataFormProcessor( IOptions<SyncSettings> settings )
	{
		BaseDir = Path.Combine( settings.Value.DataPath, "FormsData" );
		Directory.CreateDirectory( BaseDir );
	}

	public Task Process( string formId, string formName, string requestId, string? userAgent, object? data )
		=> Process( new FormData<JsonElement>( data ) { FormId = formId, FormName = formName, RequestId = requestId, UserAgent = userAgent } );

	public async Task Process( FormData<JsonElement> data )
	{
		await File.WriteAllTextAsync(
				$"{BaseDir}\\{data.FormName}-{DateTime.Now:yyyyMMdd-HHmmss}.json",
				JsonSerializer.Serialize( data, JsonSerializerOptions.Web )
			);
	}
}
