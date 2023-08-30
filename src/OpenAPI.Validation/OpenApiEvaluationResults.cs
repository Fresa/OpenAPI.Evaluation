using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.Pointer;
using Json.Schema;

namespace OpenAPI.Evaluation;

[JsonConverter(typeof(OpenApiEvaluationResultsJsonConverter))]
public class OpenApiEvaluationResults
{
    internal OpenApiEvaluationResults(bool preserveDroppedAnnotations)
    {
        IsValidWhenAllDetailsAreValid();
        PreserveDroppedAnnotations = preserveDroppedAnnotations;
    }

    public bool Exclude { get; set; }
    public bool IsValid =>
        (SchemaEvaluationResults?.All(results => results.IsValid) ?? true) &&
        (!Errors?.Any() ?? true) &&
        DetailsIsValid();

    private Func<bool> DetailsIsValid { get; set; }

    [MemberNotNull(nameof(DetailsIsValid))]
    internal void IsValidWhenAllDetailsAreValid() => DetailsIsValid = () => (Details?.All(results => results.IsValid) ?? true);
    [MemberNotNull(nameof(DetailsIsValid))]
    internal void IsValidWhenAnyDetailsAreValid() => DetailsIsValid = () => (Details?.Any(results => results.IsValid) ?? true);
    [MemberNotNull(nameof(DetailsIsValid))]
    internal void IsValidWhenExactlyOneDetailIsValid() => DetailsIsValid = () => Details?.Count(results => results.IsValid) == 1;

    public required JsonPointer EvaluationPath { get; init; }
    public required Uri SpecificationLocation { get; init; }

    private List<OpenApiEvaluationResults>? _details;
    public IReadOnlyList<OpenApiEvaluationResults>? Details => _details?.AsReadOnly();
    private List<string>? _errors;
    public IReadOnlyList<string>? Errors => _errors?.AsReadOnly();
    private List<EvaluationResults>? _schemaEvaluationResults;
    public IReadOnlyList<EvaluationResults>? SchemaEvaluationResults => _schemaEvaluationResults?.AsReadOnly();
    internal OpenApiEvaluationResults AddDetailsFrom(JsonNodeReader result)
    {
        var details = new OpenApiEvaluationResults(PreserveDroppedAnnotations)
        {
            SpecificationLocation =
                new Uri(SpecificationLocation, result.RootPath.ToString(JsonPointerStyle.UriEncoded)),
            EvaluationPath = result.Trail
        };
        _details ??= new List<OpenApiEvaluationResults>();
        _details.Add(details);
        return details;
    }

    internal void Fail(string error)
    {
        _errors ??= new List<string>();
        if (!_errors.Contains(error))
            _errors.Add(error);
    }

    internal void Report(EvaluationResults result)
    {
        _schemaEvaluationResults ??= new List<EvaluationResults>();
        _schemaEvaluationResults.Add(result);
    }

    private Dictionary<string, JsonNode?>? _annotations;
    public IReadOnlyDictionary<string, JsonNode?>? Annotations => _annotations?.AsReadOnly();
    internal bool PreserveDroppedAnnotations { get; }

    internal void SetAnnotations(IReadOnlyDictionary<string, JsonNode?> annotations)
    {
        _annotations ??= new Dictionary<string, JsonNode?>();
        foreach (var annotation in annotations)
        {
            _annotations[annotation.Key] = annotation.Value;
        }
    }
}