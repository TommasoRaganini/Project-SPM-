namespace Application.DTOs.Requests;

public record CreateItineraryRequest(
    string Title,
    string Theme,
    string Description,
    string GeographicalArea
);
