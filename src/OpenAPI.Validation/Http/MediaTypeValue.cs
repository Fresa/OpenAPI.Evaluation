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
            throw new ArgumentException("type is too short");

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
            throw new ArgumentException("sub type is too short");
       
        return new MediaTypeValue(type, subType, parameter);
    }

    public override string ToString() => 
        $"{Type}/{SubType}{(Parameter == null ? "" : $"; {Parameter}")}";

    private static readonly StringComparer StringComparer = StringComparer.InvariantCultureIgnoreCase;
    private static readonly StringComparison StringComparison = StringComparison.InvariantCultureIgnoreCase;
    public bool Equals(MediaTypeValue? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Type.Equals(other.Type, StringComparison) && SubType.Equals(other.SubType, StringComparison) &&
               (Parameter?.Equals(other.Parameter, StringComparison) ?? other.Parameter == null);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Type, StringComparer);
        hash.Add(SubType, StringComparer);
        hash.Add(Parameter, StringComparer);
        return hash.ToHashCode();
    }

    public string Type { get; }
    public string SubType { get; }
    public string? Parameter { get; }
}