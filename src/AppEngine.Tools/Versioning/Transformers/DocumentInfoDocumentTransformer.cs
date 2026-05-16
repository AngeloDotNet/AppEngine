using System.Text;
using AppEngine.Tools.Settings;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.OpenApi;

namespace AppEngine.Tools.Versioning.Transformers;

public class DocumentInfoDocumentTransformer(IApiVersionDescriptionProvider apiVersionDescriptionProvider, IOptionsMonitor<ApiDocsSettings> options) : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var apiDescription = apiVersionDescriptionProvider.ApiVersionDescriptions
            .FirstOrDefault(d => d.GroupName == context.DocumentName);

        var settings = options.CurrentValue;

        if (apiDescription is not null)
        {
            document.Info = CreateInfoForApiVersion(apiDescription, settings);

            if (settings.EnabledExternalDocs)
            {
                document.ExternalDocs = new OpenApiExternalDocs
                {
                    Description = settings.ExternalDocsText,
                    Url = settings.ExternalDocsUri
                };
            }

            if (settings.EnabledLicense)
            {
                document.Info.License = new OpenApiLicense
                {
                    Name = settings.LicenseText,
                    Url = settings.LicenseUri
                };
            }
        }

        return Task.CompletedTask;
    }

    private static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description, ApiDocsSettings apiDocsSettings)
    {
        var text = new StringBuilder(apiDocsSettings.TextInfoApiVersion);
        var info = new OpenApiInfo
        {
            Title = apiDocsSettings.TitleInfoApiVersion,
            Version = description.ApiVersion.ToString(),
        };

        if (description.IsDeprecated)
        {
            text.Append(apiDocsSettings.TextApiDeprecated);
        }

        if (description.SunsetPolicy is { } policy)
        {
            if (policy.Date is { } when)
            {
                text.Append(" The API will be sunset on ")
                    .Append(when.Date.ToShortDateString())
                    .Append('.');
            }

            if (policy.HasLinks)
            {
                text.AppendLine();
                var rendered = false;

                for (var i = 0; i < policy.Links.Count; i++)
                {
                    var link = policy.Links[i];

                    if (link.Type == "text/html")
                    {
                        if (!rendered)
                        {
                            text.Append("<h4>Links</h4><ul>");
                            rendered = true;
                        }

                        text.Append("<li><a href=\"");
                        text.Append(link.LinkTarget.OriginalString);
                        text.Append("\">");
                        text.Append(StringSegment.IsNullOrEmpty(link.Title) ? link.LinkTarget.OriginalString : link.Title.ToString());
                        text.Append("</a></li>");
                    }
                }

                if (rendered)
                {
                    text.Append("</ul>");
                }
            }
        }

        info.Description = text.ToString();

        return info;
    }
}

//public class DocumentInfoDocumentTransformer(IApiVersionDescriptionProvider apiVersionDescriptionProvider) : IOpenApiDocumentTransformer
//{
//    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
//    {
//        var apiDescription = apiVersionDescriptionProvider.ApiVersionDescriptions
//            .FirstOrDefault(d => d.GroupName == context.DocumentName);

//        if (apiDescription is not null)
//        {
//            document.Info = CreateInfoForApiVersion(apiDescription);
//        }

//        return Task.CompletedTask;
//    }

//    private static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
//    {
//        var text = new StringBuilder("An example about how to use route versioning in a Minimal API project.");

//        var info = new OpenApiInfo
//        {
//            Title = "Route Versioning Web API",
//            Version = description.ApiVersion.ToString(),
//        };

//        if (description.IsDeprecated)
//        {
//            text.Append(" This API version has been deprecated.");
//        }

//        if (description.SunsetPolicy is { } policy)
//        {
//            if (policy.Date is { } when)
//            {
//                text.Append(" The API will be sunset on ")
//                    .Append(when.Date.ToShortDateString())
//                    .Append('.');
//            }

//            if (policy.HasLinks)
//            {
//                text.AppendLine();
//                var rendered = false;

//                for (var i = 0; i < policy.Links.Count; i++)
//                {
//                    var link = policy.Links[i];

//                    if (link.Type == "text/html")
//                    {
//                        if (!rendered)
//                        {
//                            text.Append("<h4>Links</h4><ul>");
//                            rendered = true;
//                        }

//                        text.Append("<li><a href=\"");
//                        text.Append(link.LinkTarget.OriginalString);
//                        text.Append("\">");
//                        text.Append(StringSegment.IsNullOrEmpty(link.Title) ? link.LinkTarget.OriginalString : link.Title.ToString());
//                        text.Append("</a></li>");
//                    }
//                }

//                if (rendered)
//                {
//                    text.Append("</ul>");
//                }
//            }
//        }

//        info.Description = text.ToString();
//        return info;
//    }
//}