namespace OpenAPI.Validation.Http;

internal static class UriExtensions
{
    private static readonly System.Uri DummyAbsoluteHost = new("http://dummy");
    internal static string[] GetPathSegments(this System.Uri uri)
    {
        var absoluteUri = uri.IsAbsoluteUri ? uri : new System.Uri(DummyAbsoluteHost, uri);
        return absoluteUri
            .GetComponents(UriComponents.Path, UriFormat.Unescaped)
            .Split('/', StringSplitOptions.RemoveEmptyEntries);
    }
}