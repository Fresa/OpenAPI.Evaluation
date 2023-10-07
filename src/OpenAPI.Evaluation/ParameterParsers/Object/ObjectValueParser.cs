using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Json.Schema;
using OpenAPI.Evaluation.ParameterParsers.Primitive;
using OpenAPI.Evaluation.Specification;

namespace OpenAPI.Evaluation.ParameterParsers.Object;

internal abstract class ObjectValueParser : IValueParser
{
    private readonly PropertySchemaResolver _propertySchemaResolver;

    internal bool Explode { get; }
    protected ObjectValueParser(JsonSchema schema, bool explode)
    {
        _propertySchemaResolver = new PropertySchemaResolver(schema);
        Explode = explode;
    }

    internal static ObjectValueParser Create(Parameter parameter, JsonSchema schema) =>
        parameter.Style switch
        {
            Parameter.Styles.Matrix => new MatrixObjectValueParser(parameter.Explode, schema),
            Parameter.Styles.Label => new LabelObjectValueParser(parameter.Explode, schema),
            Parameter.Styles.Form => new FormObjectValueParser(parameter.Explode, schema),
            Parameter.Styles.DeepObject => new DeepObjectValueParser(parameter.Explode, schema),
            _ => throw new ArgumentException(nameof(parameter.Style),
                $"Style '{parameter.Style}' not supported for object")
        };

    public abstract bool TryParse(
        IReadOnlyCollection<string> values,
        [NotNullWhen(true)] out JsonNode? array,
        [NotNullWhen(false)] out string? error);


    protected bool TryGetObjectProperties(
        IReadOnlyList<string> keyAndValues,
        [NotNullWhen(true)] out JsonNode? obj,
        [NotNullWhen(false)] out string? error)
    {
        var jsonObject = new JsonObject();
        for (var i = 0; i < keyAndValues.Count; i += 2)
        {
            var propertyName = keyAndValues[i];
            var propertyValue = keyAndValues.Count == i + 1 ? string.Empty : keyAndValues[i + 1];
            JsonNode? value;
            if (_propertySchemaResolver.TryGetSchemaForProperty(propertyName, out var propertySchema))
            {
                var jsonType = propertySchema.GetJsonType();
                if (jsonType == null)
                {
                    obj = null;
                    error = $"Missing 'type' attribute for object schema property '{propertyName}'";
                    return false;
                }

                if (!PrimitiveJsonConverter.TryConvert(propertyValue, jsonType.Value, out value, out error))
                {
                    obj = null;
                    return false;
                }
            }
            else
            {
                // Undefined type, use string as default as any value should be valid
                value = JsonValue.Create(propertyValue);
            }
            jsonObject[propertyName] = value;
        }

        obj = jsonObject;
        error = null;
        return true;
    }
}