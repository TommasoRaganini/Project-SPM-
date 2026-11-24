namespace Application.DTOs.Responses;

/// <summary>
/// Response model for the public itinerary catalog.
/// Contains an informational message when no published itineraries are available.
/// </summary>
public record PublicCatalogResponse(
    IReadOnlyList<ItineraryResponse> Items,
    string? Message = null
);

