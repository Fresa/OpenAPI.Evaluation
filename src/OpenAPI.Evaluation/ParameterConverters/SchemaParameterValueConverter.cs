using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Json.Schema;
using OpenAPI.Evaluation.Specification;

namespace OpenAPI.Evaluation.ParameterConverters;

internal sealed class SchemaParameterValueConverter : IParameterValueConverter
{
    private readonly JsonSchema _schema;
    private readonly Parameter _parameter;
    private readonly PropertySchemaResolver _propertySchemaResolver;

    public SchemaParameterValueConverter(Parameter parameter, JsonSchema schema)
    {
        _schema = schema;
        _parameter = parameter;
        _propertySchemaResolver = new PropertySchemaResolver(_schema);
    }

    public string ParameterName => _parameter.Name;
    public string ParameterLocation => _parameter.In;
    public bool TryMap(
        string[] values,
        out JsonNode? instance,
        [NotNullWhen(false)] out string? mappingError)
    {
        var jsonType = _schema.GetJsonType();
        if (jsonType == null)
        {
            mappingError = "Schema does not contain a type attribute";
            instance = null;
            return false;
        }

        switch (jsonType)
        {
            case SchemaValueType.String:
                if (!TryGetSingleOrDefaultValue(out var stringValue, out mappingError))
                {
                    instance = null;
                    return false;
                }

                instance = JsonValue.Create(stringValue);
                return true;
            case SchemaValueType.Number:
            case SchemaValueType.Boolean:
            case SchemaValueType.Integer:
            case SchemaValueType.Null:
                if (!TryGetSingleOrDefaultValue(out var value, out mappingError))
                {
                    instance = null;
                    return false;
                }
                instance = value == null ? null : JsonNode.Parse(value);
                return true;
            case SchemaValueType.Object:
                return TryGetObject(values, out instance, out mappingError);
            case SchemaValueType.Array:
                return TryGetArray(values, out instance, out mappingError);
            default:
                throw new NotSupportedException($"Json type {Enum.GetName(jsonType.Value)} is not supported");
        }

        bool TryGetSingleOrDefaultValue(
            out string? value,
            [NotNullWhen(false)] out string? error)
        {
            if (values.Length > 1)
            {
                error = $"Expected at most one value got {values.Length}";
                value = null;
                return false;
            }

            value = values.FirstOrDefault();
            error = null;
            return true;
        }
    }

    private bool TryGetObject(
        IReadOnlyCollection<string> values,
        [NotNullWhen(true)] out JsonNode? obj,
        [NotNullWhen(false)] out string? error)
    {

        return _parameter.Style switch
        {
            Parameter.Styles.Form => TryGetFormStyleObjectProperties(values, out obj, out error),
            //Parameter.Styles.Label => TryGetLabelStyleArrayItems(itemMapper, values, out array, out error),
            //Parameter.Styles.Matrix => TryGetMatrixStyleArrayItems(itemMapper, values, out array, out error),
            //Parameter.Styles.SpaceDelimited => TryGetSpaceDelimitedStyleArrayItems(itemMapper, values, out array, out error),
            //Parameter.Styles.PipeDelimited => TryGetPipeDelimitedStyleArrayItems(itemMapper, values, out array, out error),
            Parameter.Styles.DeepObject => throw new NotImplementedException(),
            _ => StyleNotSupportedForObject(out obj, out error)
        };
    }

    private bool StyleNotSupportedForObject(
        [NotNullWhen(true)] out JsonNode? array,
        [NotNullWhen(false)] out string? error)
    {
        array = null;
        error = $"Style '{_parameter.Style}' not supported for object";
        return false;
    }

