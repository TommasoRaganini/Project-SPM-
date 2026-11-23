namespace Application.DTOs.Requests;

public record ReorderItineraryStopsRequest(IReadOnlyList<Guid> StopIds);
