using ApexPost.Api.Security;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using ApexPost.Api.Contracts;
using ApexPost.Application.Delivery;
using ApexPost.Domain.Delivery;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace ApexPost.Api.Controllers;
/// <summary>HTTP endpoints for shipment tracking and courier scans.</summary>
[Authorize]
[ProducesResponseType<ProblemDetails>(401)]
[ProducesResponseType<ProblemDetails>(403)]
[ApiController, Route("api/v{version:apiVersion}/shipments"), Tags("Shipments")]
[ApiVersion("1.0", Deprecated = true)]
[Produces("application/json")]
[ProducesResponseType<ValidationProblemDetails>(400)]
[ProducesResponseType<ProblemDetails>(404)]
[ProducesResponseType<ProblemDetails>(409)]
[ProducesResponseType<ProblemDetails>(415)]
[ProducesResponseType<ProblemDetails>(500)]
public sealed class ShipmentsV1Controller(IShipmentApiService service) : ControllerBase
{
    /// <summary>List shipments with pagination and filters.</summary>
    [HttpGet(Name = "ListShipmentsV1"), EndpointSummary("List shipments with pagination and filters")]
    [ProducesResponseType<PageResponse<ShipmentV1>>(200)]
    public async Task<ActionResult<PageResponse<ShipmentV1>>> List([FromQuery] ShipmentFilter filter, CancellationToken ct)
    {
        var page = await service.ListAsync(filter.Page, filter.PageSize, filter.Status, filter.City, ct);
        return Ok(new PageResponse<ShipmentV1>(page.Items.Select(ResponseMapping.V1).ToArray(), page.Page, page.PageSize, page.TotalCount));
    }

    /// <summary>Fetch a shipment.</summary>
    [HttpGet("{trackingNumber:length(12)}", Name = "GetShipmentV1"), EndpointSummary("Fetch a shipment")]
    [ProducesResponseType<ShipmentV1>(200)]
    public async Task<ActionResult<ShipmentV1>> Get(string trackingNumber, CancellationToken ct) => Ok(ResponseMapping.V1(await service.GetAsync(trackingNumber, ct)));
    /// <summary>Create a shipment.</summary>
    [HttpPost(Name = "CreateShipmentV1"), Consumes("application/json"), EndpointSummary("Create a shipment")]
    [ProducesResponseType<ShipmentV1>(201)]
    public async Task<ActionResult<ShipmentV1>> Create(CreateShipmentRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request.TrackingNumber, request.ToDetails(), ct);
        return CreatedAtAction(nameof(Get), new { trackingNumber = result.TrackingNumber }, ResponseMapping.V1(result));
    }

    /// <summary>Replace all editable shipment details.</summary>
    [HttpPut("{trackingNumber:length(12)}", Name = "ReplaceShipmentV1"), Consumes("application/json"), EndpointSummary("Replace all editable shipment details")]
    [ProducesResponseType<ShipmentV1>(200)]
    public async Task<ActionResult<ShipmentV1>> Replace(string trackingNumber, UpdateShipmentRequest request, CancellationToken ct) => Ok(ResponseMapping.V1(await service.ReplaceAsync(trackingNumber, request.ToDetails(), ct)));
    /// <summary>Advance a shipment through its allowed status transitions.</summary>
    [Authorize(Roles="Courier")]
    [HttpPatch("{trackingNumber:length(12)}/status", Name = "ChangeShipmentStatusV1"), Consumes("application/json"), EndpointSummary("Advance a shipment through its allowed status transitions")]
    [ProducesResponseType<ShipmentV1>(200)]
    public async Task<ActionResult<ShipmentV1>> ChangeStatus(string trackingNumber, UpdateStatusRequest request, CancellationToken ct) => Ok(ResponseMapping.V1(await service.ChangeStatusAsync(trackingNumber, Enum.Parse<DeliveryStatus>(request.Status), ct)));
    /// <summary>Delete a Created shipment.</summary>
    [Authorize(Roles="Admin")]
    [HttpDelete("{trackingNumber:length(12)}", Name = "DeleteShipmentV1"), EndpointSummary("Delete a Created shipment")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Delete(string trackingNumber, CancellationToken ct)
    {
        await service.DeleteAsync(trackingNumber, ct);
        return NoContent();
    }

    /// <summary>Upload scans idempotently using their Part A IDs.</summary>
    [Authorize(Roles="Courier")]
    [ServiceFilter(typeof(CourierOwnershipFilter))]
    [HttpPost("{trackingNumber:length(12)}/scans", Name = "UploadShipmentScansV1"), Tags("Scans"), Consumes("application/json"), EndpointSummary("Upload scans idempotently using their Part A IDs")]
    [ProducesResponseType<ScanUploadResponse>(200)]
    public async Task<ActionResult<ScanUploadResponse>> Upload(string trackingNumber, [FromBody, ValidScanBatch] List<ScanUploadRequest> scans, CancellationToken ct) => Ok(ResponseMapping.Upload(await service.UploadAsync(trackingNumber, scans.Select(s => s.ToInput()).ToArray(), ct)));
    /// <summary>Read a shipment's scan history in UTC order.</summary>
    [HttpGet("{trackingNumber:length(12)}/scans", Name = "ListShipmentScansV1"), Tags("Scans"), EndpointSummary("Read a shipment's scan history in UTC order")]
    [ProducesResponseType<ScanResponse[]>(200)]
    public async Task<ActionResult<ScanResponse[]>> Scans(string trackingNumber, CancellationToken ct) => Ok((await service.ScansAsync(trackingNumber, ct)).Select(ResponseMapping.Scan).ToArray());
}
