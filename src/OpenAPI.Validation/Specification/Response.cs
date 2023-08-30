using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using OpenAPI.Evaluation.Http;

namespace OpenAPI.Evaluation.Specification;

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

        if (_reader.TryRead("content", out var contentReader))
        {
            Content = Content.Parse(contentReader);
        }
    }

    internal static Response Parse(JsonNodeReader responsesReader) => new(responsesReader);

    public ResponseHeaders? Headers { get; }
    public Content? Content { get; }

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

        internal bool TryMatchResponseContent(MediaTypeValue mediaType,
            [NotNullWhen(true)] out MediaType.Evaluator? mediaTypeEvaluator)
        {
            if (_response.Content != null)
            {
                return _response.Content.GetEvaluator(_openApiEvaluationContext)
                    .TryMatch(mediaType, out mediaTypeEvaluator);
            }

            _openApiEvaluationContext.Results.Fail("There is no response content defined");
            mediaTypeEvaluator = null;
            return false;
        }
    }
}