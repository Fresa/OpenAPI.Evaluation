using System.Diagnostics.CodeAnalysis;
using OpenAPI.Evaluation.Http;

namespace OpenAPI.Evaluation.Specification;

public sealed partial class Paths
{
    private readonly JsonNodeReader _reader;

    private Paths(JsonNodeReader reader)
    {
        _reader = reader;

        _pathItems = _reader.ReadChildren()
            .ToDictionary(nodeReader => nodeReader.Key, nodeReader => Path.Parse(nodeReader));
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
            [NotNullWhen(true)] out Path.Evaluator? pathItemEvaluator,
            [NotNullWhen(true)] out Uri? serverUri)
        {
            if (!uri.IsAbsoluteUri)
            {
                throw new ArgumentException($"{uri} must be an absolute uri");
            }

            var requestedPathSegments = uri
                .GetPathSegments();
            var reversedRequestedPathSegments = requestedPathSegments
                .Reverse()
                .ToList();
            foreach (var (pathTemplate, pathItem) in _paths.PathItems)
            {
                var reversedApiPathSegments =
                    pathTemplate
                        .Split('/', StringSplitOptions.RemoveEmptyEntries)
                        .Reverse()
                        .ToList();
                if (reversedApiPathSegments.Count > reversedRequestedPathSegments.Count)
                {
                    continue;
                }

                var match = true;
                var routeValues = new Dictionary<string, string>();
                for (var i = 0; i < reversedApiPathSegments.Count; i++)
                {
                    var segment = reversedApiPathSegments[i];
                    var requestedSegment = reversedRequestedPathSegments[i];
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
                serverUri = new UriBuilder(uri)
                {
                    Path = string.Join('/', requestedPathSegments.Take(reversedRequestedPathSegments.Count - 
                                                                       reversedApiPathSegments.Count))
                }.Uri;
                return true;
            }
            
            _openApiEvaluationContext.Results.Fail($"'{string.Join('/', requestedPathSegments)}' does not match any of the paths: {string.Join(", ", _paths.PathItems.Keys)}");
            pathItemEvaluator = null;
            serverUri = null;
            return false;
        }
    }
}