using System.Text.Json.Nodes;
using OpenAPI.Evaluation.Collections;

namespace OpenAPI.Evaluation.Specification;

public sealed partial class MediaType
{
    private readonly JsonNodeReader _reader;
    private readonly IDictionary<string, JsonNode?> _annotations = new Dictionary<string, JsonNode?>();

    private MediaType(JsonNodeReader reader)
    {
        _reader = reader;

        if (_reader.TryRead("schema", out var schemaReader))
        {
            Schema = Schema.Parse(schemaReader);
        }

        if (_reader.TryRead("example", out var exampleReader))
        {
            _annotations.Add(exampleReader);
        }
        if (_reader.TryRead("examples", out var examplesReader))
        {
            _annotations.Add(examplesReader);
        }
    }

    internal static MediaType Parse(JsonNodeReader reader) => new(reader);

    public Schema? Schema { get; }

    internal Evaluator GetEvaluator(OpenApiEvaluationContext openApiEvaluationContext)
    {
        var context = openApiEvaluationContext.Evaluate(_reader);
        context.Results.SetAnnotations(_annotations);
        return new Evaluator(context, this);
    }

    public sealed class Evaluator
    {
        private readonly OpenApiEvaluationContext _openApiEvaluationContext;
        private readonly MediaType _mediaType;

        internal Evaluator(OpenApiEvaluationContext openApiEvaluationContext, MediaType mediaType)
        {
            _openApiEvaluationContext = openApiEvaluationContext;
            _mediaType = mediaType;
        }

        public void Evaluate(JsonNode? body)
        {
            _mediaType.Schema?
                .GetEvaluator(_openApiEvaluationContext)
                .Evaluate(body);
        }
    }
}
