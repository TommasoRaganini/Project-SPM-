using Domain.Entities;

namespace Application.DTOs.Responses;

/// <summary>
/// Detailed response model for viewing a published itinerary and its ordered stops.
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

