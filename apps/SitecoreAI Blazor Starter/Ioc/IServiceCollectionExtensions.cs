#pragma warning disable IDE0130 // Namespace does not match folder structure
using BlazorStarter.Processors;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class IServiceCollectionExtensions
{
	extension( IServiceCollection services )
	{
		public IServiceCollection AddStarterKit()
		{
			// Example form processors.
			// Each one handles submissions for a specific FormName + ApiKey (or "*" / null to accept any).
			// Add your own IFormProcessor implementations alongside these to react to form submissions.
			services
				.AddSingleton<IFormProcessor, SaveDataFormProcessor>()
				.AddSingleton<IFormProcessor, SendToTeamsFormProcessor>()
				.AddSingleton<IFormProcessor, SendToEmailFormProcessor>();

			// SendToTeamsFormProcessor uses a typed HttpClient to post Adaptive Cards to a Teams webhook.
			services.AddHttpClient<SendToTeamsFormProcessor>();

			// Options binding for the example processors. Set the corresponding "Teams" / "Email" sections
			// in appsettings.json (or, preferably, in User Secrets for any sensitive values).
			services.AddOptions<TeamsWebhookOptions>()
					.BindConfiguration( "Teams" );

			services.AddOptions<EmailProcessorOptions>()
					.BindConfiguration( "Email" );

			return services;
		}
	}
}
