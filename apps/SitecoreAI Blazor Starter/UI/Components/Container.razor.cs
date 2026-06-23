using System.Text.RegularExpressions;

namespace BlazorStarter.UI.Components;

public partial class Container
{
	[GeneratedRegex( "/mediaurl=\\\"([^\"]*)\\\"/", RegexOptions.IgnoreCase, "en-US" )]
	private static partial Regex MakeMediaUrlRegex();
	private static readonly Regex RxMediaUrl = MakeMediaUrlRegex();

}
