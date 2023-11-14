using System.Text.Json.Nodes;
using Json.Schema;

namespace OpenAPI.Evaluation.Specification;

public sealed partial class Schema
{
    private readonly JsonNodeReader _reader;

    private Schema(JsonNodeReader reader)
    {
        _reader = reader;
    }

    internal static Schema Parse(JsonNodeReader reader)
    {
        return new Schema(reader);
    }

    internal Evaluator GetEvaluator(OpenApiEvaluationContext openApiEvaluationContext) =>
        new(openApiEvaluationContext.Evaluate(_reader), _reader);

    public class Evaluator
    {
        private readonly OpenApiEvaluationContext _openApiEvaluationContext;
        private readonly JsonNodeReader _schemaReader;
        private JsonSchema? _resolvedSchema;

        internal Evaluator(OpenApiEvaluationContext openApiEvaluationContext, JsonNodeReader schemaReader)
        {
            _openApiEvaluationContext = openApiEvaluationContext;
            _schemaReader = schemaReader;
        }

        internal JsonSchema ResolveSchema() =>
            _resolvedSchema ??= _openApiEvaluationContext.EvaluationOptions.Document.FindSubschema(
                                    _schemaReader.RootPath, 
                                    _openApiEvaluationContext.EvaluationOptions.JsonSchemaEvaluationOptions) ??
                                throw new InvalidOperationException(
                                    $"Could not read schema at {_schemaReader.RootPath}, evaluated from {_schemaReader.Trail}");
        
        public void Evaluate(JsonNode? instance)
        {
            _openApiEvaluationContext.Results.Report(
                ResolveSchema()
                    .Evaluate(instance, _openApiEvaluationContext.EvaluationOptions.JsonSchemaEvaluationOptions));
        }
    }
}