using System.Text.Json;
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
    [MemberData(nameof(String))]
    [MemberData(nameof(Number))]
    [MemberData(nameof(Integer))]
    [MemberData(nameof(Boolean))]
    [MemberData(nameof(Null))]
    [MemberData(nameof(Empty))]
    [MemberData(nameof(ArrayLabel))]
    [MemberData(nameof(ArrayForm))]
    [MemberData(nameof(ArrayMatrix))]
    [MemberData(nameof(ArraySimple))]
    [MemberData(nameof(ArraySpaceDelimited))]
    [MemberData(nameof(ArrayPipeDelimited))]
    [MemberData(nameof(ObjectForm))]
    [MemberData(nameof(ObjectMatrix))]
    public void Given_a_parameter_with_schema_When_mapping_values_It_should_map_the_value_to_proper_json(
        string parameterJson,
        string[] values,
        bool shouldMap,
        string? jsonInstance)
    {
        var parameterJsonNode = JsonNode.Parse(parameterJson)!;
        var reader = new JsonNodeReader(parameterJsonNode, JsonPointer.Empty);
        Parameter.TryParse(reader, out var parameter).Should().BeTrue();
        var schema = parameterJsonNode["schema"].Deserialize<JsonSchema>();
        var converter = new SchemaParameterValueConverter(parameter!, schema!);
        converter.TryMap(values, out var instance, out var mappingError).Should().Be(shouldMap, mappingError);
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

    #region Object
    public static TheoryData<string, string[], bool, string?> ObjectMatrix => new()
    {
        {
            """
            {
                "name": "test",
                "in": "path",
                "schema": {
                    "type": "object",
                    "items": {
                        "type": "string"
                    },
                    "properties": {
                        "R": {
                            "type": "number"
                        },
                        "G": {
                            "type": "number"
                        },
                        "B": {
                            "type": "number"
                        }                        
                    }
                },
                "required": true,
                "style": "matrix",
                "explode": true
            }
            """,
            new[] { ";R=100;G=200;B=150" },
            true,
            """{"R":100,"G":200,"B":150}"""
        },
        {
            """
            {
                "name": "test",
                "in": "path",
                "schema": {
                    "type": "object",
                    "additionalProperties": { 
                        "type": "string" 
                    }            
                },
                "required": true,
                "style": "matrix",
                "explode": true
            }
            """,
            new[] { ";R=100;G=200;B" },
            true,
            """{"R":"100","G":"200","B":""}"""
        },
        {
            """
            {
                "name": "color",
                "in": "path",
                "schema": {
                    "type": "object",
                    "items": {
                        "type": "string"
                    },
                    "properties": {
                        "R": {
                            "type": "number"
                        },
                        "G": {
                            "type": "number"
                        },
                        "B": {
                            "type": "number"
                        }                        
                    }
                },
                "required": true,
                "style": "matrix",
                "explode": false
            }
            """,
            new[] { ";color=R,100,G,200,B,150" },
            true,
            """{"R":100,"G":200,"B":150}"""
        }
    };
    public static TheoryData<string, string[], bool, string?> ObjectForm => new()
    {
        {
            """
            {
                "name": "color",
                "in": "query",
                "schema": {
                    "type": "object",
                    "items": {
                        "type": "string"
                    },
                    "properties": {
                        "R": {
                            "type": "number"
                        },
                        "G": {
                            "type": "number"
                        },
                        "B": {
                            "type": "number"
                        }                        
                    }
                },
                "style": "form",
                "explode": false
            }
            """,
            new[] { "R,100,G,200,B,150" },
            true,
            """{"R":100,"G":200,"B":150}"""
        },
        {
            """
            {
                "name": "color",
                "in": "query",
                "schema": {
                    "type": "object",
                    "properties": {
                        "R": {
                            "type": "string"
                        },
                        "G": {
                            "type": "string"
                        },
                        "B": {
                            "type": "string"
                        }                        
                    }
                },
                "style": "form",
                "explode": false
            }
            """,
            new[] { "R,100,G,200,B," },
            true,
            """{"R":"100","G":"200","B":""}"""
        },
        {
            """
            {
                "name": "color",
                "in": "query",
                "schema": {
                    "type": "object",
                    "additionalProperties": { 
                        "type": "string" 
                    }
                },
                "style": "form",
                "explode": false
            }
            """,
            new[] { "R,100,G,200,B," },
            true,
            """{"R":"100","G":"200","B":""}"""
        },
        {
            """
            {
                "name": "color",
                "in": "query",
                "schema": {
                    "type": "object",
                    "patternProperties": {
                        "^R": { 
                            "type": "number" 
                        },
                        "^G": {
                            "type": "integer" 
                        }
                    },            
                    "additionalProperties": { 
                        "type": "string" 
                    }
                },
                "style": "form",
                "explode": false
            }
            """,
            new[] { "R,100,G,200,B," },
            true,
            """{"R":100,"G":200,"B":""}"""
        }
    };
    #endregion

    #region Array
    public static TheoryData<string, string[], bool, string?> ArrayLabel => new()
    {
        {
            """
            {
                "name": "test",
                "in": "path",
                "schema": {
                    "type": "array",
                    "items": {
                        "type": "string"
                    }
                },
                "required": true,
                "style": "label",
                "explode": true
            }
            """,
            new[] { ".test.test2" },
            true,
            "[\"test\",\"test2\"]"
        },
        {
            """
            {
                "name": "test",
                "in": "path",
                "schema": {
                    "type": "array",
                    "items": {
                        "type": "string"
                    }
                },
                "required": true,
                "style": "label",
                "explode": false
            }
            """,
            new[] { ".test.test2" },
            true,
            "[\"test\",\"test2\"]"
        },
        {
            """
            {
                "name": "test",
                "in": "path",
                "schema": {
                    "type": "array",
                    "items": {
                        "type": "string"
                    }
                },
                "required": true,
                "style": "label",
                "explode": false
            }
            """,
            new[] { ".test." },
            true,
            "[\"test\",\"\"]"
        }
    };

    public static TheoryData<string, string[], bool, string?> ArrayForm => new()
    {
        {
            """
            {
                "name": "test",
                "in": "query",
                "schema": {
                    "type": "array",
                    "items": {
                        "type": "string"
                    }
                },
                "style": "form",
                "explode": true
            }
            """,
            new[] { "test", "test2" },
            true,
            "[\"test\",\"test2\"]"
        },
        {
            """
            {
                "name": "test",
                "in": "query",
                "schema": {
                    "type": "array",
                    "items": {
                        "type": "string"
                    }
                },
                "style": "form",
                "explode": false
            }
            """,
            new[] { "test,test2" },
            true,
            "[\"test\",\"test2\"]"
        }
    };

    public static TheoryData<string, string[], bool, string?> ArrayMatrix => new()
    {
        {
            """
            {
                "name": "test",
                "in": "path",
                "schema": {
                    "type": "array",
                    "items": {
                        "type": "string"
                    }
                },
                "required": true,
                "style": "matrix",
                "explode": true
            }
            """,
            new[] { ";test=test;test=test2" },
            true,
            "[\"test\",\"test2\"]"
        },
        {
            """
            {
                "name": "test",
                "in": "path",
                "schema": {
                    "type": "array",
                    "items": {
                        "type": "string"
                    }
                },
                "required": true,
                "style": "matrix",
                "explode": true
            }
            """,
            new[] { ";test=test;test" },
            true,
            "[\"test\",\"\"]"
        },
        {
            """
            {
                "name": "test",
                "in": "path",
                "schema": {
                    "type": "array",
                    "items": {
                        "type": "string"
                    }
                },
                "required": true,
                "style": "matrix",
                "explode": false
            }
            """,
            new[] { ";test=test,test2" },
            true,
            "[\"test\",\"test2\"]"
        }
    };

    public static TheoryData<string, string[], bool, string?> ArraySimple => new()
    {
        {
            """
            {
                "name": "test",
                "in": "path",
                "schema": {
                    "type": "array",
                    "items": {
                        "type": "string"
                    }
                },
                "required": true,
                "style": "simple",
                "explode": true
            }
            """,
            new[] { "test,test2" },
            true,
            "[\"test\",\"test2\"]"
        },
        {
            """
            {
                "name": "test",
                "in": "path",
                "schema": {
                    "type": "array",
                    "items": {
                        "type": "string"
                    }
                },
                "required": true,
                "style": "simple",
                "explode": true
            }
            """,
            new[] { "test," },
            true,
            "[\"test\",\"\"]"
        },
        {
            """
            {
                "name": "test",
                "in": "path",
                "schema": {
                    "type": "array",
                    "items": {
                        "type": "string"
                    }
                },
                "required": true,
                "style": "simple",
                "explode": false
            }
            """,
            new[] { "test,test2" },
            true,
            "[\"test\",\"test2\"]"
        }
    };

    public static TheoryData<string, string[], bool, string?> ArraySpaceDelimited => new()
    {
        {
            """
            {
                "name": "test",
                "in": "query",
                "schema": {
                    "type": "array",
                    "items": {
                        "type": "string"
                    }
                },
                "style": "spaceDelimited",
                "explode": true
            }
            """,
            new[] { "test test2" },
            true,
            "[\"test\",\"test2\"]"
        },
        {
            """
            {
                "name": "test",
                "in": "query",
                "schema": {
                    "type": "array",
                    "items": {
                        "type": "string"
                    }
                },
                "style": "spaceDelimited",
                "explode": true
            }
            """,
            new[] { "test " },
            true,
            "[\"test\",\"\"]"
        },
        {
            """
            {
                "name": "test",
                "in": "query",
                "schema": {
                    "type": "array",
                    "items": {
                        "type": "string"
                    }
                },
                "style": "spaceDelimited",
                "explode": false
            }
            """,
            new[] { "test", "test2" },
            true,
            "[\"test\",\"test2\"]"
        }
    };

    public static TheoryData<string, string[], bool, string?> ArrayPipeDelimited => new()
    {
        {
            """
            {
                "name": "test",
                "in": "query",
                "schema": {
                    "type": "array",
                    "items": {
                        "type": "string"
                    }
                },
                "style": "pipeDelimited",
                "explode": true
            }
            """,
            new[] { "test|test2" },
            true,
            "[\"test\",\"test2\"]"
        },
        {
            """
            {
                "name": "test",
                "in": "query",
                "schema": {
                    "type": "array",
                    "items": {
                        "type": "string"
                    }
                },
                "style": "pipeDelimited",
                "explode": true
            }
            """,
            new[] { "test|" },
            true,
            "[\"test\",\"\"]"
        },
        {
            """
            {
                "name": "test",
                "in": "query",
                "schema": {
                    "type": "array",
                    "items": {
                        "type": "string"
                    }
                },
                "style": "pipeDelimited",
                "explode": false
            }
            """,
            new[] { "test", "test2" },
            true,
            "[\"test\",\"test2\"]"
        }
    };
    #endregion

    public static TheoryData<string, string[], bool, string?> String => new()
    {
        {
            """
            {
                "name": "test",
                "in": "query",
                "schema": {
                    "type": "string" 
                }
            }
            """,
            new[] { "test" },
            true,
            "\"test\""
        },
        {
            """
            {
                "name": "test",
                "in": "query",
                "schema": {
                    "type": "string" 
                }
            }
            """,
            new[] { "test", "test2" },
            false,
            null
        }
    };

    public static TheoryData<string, string[], bool, string?> Number => new()
    {
        {
            """
            {
                "name": "test",
                "in": "query",
                "schema": {
                    "type": "number" 
                }
            }
            """,
            new[] { "1.2" },
            true,
            "1.2"
        },
        {
            """
            {
                "name": "test",
                "in": "query",
                "schema": {
                    "type": "number" 
                }
            }
            """,
            new[] { "1.2", "1.3" },
            false,
            null
        }
    };

    public static TheoryData<string, string[], bool, string?> Integer => new()
    {
        {
            """
            {
                "name": "test",
                "in": "query",
                "schema": {
                    "type": "integer" 
                }
            }
            """,
            new[] { "1" },
            true,
            "1"
        }
    };

    public static TheoryData<string, string[], bool, string?> Boolean => new()
    {
        {
            """
            {
                "name": "test",
                "in": "query",
                "schema": {
                    "type": "boolean" 
                }
            }
            """,
            new[] { "true" },
            true,
            "true"
        }
    };

    public static TheoryData<string, string[], bool, string?> Null => new()
    {
        {
            """
            {
                "name": "test",
                "in": "query",
                "schema": {
                    "type": "null" 
                }
            }
            """,
            System.Array.Empty<string>(),
            true,
            null
        }
    };

    public static TheoryData<string, string[], bool, string?> Empty => new()
    {
        {
            """
            {
                "name": "test",
                "in": "query",
                "schema": {
                }
            }
            """,
            new[] { "test" },
            false,
            null
        }
    };
}
