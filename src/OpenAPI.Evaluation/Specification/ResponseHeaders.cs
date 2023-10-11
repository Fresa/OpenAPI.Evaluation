using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;

namespace OpenAPI.Evaluation.Specification;

public sealed class ResponseHeaders : IReadOnlyDictionary<string, HeaderParameter>
{
    private readonly JsonNodeReader _reader;
    private readonly Dictionary<string, HeaderParameter> _headers = new();

    private ResponseHeaders(JsonNodeReader reader)
    {
        _reader = reader;
        foreach (var headerReader in reader.ReadChildren())
        {
            if (HeaderParameter.TryParseResponseHeader(headerReader, headerReader.Key, out var header))
            {
                _headers.Add(headerReader.Key, header);
            }
        }
    }

    internal static ResponseHeaders Parse(JsonNodeReader responsesReader) => new(responsesReader);
    public IEnumerator<KeyValuePair<string, HeaderParameter>> GetEnumerator() => _headers.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => _headers.Count;
    public bool ContainsKey(string key) => _headers.ContainsKey(key);

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out HeaderParameter value) => _headers.TryGetValue(key, out value);

    public HeaderParameter this[string key] => _headers[key];

    public IEnumerable<string> Keys => _headers.Keys;
    public IEnumerable<HeaderParameter> Values => _headers.Values;

    internal Evaluator GetEvaluator(OpenApiEvaluationContext openApiEvaluationContext)
    {
        return new Evaluator(openApiEvaluationContext.Evaluate(_reader), this);
    }

    internal class Evaluator
    {
        private readonly OpenApiEvaluationContext _openApiEvaluationContext;
        private readonly ResponseHeaders _headers;

        internal Evaluator(OpenApiEvaluationContext openApiEvaluationContext, ResponseHeaders headers)
        {
            _openApiEvaluationContext = openApiEvaluationContext;
            _headers = headers;
        }

        public void EvaluateRequestHeaders(IDictionary<string, IEnumerable<string>> headers)
        {
            foreach (var parameter in _headers.Values)
            {
                parameter.GetEvaluator(_openApiEvaluationContext).Evaluate(headers);
            }
        }
    }
}