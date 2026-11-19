namespace Application.DTOs;

public record CreateItineraryRequest(
    string Title,
    string Theme,
    string Description,
    string GeographicalArea
);
