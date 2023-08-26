using Yaml2JsonNode;
using YamlDotNet.RepresentationModel;

namespace OpenAPI.Validation.IntegrationTests.OpenApi;

internal static class OpenApi
{
    public static OpenApiDocument Load(string pathRelativeToRoot, Uri baseUri)
    {
        if (!pathRelativeToRoot.EndsWith(".yaml", StringComparison.InvariantCultureIgnoreCase))
            throw new ArgumentException("Only yaml files are supported", pathRelativeToRoot);
        var yaml = new YamlStream();
        using var reader = File.OpenText(Path.Combine(AppContext.BaseDirectory, pathRelativeToRoot));
        yaml.Load(reader);
        var document = yaml.ToJsonNode().First() ??
                       throw new InvalidOperationException($"{pathRelativeToRoot} is not an open api document");
        return OpenApiDocument.Parse(document, baseUri);
    }
}