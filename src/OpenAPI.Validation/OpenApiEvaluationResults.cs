using System.Text.Json.Serialization;
using Json.Pointer;
using Json.Schema;

namespace OpenAPI.Validation;

[JsonConverter(typeof(OpenApiEvaluationResultsJsonConverter))]
public class OpenApiEvaluationResults
{
    public OpenApiEvaluationResults()
    {
        AllDetails();
    }

    public bool Exclude { get; set; }
    public bool IsValid =>
        (SchemaEvaluationResults?.All(results => results.IsValid) ?? true) &&
        (!Errors?.Any() ?? true) &&
        DetailsIsValid();

    private Func<bool> DetailsIsValid { get; set; }
    internal void AllDetails() => DetailsIsValid = () => (Details?.All(results => results.IsValid) ?? true);
    internal void AnyDetails() => DetailsIsValid = () => (Details?.Any(results => results.IsValid) ?? true);
    internal void OneDetail() => DetailsIsValid = () => Details?.Count(results => results.IsValid) == 1;

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
        var details = new OpenApiEvaluationResults
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
}