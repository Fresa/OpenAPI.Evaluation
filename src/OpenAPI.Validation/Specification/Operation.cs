namespace OpenAPI.Validation.Specification;

public sealed partial class Operation
{
    private readonly JsonNodeReader _reader;

    internal Operation(JsonNodeReader reader)
    {
        _reader = reader;
    }

    internal static Operation Parse(JsonNodeReader reader)
    {
        return null!;
    }
}