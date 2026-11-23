namespace Application.DTOs.Responses;

public record ItinerarySegmentResponse(Guid FromStopId, Guid ToStopId);
