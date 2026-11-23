using Domain.Entities;
using Domain.States;

namespace Application.DTOs.Responses;

public record ItineraryResponse(
    Guid Id,
    string Title,
    string Theme,
    string Description,
    string GeographicalArea,
    string ResponsibleEditorId,
    string Status,
    bool IsVisibleInPublicCatalog,
    DateTime CreatedAtUtc,
    string ParticipationStatus
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
        itinerary.CreatedAtUtc,
        itinerary.ParticipationStatus.ToString()
    );
}

