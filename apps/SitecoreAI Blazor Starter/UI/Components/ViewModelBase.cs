using Microsoft.AspNetCore.Components;
using PINGWorks.SitecoreAI.BlazorSDK.Binding;

namespace BlazorStarter.UI.Components;

public abstract class ViewModelBase
{
	[SitecoreComponentParam]
	public MarkupString? Styles { get; set; }

	[SitecoreComponentParam( "FieldNames" )]
	public string? RenderingVariant { get; set; }

	[SitecoreComponentParam]
	public string? GridParameters { get; set; }
}
