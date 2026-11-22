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
    /// </summary>
    [HttpGet("public-catalog")]
    [ProducesResponseType(typeof(IEnumerable<ItineraryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublicCatalog(
        CancellationToken cancellationToken)
    {
        var catalog = await _itineraryService.GetPublicCatalogAsync(
            cancellationToken);

        return Ok(catalog);
    }

    // ============================================================
    // ITINERARY STOPS - NEW USER STORY
    // ============================================================

    /// <summary>
    /// US: Adds a new geolocated fixed stop to a draft itinerary.
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
    /// US: Updates an existing geolocated fixed stop.
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
    /// US: Retrieves all fixed stops belonging to an itinerary.
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
}
