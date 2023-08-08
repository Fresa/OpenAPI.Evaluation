using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Pointer;
using Json.Schema;

namespace OpenAPI.Validation;

internal sealed class OpenApiEvaluationContext
{
    private readonly JsonNodeBaseDocument _document;
    private readonly JsonNodeReader _reader;

    private OpenApiEvaluationContext(JsonNodeBaseDocument document, JsonNodeReader reader, OpenApiEvaluationResults results)
    {
        _document = document;
        _reader = reader;
        Results = results;
    }

    public OpenApiEvaluationContext(JsonNodeBaseDocument document, JsonNodeReader reader)
    {
        _document = document;
        _reader = reader;
        Results = new OpenApiEvaluationResults
        {
            EvaluationPath = reader.Trail,
            SpecificationLocation = new(document.BaseUri, reader.RootPath.ToString(JsonPointerStyle.UriEncoded))
        };
    }

    internal OpenApiEvaluationResults Results { get; }
    
    internal OpenApiEvaluationContext Evaluate(params PointerSegment[] pointerSegments)
    {
        var reader = _reader.Read(pointerSegments);
        return new OpenApiEvaluationContext(_document, reader, Results.AddDetailsFrom(reader));
    }

    internal bool TryEvaluate(PointerSegment pointerSegment, [NotNullWhen(true)] out OpenApiEvaluationContext? context)
    {
        if (_reader.TryRead(JsonPointer.Create(pointerSegment), out var reader))
        {
            var results = Results.AddDetailsFrom(reader);
            context = new OpenApiEvaluationContext(_document, reader, results);
            return true;
        }

        context = null;
        return false;
    }

    internal string GetKey() => _reader.Key;

    internal T GetValue<T>(params PointerSegment[] pointerSegments) => 
        _reader.Read(pointerSegments).GetValue<T>();

    internal bool TryGetValue<T>(PointerSegment pointerSegment, out T? value)
    {
        if (_reader.TryRead(JsonPointer.Create(pointerSegment), out var reader))
        {
            value = reader.GetValue<T>();
            return true;
        }

        value = default;
        return false;
    }

    internal IEnumerable<OpenApiEvaluationContext> EvaluateChildren() =>
        _reader.ReadChildren().Select(reader =>
            new OpenApiEvaluationContext(_document, reader, Results.AddDetailsFrom(reader)));

    internal void Validate(JsonDocument instance, EvaluationOptions evaluationOptions)
    {
        var schema = ResolveSchema(evaluationOptions);
        var result = schema.Evaluate(instance, evaluationOptions);
        Results.Report(result);
    }

    internal void Validate(JsonNode? instance, EvaluationOptions evaluationOptions)
    {
        var schema = ResolveSchema(evaluationOptions);
        var result = schema.Evaluate(instance, evaluationOptions);
        Results.Report(result);
    }

    internal void Validate(IEnumerable<string?> values, EvaluationOptions evaluationOptions)
    {
        var schema = ResolveSchema(evaluationOptions);
        var array = new JsonArray(
            values.Select(value =>
                    JsonValue.Create(value) as JsonNode)
                .ToArray());
        var stringArrayResult = schema.Evaluate(array, evaluationOptions);
        // If there is only one value the schema might describe a primitive type so we fall back on trying to validate the value as such
        if (!stringArrayResult.IsValid &&
            array.Count == 1)
        {
            var stringResult = schema.Evaluate(array.First(), evaluationOptions);
            // We don't know why validation failed, it might not be related to the instance type but other constraints, so we add both evaluation results
            if (!stringResult.IsValid)
            {
                Results.Report(stringArrayResult);
            }
            Results.Report(stringResult);
            return;
        }

        Results.Report(stringArrayResult);
    }
    
    private JsonSchema? _resolvedSchema;
    private JsonSchema ResolveSchema(EvaluationOptions evaluationOptions) =>
        _resolvedSchema ??= _document.FindSubschema(_reader.RootPath, evaluationOptions) ??
                            throw new InvalidOperationException(
                                $"Could not read schema at {_reader.RootPath}, evaluated from {_reader.Trail}");

}