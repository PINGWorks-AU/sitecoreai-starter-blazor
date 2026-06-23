using BlazorStarter.Apis.Webhooks;
using BlazorStarter.UI;
using PINGWorks.SitecoreAI.BlazorSDK.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
	.AddSitecoreBlazorSDK( builder.Configuration )
	.AddStarterKit()
	.AddRazorComponents()
	.AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if ( !app.Environment.IsDevelopment() )
{
	app.UseExceptionHandler( "/status-500", createScopeForErrors: true );
	app.UseHsts();
}

app.UseStatusCodePagesWithReExecute( "/status-{0}", createScopeForStatusCodePages: true );    // Customise the status code pages, like 404 and 500
app.UseHttpsRedirection();
app.UseRequestLocalization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode( opt => opt.ContentSecurityFrameAncestorsPolicy = null )  // Sitecore BlazorSDK will write its own CSP policies
		.AddAdditionalAssemblies( typeof( SitecorePage ).Assembly );                          // Sitecore BlazorSDK adds default route handling

app.UseSitecoreBlazorSDK();                                                                   // Register Sitecore routes and middleware

// Webhook for receiving form data. Implement IFormProcessor to act on submitted data.
app.MapPost( "/api/webhooks/form-data", FormReceiver.ReceiveData ).DisableAntiforgery();

await app.RunAsync();
