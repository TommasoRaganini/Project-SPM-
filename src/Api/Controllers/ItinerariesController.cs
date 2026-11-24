using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ItinerariesController : ControllerBase
{
    private readonly IItineraryService _itineraryService;

    public ItinerariesController(IItineraryService itineraryService)
    {
        _itineraryService = itineraryService;
    }

    /// <summary>
    /// Creates a new thematic itinerary in draft status.
    /// The authenticated editor identity is provided via the 'X-Editor-Id' HTTP header.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ItineraryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(IDictionary<string, string[]>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDraft(
        [FromBody] CreateItineraryRequest request,
        [FromHeader(Name = "X-Editor-Id")] string? headerEditorId,
        CancellationToken cancellationToken)
    {
        var editorId = headerEditorId ?? string.Empty;

        var result = await _itineraryService.CreateDraftAsync(
            request, editorId, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value!.Id },
            result.Value
        );
    }

    /// <summary>
    /// Retrieves a draft itinerary by its identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ItineraryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        [FromHeader(Name = "X-Editor-Id")] string? editorId,
        CancellationToken cancellationToken)
    {
        var result = await _itineraryService.GetDraftByIdAsync(
            id, editorId ?? string.Empty, cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Errors.FirstOrDefault();

            if (error == "Itinerary not found.")
                return NotFound(new { error });

            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Retrieves the public catalog of published itineraries.
    /// </summary>
    [HttpGet("public-catalog")]
    [ProducesResponseType(typeof(PublicCatalogResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublicCatalog(
        CancellationToken cancellationToken)
    {
        var catalog = await _itineraryService.GetPublicCatalogAsync(
            cancellationToken);

        return Ok(catalog);
    }

    /// <summary>
    /// Retrieves details of a published itinerary and its ordered stops for public viewing.
    /// </summary>
    [HttpGet("public-catalog/{id:guid}")]
    [ProducesResponseType(typeof(PublicItineraryDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublishedById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _itineraryService.GetPublishedItineraryByIdAsync(
            id, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Publishes a draft itinerary once all prerequisites are satisfied.
    /// </summary>
    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(typeof(ItineraryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Publish(
        [FromRoute] Guid id,
        [FromHeader(Name = "X-Editor-Id")] string? editorId,
        CancellationToken cancellationToken)
    {
        var result = await _itineraryService.PublishItineraryAsync(
            id,
            editorId ?? string.Empty,
            cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Errors.FirstOrDefault();

            if (error == "Itinerary not found.")
                return NotFound(new { error });

            if (error == "You are not authorized to publish this itinerary.")
                return StatusCode(StatusCodes.Status403Forbidden, new { error });

            return BadRequest(new { errors = result.Errors });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Adds a new geolocated fixed stop to a draft itinerary.
    /// </summary>
    [HttpPost("{id:guid}/stops")]
    [ProducesResponseType(typeof(ItineraryStopResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddStop(
        [FromRoute] Guid id,
        [FromBody] CreateItineraryStopRequest request,
        [FromHeader(Name = "X-Editor-Id")] string? editorId,
        CancellationToken cancellationToken)
    {
        var result = await _itineraryService.AddStopAsync(
            id,
            request,
            editorId ?? string.Empty,
            cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Errors.FirstOrDefault();

            if (error == "Itinerary not found.")
                return NotFound(new { error });

            if (error == "You are not authorized to modify this itinerary.")
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new { error });

            return BadRequest(new { errors = result.Errors });
        }

        return CreatedAtAction(
            nameof(GetStops),
            new { id },
            result.Value);
    }

    /// <summary>
    /// Updates an existing geolocated fixed stop in a draft itinerary.
    /// </summary>
    [HttpPut("{id:guid}/stops/{stopId:guid}")]
    [ProducesResponseType(typeof(ItineraryStopResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStop(
        [FromRoute] Guid id,
        [FromRoute] Guid stopId,
        [FromBody] UpdateItineraryStopRequest request,
        [FromHeader(Name = "X-Editor-Id")] string? editorId,
        CancellationToken cancellationToken)
    {
        var result = await _itineraryService.UpdateStopAsync(
            id,
            stopId,
            request,
            editorId ?? string.Empty,
            cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Errors.FirstOrDefault();

            if (error == "Itinerary not found.")
                return NotFound(new { error });

            if (error == "You are not authorized to modify this itinerary.")
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new { error });

            if (error == "Stop not found.")
                return NotFound(new { error });

            return BadRequest(new { errors = result.Errors });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Retrieves all fixed stops belonging to an itinerary.
    /// </summary>
    [HttpGet("{id:guid}/stops")]
    [ProducesResponseType(typeof(IEnumerable<ItineraryStopResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStops(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _itineraryService.GetStopsAsync(
            id, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new
            {
                error = result.Errors.FirstOrDefault()
            });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Replaces the order of all fixed stops in a draft itinerary.
    /// </summary>
    [HttpPut("{id:guid}/stops/order")]
    [ProducesResponseType(typeof(IReadOnlyList<ItineraryStopResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReorderStops(
        [FromRoute] Guid id,
        [FromBody] ReorderItineraryStopsRequest request,
        [FromHeader(Name = "X-Editor-Id")] string? editorId,
        CancellationToken cancellationToken)
    {
        var result = await _itineraryService.ReorderStopsAsync(
            id, request, editorId ?? string.Empty, cancellationToken);

        if (result.IsSuccess)
            return Ok(result.Value);

        var error = result.Errors.FirstOrDefault();
        if (error == "Itinerary not found.")
            return NotFound(new { error });

        if (error == "You are not authorized to modify this itinerary.")
            return StatusCode(StatusCodes.Status403Forbidden, new { error });

        return BadRequest(new { errors = result.Errors });
    }

    /// <summary>
    /// Retrieves the consecutive segments connecting stops in the saved order.
    /// </summary>
    [HttpGet("{id:guid}/segments")]
    [ProducesResponseType(typeof(IReadOnlyList<ItinerarySegmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSegments(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _itineraryService.GetSegmentsAsync(id, cancellationToken);
        if (result.IsFailure)
            return NotFound(new { error = result.Errors.FirstOrDefault() });

        return Ok(result.Value);
    }
}
