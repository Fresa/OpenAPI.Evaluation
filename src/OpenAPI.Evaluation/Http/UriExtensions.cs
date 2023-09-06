namespace OpenAPI.Evaluation.Http;

internal static class UriExtensions
{
    private static readonly Uri DummyAbsoluteHost = new("http://dummy");
    internal static string[] GetPathSegments(this Uri uri) =>
        uri.GetPath()
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

    internal static string GetPath(this Uri uri)
    {
        var absoluteUri = uri.IsAbsoluteUri ? uri : new Uri(DummyAbsoluteHost, uri);
        return absoluteUri
            .GetComponents(UriComponents.Path, UriFormat.Unescaped);
    }
}