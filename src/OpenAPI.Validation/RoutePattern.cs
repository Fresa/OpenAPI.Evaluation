namespace OpenAPI.Validation;

internal sealed record RoutePattern(string Template, IReadOnlyDictionary<string, string> Values);