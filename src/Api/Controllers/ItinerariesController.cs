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
    /// US01: Creates a new thematic itinerary in draft status.
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
            // AC04: Reports issues and does not save itinerary
            return BadRequest(new { errors = result.Errors });
        }

        // AC01, AC02, AC03: Returns created draft itinerary
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value!.Id },
            result.Value
        );
    }

    /// <summary>
    /// US01 / AC05: Reopens draft and retrieves previously saved data.
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
    /// AC02 & US05: Public catalog of published itineraries (excludes drafts).
    /// AC05: Returns an understandable message when no published itineraries are available.
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
    /// US05 / AC03: Opens a published itinerary for tourists, displaying description, ordered stops, and map coordinates.
    /// AC04: Rejects drafts from public views.
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
    /// US04: Publishes a draft itinerary (AC01 - AC06).
    /// AC01: Requires title, theme, description, and at least two valid fixed stops.
    /// AC02: Changes status from Draft to Published.
    /// AC03: Published itinerary appears in public catalog.
    /// AC04: Fixes stops and their order as saved at publication time.
    /// AC05: Prevents publication and returns errors if prerequisites are not met.
    /// AC06: Keeps participation status Closed.
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
    /// US02: Adds a new geolocated fixed stop to a draft itinerary.
    /// AC01: Name, description and geographical coordinates are required.
    /// AC04: Coordinates must be within valid geographical ranges.
    /// AC05: Only the responsible editor can modify itinerary stops.
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
    /// US02: Updates an existing geolocated fixed stop.
    /// AC03: Changes are saved and returned.
    /// AC04: Coordinates must be within valid geographical ranges.
    /// AC05: Only the responsible editor can modify itinerary stops.
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
    /// US02: Retrieves all fixed stops belonging to an itinerary.
    /// AC02: Returns the itinerary stops for visualization on the map.
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
    /// US03: Replaces the order of all fixed stops in a draft itinerary.
    /// AC01: The responsible editor supplies the complete sequence.
    /// AC02: The updated order is saved and returned.
    /// AC03: Duplicate, missing and foreign stops are rejected.
    /// AC05: Stop details and coordinates are preserved.
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
    /// US03: Retrieves the segments of an itinerary.
    /// AC04: Each segment connects two consecutive stops in the saved order.
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
