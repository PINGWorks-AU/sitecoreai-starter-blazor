using Microsoft.AspNetCore.StaticFiles;

namespace Microsoft.AspNetCore.Builder;

public static class WebApplicationExtensions
{
	extension( WebApplication app )
	{
		public WebApplication UseMarkdownStaticFiles()
		{
			var markdownProvider = new FileExtensionContentTypeProvider();
			markdownProvider.Mappings.Clear();
			markdownProvider.Mappings[".md"] = "text/markdown; charset=utf-8";

			app.UseStaticFiles( new StaticFileOptions { ContentTypeProvider = markdownProvider } );

			return app;
		}
	}
}
