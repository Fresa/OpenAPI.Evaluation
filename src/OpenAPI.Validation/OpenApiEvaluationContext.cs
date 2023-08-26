using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Json.Pointer;
using Json.Schema;

namespace OpenAPI.Validation;

internal sealed class OpenApiEvaluationContext
{
    private readonly JsonNodeBaseDocument _document;
    private readonly JsonNodeReader _reader;
    private readonly EvaluationOptions _evaluationOptions;

    private OpenApiEvaluationContext(JsonNodeBaseDocument document, JsonNodeReader reader, OpenApiEvaluationResults results, EvaluationOptions evaluationOptions)
    {
        _document = document;
        _reader = reader;
        Results = results;
        _evaluationOptions = evaluationOptions;
    }

    public OpenApiEvaluationContext(JsonNodeBaseDocument document, JsonNodeReader reader, EvaluationOptions evaluationOptions)
    {
        _document = document;
        _reader = reader;
        _evaluationOptions = evaluationOptions;
        Results = new OpenApiEvaluationResults
        {
            EvaluationPath = reader.Trail,
            SpecificationLocation = new(document.BaseUri, reader.RootPath.ToString(JsonPointerStyle.UriEncoded))
        };
    }

    internal OpenApiEvaluationResults Results { get; }

    internal OpenApiEvaluationContext Evaluate(JsonNodeReader reader)
    {
        return new OpenApiEvaluationContext(_document, reader, Results.AddDetailsFrom(reader), _evaluationOptions);
    }

    internal OpenApiEvaluationContext Evaluate(params PointerSegment[] pointerSegments)
    {
        var reader = _reader.Read(pointerSegments);
        return new OpenApiEvaluationContext(_document, reader, Results.AddDetailsFrom(reader), _evaluationOptions);
    }

    internal bool TryEvaluate(PointerSegment pointerSegment, [NotNullWhen(true)] out OpenApiEvaluationContext? context)
    {
        if (_reader.TryRead(JsonPointer.Create(pointerSegment), out var reader))
        {
            var results = Results.AddDetailsFrom(reader);
            context = new OpenApiEvaluationContext(_document, reader, results, _evaluationOptions);
            return true;
        }

        context = null;
        return false;
    }

    internal string GetKey() => _reader.Key;

    internal T GetValue<T>(params PointerSegment[] pointerSegments) =>
        pointerSegments.Any() ? _reader.Read(pointerSegments).GetValue<T>() : _reader.GetValue<T>();

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
            new OpenApiEvaluationContext(_document, reader, Results.AddDetailsFrom(reader), _evaluationOptions));
    
    internal void EvaluateAgainstSchema(JsonNode? instance)
    {
        var schema = ResolveSchema();
        var result = schema.Evaluate(instance, _evaluationOptions);
        Results.Report(result);
    }

    internal void EvaluateAgainstSchema(IEnumerable<string?> values)
    {
        var schema = ResolveSchema();
        var array = new JsonArray(
            values.Select(value =>
                    JsonValue.Create(value) as JsonNode)
                .ToArray());
        var stringArrayResult = schema.Evaluate(array, _evaluationOptions);
        // If there is only one value the schema might describe a primitive type so we fall back on trying to validate the value as such
        if (!stringArrayResult.IsValid &&
            array.Count == 1)
        {
            var stringResult = schema.Evaluate(array.First(), _evaluationOptions);
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
    private JsonSchema ResolveSchema() =>
        _resolvedSchema ??= _document.FindSubschema(_reader.RootPath, _evaluationOptions) ??
                            throw new InvalidOperationException(
                                $"Could not read schema at {_reader.RootPath}, evaluated from {_reader.Trail}");

    internal void EvaluateAsRequired(string name)
    {
        Results.Report(
            new JsonSchemaBuilder()
                .Required(name)
                .Evaluate(null, _evaluationOptions));
    }
}