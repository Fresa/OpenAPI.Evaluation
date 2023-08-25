using System.Net.Http.Headers;

namespace OpenAPI.Validation.Specification;

public sealed class Response
{
    private readonly JsonNodeReader _reader;

    private Response(JsonNodeReader reader)
    {
        _reader = reader;
        if (_reader.TryRead("headers", out var headersReader))
        {
            Headers = ResponseHeaders.Parse(headersReader);
        }
    }

    internal static Response Parse(JsonNodeReader responsesReader) => new(responsesReader);

    public ResponseHeaders? Headers { get; }

    internal Evaluator GetEvaluator(OpenApiEvaluationContext openApiEvaluationContext)
    {
        return new Evaluator(openApiEvaluationContext.Evaluate(_reader), this);
    }

    internal class Evaluator
    {
        private readonly OpenApiEvaluationContext _openApiEvaluationContext;
        private readonly Response _response;

        internal Evaluator(OpenApiEvaluationContext openApiEvaluationContext, Response response)
        {
            _openApiEvaluationContext = openApiEvaluationContext;
            _response = response;
        }

        public void EvaluateHeaders(HttpResponseHeaders headers)
        {
            _response.Headers?.GetEvaluator(_openApiEvaluationContext).EvaluateRequestHeaders(headers);
        }
    }
}