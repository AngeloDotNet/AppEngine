using Microsoft.AspNetCore.Http;

namespace AppEngine.Validation;

public class ValidationOptions
{
    public ErrorResponseFormat ErrorResponseFormat { get; set; }
    public Func<EndpointFilterInvocationContext, IDictionary<string, string[]>, string>? ValidationErrorTitleMessageFactory { get; set; }
}