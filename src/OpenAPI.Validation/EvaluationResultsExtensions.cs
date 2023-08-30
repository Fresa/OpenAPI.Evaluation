using Json.Schema;

namespace OpenAPI.Evaluation;

internal static class EvaluationResultsExtensions
{
    internal static void IgnoreValidResults(this IEnumerable<EvaluationResults> results)
    {
        foreach (var result in results)
        {
            result.IgnoreValidResults();
        }
    }

    private static void IgnoreValidResults(this EvaluationResults results)
    {
        results.Details.IgnoreValidResults();
        if (results.IsValid)
        {
            // todo: How come this was removed? 
            //results.Ignore();
        }
    }
}