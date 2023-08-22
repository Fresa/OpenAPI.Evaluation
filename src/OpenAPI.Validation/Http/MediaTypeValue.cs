namespace OpenAPI.Validation.Http;

public sealed record MediaTypeValue
{
    private MediaTypeValue(string type, string subType, string? parameter) 
    {
        Type = type;
        SubType = subType;
        Parameter = parameter;
    }

    internal static MediaTypeValue Parse(string value)
    {
        var subTypeIndex = value.IndexOf('/');
        if (subTypeIndex == -1)
            throw new ArgumentException($"{value} is not properly formatted");

        var type = value[..subTypeIndex];
        if (type.Length < 1)
            throw new ArgumentException("type is to short");

        var rest = value[(subTypeIndex + 1)..];
        var parameterIndex = rest.IndexOf(';');
        string? parameter = null;
        if (parameterIndex != -1)
        {
            parameter = rest[(parameterIndex + 1)..].Trim();
            if (parameter.IndexOf('=') == -1)
                throw new ArgumentException($"{parameter} is missing '='");
        }
        var subType = rest[..parameterIndex].Trim();
        if (subType.Length < 1)
            throw new ArgumentException("sub type is to short");
       
        return new MediaTypeValue(type, subType, parameter);
    }

    public override string ToString() => 
        $"{Type}/{SubType}{(Parameter == null ? "" : $"; {Parameter}")}";

    public bool Equals(MediaTypeValue? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Type == other.Type && SubType == other.SubType && Parameter == other.Parameter;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Type, SubType, Parameter);
    }

    public string Type { get; }
    public string SubType { get; }
    public string? Parameter { get; }
}