    private bool TryGetFormStyleObjectProperties(
        IReadOnlyCollection<string> values,
        [NotNullWhen(true)] out JsonNode? obj,
        [NotNullWhen(false)] out string? error)
    {
        if (_parameter.Explode)
        {
            error = "form style with explode not supported for objects as the parameter name cannot be determined";
            obj = null;
            return false;
        }

        if (values.Count != 1)
        {
            error = "Expected one value when parameter doesn't specify explode";
            obj = null;
            return false;
        }

        var arrayValues = values.First().Split(',');

        var jsonObject = new JsonObject();
        for (var i = 0; i < arrayValues.Length; i += 2)
        {
            var propertyName = arrayValues[i];
            var propertyValue = arrayValues[i + 1];
            JsonNode? value;
            if (_propertySchemaResolver.TryGetSchemaForProperty(propertyName, out var propertySchema))
            {
                var propertyConverter = new SchemaParameterValueConverter(_parameter, propertySchema);
                if (!propertyConverter.TryMap(new[] { propertyValue }, out value, out error))
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

    private class PropertySchemaResolver
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


    private bool TryGetArray(
        IReadOnlyCollection<string> values,
        [NotNullWhen(true)] out JsonNode? array,
        [NotNullWhen(false)] out string? error)
    {
        var itemsSchema = _schema.GetItems();
        if (itemsSchema == null)
        {
            error = "Missing 'items' keyword for array";
            array = null;
            return false;
        }

        var itemMapper = new SchemaParameterValueConverter(_parameter, itemsSchema);
        return _parameter.Style switch
        {
            Parameter.Styles.Form => TryGetFormStyleArrayItems(itemMapper, values, out array, out error),
            Parameter.Styles.Label => TryGetLabelStyleArrayItems(itemMapper, values, out array, out error),
            Parameter.Styles.Matrix => TryGetMatrixStyleArrayItems(itemMapper, values, out array, out error),
            Parameter.Styles.Simple => TryGetSimpleStyleArrayItems(itemMapper, values, out array, out error),
            Parameter.Styles.SpaceDelimited => TryGetSpaceDelimitedStyleArrayItems(itemMapper, values, out array, out error),
            Parameter.Styles.PipeDelimited => TryGetPipeDelimitedStyleArrayItems(itemMapper, values, out array, out error),
            _ => StyleNotSupportedForArray(out array, out error)
        };
    }

    private bool StyleNotSupportedForArray(
        [NotNullWhen(true)] out JsonNode? array,
        [NotNullWhen(false)] out string? error)
    {
        array = null;
        error = $"Style '{_parameter.Style}' not supported for arrays";
        return false;
    }

    private bool TryGetPipeDelimitedStyleArrayItems(
        IParameterValueConverter itemMapper,
        IReadOnlyCollection<string> values,
        [NotNullWhen(true)] out JsonNode? array,
        [NotNullWhen(false)] out string? error)
    {
        if (!_parameter.Explode)
        {
            return TryGetArrayItems(itemMapper, values.ToArray(), out array, out error);
        }

        if (values.Count != 1)
        {
            error = "Expected one value when parameter doesn't specify explode";
            array = null;
            return false;
        }

        var arrayItems = values.First().Split('|');
        return TryGetArrayItems(itemMapper, arrayItems, out array, out error);

    }

    private bool TryGetSpaceDelimitedStyleArrayItems(
        IParameterValueConverter itemMapper,
        IReadOnlyCollection<string> values,
        [NotNullWhen(true)] out JsonNode? array,
        [NotNullWhen(false)] out string? error)
    {
        if (!_parameter.Explode)
        {
            return TryGetArrayItems(itemMapper, values.ToArray(), out array, out error);
        }

        if (values.Count != 1)
        {
            error = "Expected one value when parameter doesn't specify explode";
            array = null;
            return false;
        }

        var arrayItems = values.First().Split(' ');
        return TryGetArrayItems(itemMapper, arrayItems, out array, out error);

    }

    private bool TryGetFormStyleArrayItems(
        IParameterValueConverter itemMapper,
        IReadOnlyCollection<string> values,
        [NotNullWhen(true)] out JsonNode? array,
        [NotNullWhen(false)] out string? error)
    {
        if (_parameter.Explode)
        {
            return TryGetArrayItems(itemMapper, values.ToArray(), out array, out error);
        }

        if (values.Count != 1)
        {
            error = "Expected one value when parameter doesn't specify explode";
            array = null;
            return false;
        }

        var arrayValues = values.First().Split(',');
        return TryGetArrayItems(itemMapper, arrayValues, out array, out error);
    }

    private bool TryGetLabelStyleArrayItems(
        IParameterValueConverter itemMapper,
        IReadOnlyCollection<string> values,
        [NotNullWhen(true)] out JsonNode? array,
        [NotNullWhen(false)] out string? error)
    {
        if (values.Count != 1)
        {
            error = $"Expected one value when parameter style is '{Parameter.Styles.Label}'";
            array = null;
            return false;
        }
        var arrayValues = values
            .First()
            .Split('.')[1..];
        return TryGetArrayItems(itemMapper, arrayValues, out array, out error);
    }

    private bool TryGetSimpleStyleArrayItems(
        IParameterValueConverter itemMapper,
        IReadOnlyCollection<string> values,
        [NotNullWhen(true)] out JsonNode? array,
        [NotNullWhen(false)] out string? error)
    {
        if (values.Count != 1)
        {
            error = $"Expected one value when parameter style is '{Parameter.Styles.Simple}'";
            array = null;
            return false;
        }

        var arrayValues = values
            .First()
            .Split(',');
        return TryGetArrayItems(itemMapper, arrayValues, out array, out error);
    }

    private bool TryGetMatrixStyleArrayItems(
        IParameterValueConverter itemMapper,
        IReadOnlyCollection<string> values,
        [NotNullWhen(true)] out JsonNode? array,
        [NotNullWhen(false)] out string? error)
    {
        if (values.Count != 1)
        {
            error = $"Expected one value when parameter style is '{Parameter.Styles.Matrix}'";
            array = null;
            return false;
        }

        var arrayValues = values
            .First()
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .SelectMany(expression =>
            {
                var valueAndKey = expression.Split('=');
                var value = valueAndKey.Length == 1 ? string.Empty : valueAndKey.Last();
                return _parameter.Explode ? new[] { value } : value.Split(',');
            })
            .ToArray();
        return TryGetArrayItems(itemMapper, arrayValues, out array, out error);
    }

    private static bool TryGetArrayItems(
        IParameterValueConverter itemMapper,
        IReadOnlyList<string> values,
        [NotNullWhen(true)] out JsonNode? array,
        [NotNullWhen(false)] out string? error)
    {
        var items = new JsonNode?[values.Count];
        for (var index = 0; index < values.Count; index++)
        {
            var arrayValue = values[index];
            if (!itemMapper.TryMap(new[] { arrayValue }, out var item, out error))
            {
                array = null;
                return false;
            }

            items[index] = item;
        }

        error = null;
        array = new JsonArray(items);
        return true;
    }

}