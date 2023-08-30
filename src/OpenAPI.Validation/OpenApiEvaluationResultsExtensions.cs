namespace OpenAPI.Evaluation;

public static class OpenApiEvaluationResultsExtensions
{
    private static void IgnoreValidResults(this IEnumerable<OpenApiEvaluationResults> results)
    {
        foreach (var result in results)
        {
            result.IgnoreValidResults();
        }
    }

    public static void IgnoreValidResults(this OpenApiEvaluationResults results)
    {
        if (results.IsValid)
        {
            results.Exclude = true;
            return;
        }

        results.Details?.IgnoreValidResults();
        results.SchemaEvaluationResults?.IgnoreValidResults();
    }
}