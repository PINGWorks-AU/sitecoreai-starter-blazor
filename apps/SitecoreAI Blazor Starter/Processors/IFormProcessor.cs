namespace BlazorStarter.Processors;

public interface IFormProcessor
{
	string FormName { get; }
	string? ApiKey { get; }
	Type ModelType { get; }

	Task Process( string formId, string formName, string requestId, string? userAgent, object? data );
}

public interface IFormProcessor<TModel> : IFormProcessor
{
	Task Process( FormData<TModel> data );
}
