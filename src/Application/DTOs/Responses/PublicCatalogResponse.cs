namespace Application.DTOs.Responses;

/// <summary>
/// Response model for the public itinerary catalog (US05).
/// AC05: Contains an understandable message when no published itineraries are available.
/// </summary>
public record PublicCatalogResponse(
    IReadOnlyList<ItineraryResponse> Items,
    string? Message = null
);

