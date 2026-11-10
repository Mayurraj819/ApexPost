using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ApexPost.Api.OpenApi;
/// <summary>Shipment API contract or supporting operation.</summary>
public sealed class LogisticsDocumentTransformer : IOpenApiDocumentTransformer
{
    /// <summary>Shipment API contract or supporting operation.</summary>
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info = new OpenApiInfo
        {
            Title = "ApexPost Logistics API",
            Version = context.DocumentName,
            Description = "Shipment tracking and idempotent courier scan upload. v1 is deprecated; migrate to v2.",
            Contact = new OpenApiContact
            {
                Name = "ApexPost Training Team",
                Email = "api-team@apexpost.example"
            }
        };
        document.Servers = [];
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Paste a development JWT. Never commit tokens."
        };
        document.Components.SecuritySchemes["PartnerKey"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            Name = "X-Api-Key",
            In = ParameterLocation.Header
        };
        foreach (var path in document.Paths)
            foreach (var operation in path.Value.Operations?.Values.AsEnumerable() ?? [])
                operation.Security = [new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(path.Key.Contains("/partner/") ? "PartnerKey" : "Bearer", document)] = []
                }

                ];
        return Task.CompletedTask;
    }
}
