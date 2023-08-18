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

    internal class Evaluator
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
            [NotNullWhen(true)] out PathItem? pathItem,
            [NotNullWhen(true)] out RoutePattern? routePattern)
        {
            pathItem = null;
            routePattern = null;

            var path = uri.AbsolutePath;
            var requestedPathSegments = uri.Segments;

            foreach (var pathEvaluationContext in _openApiEvaluationContext.EvaluateChildren())
            {
                var pathTemplate = pathEvaluationContext.GetKey();

                var apiPathSegments = pathTemplate.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (apiPathSegments.Length != requestedPathSegments.Length)
                {
                    pathEvaluationContext.Results.Fail(DoesNotMatchPathErrorMessage());
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
                    pathEvaluationContext.Results.Fail(DoesNotMatchPathErrorMessage());
                    continue;
                }

                // todo
                routePattern = new RoutePattern(pathTemplate, routeValues);
                pathItem = _paths.PathItems[pathTemplate];

                string DoesNotMatchPathErrorMessage() => $"'{path}' does not match '{pathTemplate}'";
            }

            return pathItem != null;
        }
    }
}