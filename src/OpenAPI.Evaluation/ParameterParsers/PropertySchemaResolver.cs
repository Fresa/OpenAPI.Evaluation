using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Json.Schema;

namespace OpenAPI.Evaluation.ParameterParsers;

internal sealed class PropertySchemaResolver
{
    private readonly IReadOnlyDictionary<string, JsonSchema>? _propertySchemas;
    private readonly JsonSchema? _additionalPropertiesSchema;
    private readonly IReadOnlyDictionary<Regex, JsonSchema>? _patternPropertySchemas;

    public PropertySchemaResolver(JsonSchema schema)
    {
        _propertySchemas = schema.GetProperties();
        _additionalPropertiesSchema = schema.GetAdditionalProperties();
        _patternPropertySchemas = schema.GetPatternProperties();

    }

    public bool TryGetSchemaForProperty(string propertyName, [NotNullWhen(true)] out JsonSchema? schema)
    {
        if (_propertySchemas?.TryGetValue(propertyName, out schema) ?? false)
            return true;

        if (_patternPropertySchemas != null)
        {
            foreach (var (pattern, patternSchema) in _patternPropertySchemas)
            {
                if (pattern.Match(propertyName).Success)
                {
                    schema = patternSchema;
                    return true;
                }
            }
        }

        if (_additionalPropertiesSchema != null)
        {
            schema = _additionalPropertiesSchema;
            return true;
        }

        schema = null;
        return false;
    }
}