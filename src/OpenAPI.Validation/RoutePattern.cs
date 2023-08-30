namespace OpenAPI.Evaluation;

internal sealed record RoutePattern(string Template, IReadOnlyDictionary<string, string> Values);