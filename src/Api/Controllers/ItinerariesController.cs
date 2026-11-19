using Application.DTOs;
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

        var result = await _itineraryService.CreateDraftAsync(request, editorId, cancellationToken);

        if (result.IsFailure)
        {
            // AC04: Reports issues and does not save itinerary
            return BadRequest(new { errors = result.Errors });
        }

        // AC01, AC02, AC03: Returns created draft itinerary with unique ID and Draft status
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
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        [FromHeader(Name = "X-Editor-Id")] string? editorId,
        CancellationToken cancellationToken)
    {
        var result = await _itineraryService.GetDraftByIdAsync(id, editorId ?? string.Empty, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// AC02 & US05: Public catalog of published itineraries (excludes drafts).
    /// </summary>
    [HttpGet("public-catalog")]
    [ProducesResponseType(typeof(IEnumerable<ItineraryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublicCatalog(CancellationToken cancellationToken)
    {
        var catalog = await _itineraryService.GetPublicCatalogAsync(cancellationToken);
        return Ok(catalog);
    }
}
