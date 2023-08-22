using System.Text.Json.Nodes;

namespace OpenAPI.Validation.Specification;

public sealed partial class MediaType
{
    private readonly JsonNodeReader _reader;

    private MediaType(JsonNodeReader reader)
    {
        _reader = reader;

        if (_reader.TryRead("schema", out var schemaReader))
        {
            Schema = Schema.Parse(schemaReader);
        }
    }

    internal static MediaType Parse(JsonNodeReader reader)
    {
        return new MediaType(reader);
    }

    public Schema? Schema { get; }

    internal Evaluator GetEvaluator(OpenApiEvaluationContext openApiEvaluationContext) =>
        new(openApiEvaluationContext.Evaluate(_reader), this);
    
    public class Evaluator
    {
        private readonly OpenApiEvaluationContext _openApiEvaluationContext;
        private readonly MediaType _mediaType;

        internal Evaluator(OpenApiEvaluationContext openApiEvaluationContext, MediaType mediaType)
        {
            _openApiEvaluationContext = openApiEvaluationContext;
            _mediaType = mediaType;
        }

        public void EvaluateBody(JsonNode? body)
        {
            _mediaType.Schema?
                .GetEvaluator(_openApiEvaluationContext)
                .Evaluate(body);
        }
    }
}