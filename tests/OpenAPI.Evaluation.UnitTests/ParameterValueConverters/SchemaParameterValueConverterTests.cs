using System.Text.Json.Nodes;
using FluentAssertions;
using Json.Pointer;
using Json.Schema;
using OpenAPI.Evaluation.ParameterConverters;
using OpenAPI.Evaluation.Specification;

namespace OpenAPI.Evaluation.UnitTests.ParameterValueConverters;

public class SchemaParameterValueConverterTests
{
    [Theory]
    [InlineData("""
        {
            "type": "string" 
        }
        """,
        new[] { "test" },
        true,
        "\"test\"")]
    [InlineData("""
        {
            "type": "string" 
        }
        """,
        new[] { "test", "test2" },
        false,
        null)]
    [InlineData("""
        {
            "type": "number" 
        }
        """,
        new[] { "1.2" },
        true,
        "1.2")]
    [InlineData("""
        {
            "type": "number" 
        }
        """,
        new[] { "1.2", "1.3" },
        false,
        null)]
    [InlineData("""
        {
            "type": "integer" 
        }
        """,
        new[] { "1" },
        true,
        "1")]
    [InlineData("""
        {
            "type": "boolean" 
        }
        """,
        new[] { "true" },
        true,
        "true")]
    [InlineData("""
        {
            "type": "null" 
        }
        """,
        new string[0],
        true,
        null)]
    [InlineData("""
        {
        }
        """,
        new[] { "test" },
        false,
        null)]
    public void Given_a_schema_When_mapping_values_It_should_map_the_value_to_proper_json(string jsonSchema, string[] values, bool shouldMap, string? jsonInstance)
    {
        var parameterJsonNode = JsonNode.Parse(
            """
            {
                "name": "test",
                "in": "query"
            }
            """)!;
        parameterJsonNode["schema"] = JsonNode.Parse(jsonSchema);
        var reader = new JsonNodeReader(parameterJsonNode, JsonPointer.Empty);
        var parameter = QueryParameter.Parse(reader);
        var schema = JsonSchema.FromText(jsonSchema);
        var converter = new SchemaParameterValueConverter(parameter, schema);
        converter.TryMap(values, out var instance, out var mappingError).Should().Be(shouldMap);
        if (!shouldMap)
        {
            mappingError.Should().NotBeNullOrEmpty();
            return;
        }

        if (instance is null)
        {
            jsonInstance.Should().BeNull();
        }
        else
        {
            jsonInstance.Should().NotBeNullOrEmpty();
            instance.ToJsonString().Should().BeEquivalentTo(jsonInstance);
        }
    }
}
