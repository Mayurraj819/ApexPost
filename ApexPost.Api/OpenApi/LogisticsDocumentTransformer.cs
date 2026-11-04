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
        return Task.CompletedTask;
    }
}
