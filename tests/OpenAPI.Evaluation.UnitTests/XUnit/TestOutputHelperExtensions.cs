using System.Text.Encodings.Web;
using System.Text.Json;
using Xunit.Abstractions;

namespace OpenAPI.Evaluation.UnitTests.XUnit;

internal static class TestOutputHelperExtensions
{
    private static readonly JsonSerializerOptions EvaluationResultSerializerOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    internal static void WriteEvaluationResult(this ITestOutputHelper testOutputHelper,
        OpenApiEvaluationContext context)
    {
        testOutputHelper.WriteLine(
            JsonSerializer.Serialize(context.Results, EvaluationResultSerializerOptions));
    }
}