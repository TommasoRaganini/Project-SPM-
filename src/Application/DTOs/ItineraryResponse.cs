using Domain.Entities;
using Domain.States;

namespace Application.DTOs;

public record ItineraryResponse(
    Guid Id,
    string Title,
    string Theme,
    string Description,
    string GeographicalArea,
    string ResponsibleEditorId,
    string Status,
    bool IsVisibleInPublicCatalog,
    DateTime CreatedAtUtc
)
{
    public static ItineraryResponse FromEntity(Itinerary itinerary) => new(
        itinerary.Id,
        itinerary.Title,
        itinerary.Theme,
        itinerary.Description,
        itinerary.GeographicalArea,
        itinerary.ResponsibleEditorId,
        itinerary.Status.ToString(),
        itinerary.State.IsVisibleInPublicCatalog,
        itinerary.CreatedAtUtc
    );
}

