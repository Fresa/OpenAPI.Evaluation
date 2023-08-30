using System.ComponentModel;

namespace OpenAPI.Evaluation.Specification;

public sealed class ServerVariable
{
    private ServerVariable(JsonNodeReader reader)
    {
        Default = reader.Read("default").GetValue<string>();

        if (reader.TryRead("enum", out var enumReader))
        {
            Enum = enumReader
                .ReadChildren()
                .Select(enumValueReader => enumValueReader
                    .GetValue<string>())
                .ToArray();

            if (!Enum.Any())
                throw new ArgumentException("Enum must contain at least one value");
            if (!Enum.Contains(Default))
                throw new InvalidEnumArgumentException("Enum doesn't contain the default value");
        }
        
        if (reader.TryRead("description", out var descriptionReader))
        {
            Description = descriptionReader.GetValue<string>();
        }
    }

    public string[]? Enum { get; }
    public string Default { get; }
    public string? Description { get; }

    internal static ServerVariable Parse(JsonNodeReader reader) => new(reader);
}