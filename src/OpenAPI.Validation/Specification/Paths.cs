using System.Diagnostics.CodeAnalysis;
using OpenAPI.Validation.Extensions;

namespace OpenAPI.Validation.Specification;

public sealed partial class Paths
{
    private readonly JsonNodeReader _reader;

    private Paths(JsonNodeReader reader)
    {
        _reader = reader;

        _pathItems = _reader.ReadChildren()
            .ToDictionary(nodeReader => nodeReader.Key, Path.Parse);
    }

    internal static Paths Parse(JsonNodeReader reader)
    {
        return new Paths(reader);
    }

    private readonly Dictionary<string, Path> _pathItems;
    public IReadOnlyDictionary<string, Path> PathItems => _pathItems.AsReadOnly();

    internal Evaluator GetEvaluator(OpenApiEvaluationContext openApiEvaluationContext) => 
        new(openApiEvaluationContext.Evaluate(_reader), this);

    internal sealed class Evaluator
    {
        private readonly OpenApiEvaluationContext _openApiEvaluationContext;
        private readonly Paths _paths;

        internal Evaluator(OpenApiEvaluationContext openApiEvaluationContext, Paths paths)
        {
            _openApiEvaluationContext = openApiEvaluationContext;
            _paths = paths;
        }

        internal bool TryMatch(Uri uri, 
            [NotNullWhen(true)] out Path.Evaluator? pathItemEvaluator)
        {
            pathItemEvaluator = null;
            if (uri.IsAbsoluteUri)
            {
                _openApiEvaluationContext.Results.Fail($"{uri} must be a path relative to the server");
                return false;
            }

            var requestedPathSegments = uri.GetPathSegments();
            foreach (var (pathTemplate, pathItem) in _paths.PathItems)
            {
                var apiPathSegments = pathTemplate.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (apiPathSegments.Length != requestedPathSegments.Length)
                {
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
                    continue;
                }

                var routePattern = new RoutePattern(pathTemplate, routeValues);
                pathItemEvaluator = pathItem.GetEvaluator(_openApiEvaluationContext, routePattern);
                return true;
            }
            
            _openApiEvaluationContext.Results.Fail($"'{string.Join('/', requestedPathSegments)}' does not match any of the paths: {string.Join(", ", _paths.PathItems.Keys)}");
            return pathItemEvaluator != null;
        }
    }
}