using System.Text.Json.Nodes;
using Json.Pointer;

namespace OpenAPI.Validation.Specification;

public sealed partial class MediaType
{
    private readonly JsonNodeReader _reader;

    internal MediaType(JsonNodeReader reader)
    {
        _reader = reader;

        if (_reader.TryRead("schema", out var schemaReader))
        {
            Schema = schemaReader.RootPath;
        }
    }

    internal static MediaType Parse(JsonNodeReader reader)
    {
        return new MediaType(reader);
    }

    public JsonPointer? Schema { get; }

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
            // todo
        }
    }
}