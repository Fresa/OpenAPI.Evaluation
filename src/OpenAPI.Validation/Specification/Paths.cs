using System.Diagnostics.CodeAnalysis;

namespace OpenAPI.Validation.Specification;

public sealed partial class Paths
{
    private readonly JsonNodeReader _reader;

    private Paths(JsonNodeReader reader)
    {
        _reader = reader;

        _pathItems = _reader.ReadChildren()
            .ToDictionary(nodeReader => nodeReader.Key, PathItem.Parse);
    }

    internal static Paths Parse(JsonNodeReader reader)
    {
        return new Paths(reader);
    }

    private readonly Dictionary<string, PathItem> _pathItems;
    public IReadOnlyDictionary<string, PathItem> PathItems => _pathItems.AsReadOnly();

    internal Evaluator GetEvaluator(OpenApiEvaluationContext openApiEvaluationContext) => 
        new(openApiEvaluationContext.Evaluate(_reader), this);

    internal sealed class Evaluator
    {
        private readonly OpenApiEvaluationContext _openApiEvaluationContext;
        private readonly Paths _paths;

        internal Evaluator(OpenApiEvaluationContext openApiEvaluationContext, Paths paths)
        {
            _openApiEvaluationContext = openApiEvaluationContext;
            _openApiEvaluationContext.Results.OneDetail();

            _paths = paths;
        }

        internal bool TryMatch(Uri uri, 
            [NotNullWhen(true)] out PathItem.Evaluator? pathItemEvaluator)
        {
            pathItemEvaluator = null;
            if (uri.IsAbsoluteUri)
            {
                _openApiEvaluationContext.Results.Fail($"{uri} must be a path relative to the server");
                return false;
            }

            var requestedPathSegments = uri.OriginalString.Split('/', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pathEvaluationContext in _openApiEvaluationContext.EvaluateChildren())
            {
                var pathTemplate = pathEvaluationContext.GetKey();

                var apiPathSegments = pathTemplate.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (apiPathSegments.Length != requestedPathSegments.Length)
                {
                    DoesNotMatchPath();
                    continue;
                }

                var match = true;
                var routeValues = new Dictionary<string, string>();
                for (var i = 0; i < apiPathSegments.Length; i++)
                {
                    var segment = apiPathSegments[i];
                    var requestedSegment = requestedPathSegments[i];
                    if (segment.StartsWith('{') && segment.EndsWith('}'))
                    {
                        routeValues.Add(segment.TrimStart('{').TrimEnd('}'), requestedSegment);
                        continue;
                    }

                    if (segment.Equals(requestedSegment, StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    match = false;
                    break;
                }

                if (!match)
                {
                    DoesNotMatchPath();
                    continue;
                }

                var routePattern = new RoutePattern(pathTemplate, routeValues);
                var pathItem = _paths.PathItems[pathTemplate];
                pathItemEvaluator = pathItem.GetEvaluator(pathEvaluationContext, routePattern);

                void DoesNotMatchPath() => pathEvaluationContext.Results.Fail($"'{string.Join('/', requestedPathSegments)}' does not match '{string.Join('/', apiPathSegments)}'");
            }

            return pathItemEvaluator != null;
        }
    }
}