namespace BlazorStarter.Processors;

public class FormData
{
	public required string FormId { get; init; }
	public required string FormName { get; init; }
	public string? RequestId { get; init; }
	public string? UserAgent { get; set; }
}

public class FormData<T>( object? data ) : FormData
{
	public T? Data { get; } = data is T t ? t : default;
}
