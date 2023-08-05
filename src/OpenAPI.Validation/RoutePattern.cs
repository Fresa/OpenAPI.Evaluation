namespace OpenAPI.Validation;

internal sealed class RoutePattern
{
    public RoutePattern(string template, IReadOnlyDictionary<string, string> values)
    {
        Template = template;
        Values = values;
    }

    public string Template { get; }
    public IReadOnlyDictionary<string, string> Values { get; }
}