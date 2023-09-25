using Json.Pointer;

namespace OpenAPI.Evaluation;

internal sealed class OpenApiEvaluationContext
{
    private OpenApiEvaluationContext(
        OpenApiEvaluationResults results,
        OpenApiEvaluationOptions evaluationOptions)
    {
        Results = results;
        EvaluationOptions = evaluationOptions;
    }

    public OpenApiEvaluationContext(JsonNodeReader reader, OpenApiEvaluationOptions evaluationOptions)
    {
        EvaluationOptions = evaluationOptions;
        Results = new OpenApiEvaluationResults(evaluationOptions.JsonSchemaEvaluationOptions.PreserveDroppedAnnotations)
        {
            EvaluationPath = reader.Trail,
            SpecificationLocation = new(evaluationOptions.Document.BaseUri, reader.RootPath.ToString(JsonPointerStyle.UriEncoded))
        };
    }

    internal OpenApiEvaluationResults Results { get; }
    internal OpenApiEvaluationOptions EvaluationOptions { get; }

    internal OpenApiEvaluationContext Evaluate(JsonNodeReader reader)
    {
        return new OpenApiEvaluationContext(Results.AddDetailsFrom(reader), EvaluationOptions);
    }
}