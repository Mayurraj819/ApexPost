using ApexPost.Api.Contracts;
using ApexPost.Application.Delivery;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexPost.Api.Controllers;
/// <summary>Read-only shipment access for partner systems.</summary>
[ApiController, ApiVersion("2.0"), Route("api/v{version:apiVersion}/partner/shipments"), Tags("Partners")]
[Authorize(AuthenticationSchemes = "PartnerKey")]
public sealed class PartnerShipmentsController(IShipmentApiService service) : ControllerBase
{
    /// <summary>Fetch a shipment using an approved X-Api-Key.</summary>
    [HttpGet("{trackingNumber:length(12)}", Name = "GetPartnerShipmentV2"), EndpointSummary("Partner read-only shipment lookup")]
    [ProducesResponseType<ShipmentV2>(200), ProducesResponseType<ProblemDetails>(401), ProducesResponseType<ProblemDetails>(404), ProducesResponseType<ProblemDetails>(500)]
    public async Task<ActionResult<ShipmentV2>> Get(string trackingNumber, CancellationToken ct) => Ok(ResponseMapping.V2(await service.GetAsync(trackingNumber, ct)));
}
