namespace OpenAPI.Validation.Http;

public sealed class MediaTypeRange
{
    internal MediaTypeRange(MediaTypeValue value)
    {
        Type = value.Type;
        SubType = value.SubType;
        Parameter = value.Parameter;

        if (Type == "*")
        {
            if (SubType != "*")
                throw new ArgumentException($"{SubType} cannot be a token when type is a range");
        }
        else if (SubType == "*")
        {
            Precedence = 2;
        }
        else
        {
            Precedence = 4;
        }

        if (Parameter != null)
        {
            Precedence++;
        }
    }

    private bool Equals(MediaTypeRange other)
    {
        return Type == other.Type && SubType == other.SubType && Parameter == other.Parameter && Precedence == other.Precedence;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is MediaTypeRange other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Type, SubType, Parameter, Precedence);
    }

    public string Type { get; }
    public string SubType { get; }
    public string? Parameter { get; }
    public ushort Precedence { get; }

    internal bool Matches(MediaTypeValue mediaType)
    {
        if ((Parameter == null && mediaType.Parameter != null) ||
            (Parameter != null && mediaType.Parameter == null))
            return false;

        if (Parameter != null && !Parameter.Equals(mediaType.Parameter, StringComparison.CurrentCultureIgnoreCase))
            return false;

        if (Type == "*")
            return true;
        if (!Type.Equals(mediaType.Type, StringComparison.CurrentCultureIgnoreCase))
            return false;
        if (SubType == "*") 
            return true;
        if (!SubType.Equals(mediaType.SubType, StringComparison.CurrentCultureIgnoreCase))
            return false;

        return true;
    }
}