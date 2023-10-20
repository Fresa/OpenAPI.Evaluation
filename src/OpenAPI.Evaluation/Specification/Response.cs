using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using OpenAPI.Evaluation.Collections;
using OpenAPI.Evaluation.Http;

namespace OpenAPI.Evaluation.Specification;

public sealed class Response
{
    private readonly JsonNodeReader _reader;
    private readonly IDictionary<string, JsonNode?> _annotations = new Dictionary<string, JsonNode?>();

    private Response(JsonNodeReader reader)
    {
        _reader = reader;
        
        var descriptionReader = _reader.Read("description");
        _annotations.Add(descriptionReader);
        Description = descriptionReader.GetValue<string>();

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

    public string Description { get; }
    public ResponseHeaders? Headers { get; }
    public Content? Content { get; }

    internal Evaluator GetEvaluator(OpenApiEvaluationContext openApiEvaluationContext)
    {
        var context = openApiEvaluationContext.Evaluate(_reader);
        context.Results.SetAnnotations(_annotations);
        return new Evaluator(context, this);
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

        public void EvaluateHeaders(IDictionary<string, IEnumerable<string>> headers)
        {
            _response.Headers?.GetEvaluator(_openApiEvaluationContext).EvaluateRequestHeaders(headers);
        }

        internal bool TryMatchResponseContent(MediaTypeValue? mediaType,
            [NotNullWhen(true)] out MediaType.Evaluator? mediaTypeEvaluator)
        {
            if (mediaType == null)
            {
                mediaTypeEvaluator = null;
                if (_response.Content == null)
                {
                    return false;
                }

                _openApiEvaluationContext.Results.Fail(
                    "Media type is not specified but there is content defined");
                return false;
            }

            if (_response.Content != null)
            {
                return _response.Content.GetEvaluator(_openApiEvaluationContext)
                    .TryMatch(mediaType, out mediaTypeEvaluator);
            }

            _openApiEvaluationContext.Results.Fail(
                $"Media type '{mediaType}' was requested, but there is no content defined");
            mediaTypeEvaluator = null;
            return false;
        }
    }
}