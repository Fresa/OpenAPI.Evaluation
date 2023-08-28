namespace OpenAPI.Validation.Specification;

internal readonly struct SemVer
{
    internal int Major { get; init; }
    internal int Minor { get; init; }
    internal int Patch { get; init; }

    public static implicit operator SemVer(string semverString)
    {
        var parts = semverString.Split('.');
        if (parts.Length != 3)
            throw new ArgumentException($"{semverString} does not consist of three parts", nameof(semverString));
        if (!int.TryParse(parts[0], out var major))
            throw new ArgumentException($"The major part of {semverString} is not a valid integer", nameof(semverString));
        if (!int.TryParse(parts[1], out var minor))
            throw new ArgumentException($"The minor part of {semverString} is not a valid integer", nameof(semverString));
        if (!int.TryParse(parts[2], out var patch))
            throw new ArgumentException($"The patch part of {semverString} is not a valid integer", nameof(semverString));
        return new SemVer
        {
            Major = major,
            Minor = minor,
            Patch = patch
        };
    }

    public override string ToString() =>
        $"{Major}.{Minor}.{Patch}";

    public static bool operator ==(SemVer current, SemVer other) =>
        current.Equals(other);
    public static bool operator !=(SemVer current, SemVer other) =>
        !current.Equals(other);
    public static bool operator >(SemVer current, SemVer other)
    {
        if (current.Major > other.Major)
            return true;
        if (current.Major < other.Major)
            return false;
        if (current.Minor > other.Minor)
            return true;
        if (current.Minor < other.Minor)
            return false;
        return current.Patch > other.Patch;
    }
    public static bool operator >=(SemVer current, SemVer other) =>
        current > other ||
        current == other;
    public static bool operator <(SemVer current, SemVer other) =>
        !(current >= other);
    public static bool operator <=(SemVer current, SemVer other) =>
        current < other ||
        current == other;
    public override int GetHashCode() =>
        HashCode.Combine(Major, Minor, Patch);
    public override bool Equals(object? obj) =>
        obj is SemVer other &&
        GetHashCode() == other.GetHashCode();
}