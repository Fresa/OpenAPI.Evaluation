using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace OpenAPI.Evaluation.Specification;

public sealed class Responses : IReadOnlyDictionary<int, Response>
{
    private readonly JsonNodeReader _reader;
    private readonly Dictionary<int, Response> _responses = new();

    private Responses(JsonNodeReader reader)
    {
        _reader = reader;
        foreach (var responseReader in _reader.ReadChildren())
        {
            if (responseReader.Key == "default")
            {
                Default = Response.Parse(responseReader);
                continue;
            }

            if (!int.TryParse(responseReader.Key, out var statusCode))
            {
                throw new InvalidOperationException(
                    $"Response object '{responseReader.Key}' is not a http status code nor 'default'");
            }

            _responses.Add(statusCode, Response.Parse(responseReader));
        }
    }

    internal static Responses Parse(JsonNodeReader responsesReader) => new(responsesReader);
    public Response? Default { get; }

    public IEnumerator<KeyValuePair<int, Response>> GetEnumerator() => _responses.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => _responses.Count;
    public bool ContainsKey(int key) => _responses.ContainsKey(key);

    public bool TryGetValue(int key, [MaybeNullWhen(false)] out Response value) => _responses.TryGetValue(key, out value);

    public Response this[int key] => _responses[key];

    public IEnumerable<int> Keys => _responses.Keys;
    public IEnumerable<Response> Values => _responses.Values;

    internal Evaluator GetEvaluator(OpenApiEvaluationContext openApiEvaluationContext)
    {
        return new Evaluator(openApiEvaluationContext.Evaluate(_reader), this);
    }

    internal class Evaluator
    {
        private readonly OpenApiEvaluationContext _openApiEvaluationContext;
        private readonly Responses _responses;

        internal Evaluator(OpenApiEvaluationContext openApiEvaluationContext, Responses responses)
        {
            _openApiEvaluationContext = openApiEvaluationContext;
            _responses = responses;
        }

        internal bool TryMatchResponseContent(int statusCode,
            [NotNullWhen(true)] out Response.Evaluator? responseEvaluator)
        {
            if (!_responses.TryGetValue(statusCode, out var response))
            {
                if (_responses.Default == null)
                {
                    _openApiEvaluationContext.Results.Fail(
                        $"There is no response with status code {statusCode} defined and no default response");
                    responseEvaluator = null;
                    return false;
                }

                response = _responses.Default;
            }

            responseEvaluator = response.GetEvaluator(_openApiEvaluationContext);
            return true;
        }
    }
}