#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.AspNetCore.Components;

public static class MarkupStringExtensions
{
	extension( MarkupString ms )
	{
		public bool IsNotEmpty => !string.IsNullOrWhiteSpace( ms.Value );
	}
}
