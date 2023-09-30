using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Json.Schema;
using OpenAPI.Evaluation.Specification;

namespace OpenAPI.Evaluation.ParameterConverters;

internal sealed class SchemaParameterValueConverter : IParameterValueConverter
{
    private readonly JsonSchema _schema;
    private readonly Parameter _parameter;

    public SchemaParameterValueConverter(Parameter parameter, JsonSchema schema)
    {
        _schema = schema;
        _parameter = parameter;
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
                throw new NotImplementedException($"Cannot map {jsonType} to json, please register a parameter value mapper");
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