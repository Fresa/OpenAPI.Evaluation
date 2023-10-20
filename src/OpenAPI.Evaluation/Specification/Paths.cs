using System.Collections;
using System.Diagnostics.CodeAnalysis;
using OpenAPI.Evaluation.Http;

namespace OpenAPI.Evaluation.Specification;

public sealed partial class Paths : IReadOnlyDictionary<string, Path>
{
    private readonly JsonNodeReader _reader;
    private readonly Dictionary<string, Path> _pathItems;

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

    public IEnumerator<KeyValuePair<string, Path>> GetEnumerator() => _pathItems.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public int Count => _pathItems.Count;   
    public bool ContainsKey(string key) => _pathItems.ContainsKey(key);
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out Path value) =>
        _pathItems.TryGetValue(key, out value);
    public Path this[string key] => _pathItems[key];
    public IEnumerable<string> Keys => _pathItems.Keys;
    public IEnumerable<Path> Values => _pathItems.Values;

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
                throw new ArgumentException($"{uri} must be an absolute uri", nameof(uri));
            }

            var requestedPathSegments = uri
                .GetPathSegments();
            var reversedRequestedPathSegments = requestedPathSegments
                .Reverse()
                .ToList();
            foreach (var (pathTemplate, pathItem) in _paths)
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
            
            _openApiEvaluationContext.Results.Fail($"'{string.Join('/', requestedPathSegments)}' does not match any of the paths: {string.Join(", ", _paths.Keys)}");
            pathItemEvaluator = null;
            serverUri = null;
            return false;
        }
    }
}