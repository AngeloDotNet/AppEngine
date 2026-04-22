using Microsoft.OpenApi;

namespace AppEngine.Tools.OpenApi.Options;

public class OpenApiOperationOptions
{
    internal OpenApiOperationOptions()
    { }

    public IList<OpenApiParameter> Parameters { get; } = [];
}