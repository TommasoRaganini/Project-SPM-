using Domain.Entities;

namespace Application.DTOs.Responses;

/// <summary>
/// Unified response model for a tourist opening a published itinerary (US05 / AC03).
/// Aggregates descriptive metadata and ordered geolocated fixed stops for map display.
/// </summary>
public record PublicItineraryDetailResponse(
    Guid Id,
    string Title,
    string Theme,
    string Description,
    string GeographicalArea,
    IReadOnlyList<ItineraryStopResponse> Stops
)
{
    public static PublicItineraryDetailResponse FromEntity(Itinerary itinerary) => new(
        itinerary.Id,
        itinerary.Title,
        itinerary.Theme,
        itinerary.Description,
        itinerary.GeographicalArea,
        itinerary.GetOrderedStops().Select(stop => new ItineraryStopResponse
        {
            Id = stop.Id,
            ItineraryId = stop.ItineraryId,
            Name = stop.Name,
            Description = stop.Description,
            Latitude = stop.Latitude,
            Longitude = stop.Longitude,
            Position = stop.Position
        }).ToList()
    );
}

