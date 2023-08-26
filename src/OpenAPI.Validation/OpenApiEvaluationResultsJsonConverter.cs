using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenAPI.Validation;

internal class OpenApiEvaluationResultsJsonConverter : JsonConverter<OpenApiEvaluationResults>
{
    public override OpenApiEvaluationResults? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(
        Utf8JsonWriter writer,
        OpenApiEvaluationResults value,
        JsonSerializerOptions options)
    {
        if (value.Exclude)
            return;
        writer.WriteStartObject();
        writer.WriteBoolean("valid", value.IsValid);
        writer.WritePropertyName("evaluationPath");
        JsonSerializer.Serialize(writer, value.EvaluationPath, options);
        writer.WritePropertyName("specificationLocation");
        JsonSerializer.Serialize(writer, value.SpecificationLocation, options);
        if (value.SchemaEvaluationResults != null)
        {
            writer.WritePropertyName("schemaEvaluationResults");
            JsonSerializer.Serialize(writer, value.SchemaEvaluationResults, options);
        }
        if (value.Details != null)
        {
            writer.WritePropertyName("details");
            JsonSerializer.Serialize(writer, value.Details, options);
        }
        if (value.Errors != null)
        {
            writer.WritePropertyName("errors");
            JsonSerializer.Serialize(writer, value.Errors, options);
        }

        if (value.Annotations != null)
        {
            if (value.IsValid)
            {
                writer.WritePropertyName("annotations");
                JsonSerializer.Serialize(writer, value.Annotations, options);
            }
            else if (value.PreserveDroppedAnnotations)
            {
                writer.WritePropertyName("droppedAnnotations");
                JsonSerializer.Serialize(writer, value.Annotations, options);
            }
        }

        writer.WriteEndObject();
    }
}