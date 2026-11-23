namespace Application.DTOs.Responses;

public class ItineraryStopResponse
{
    public Guid Id { get; set; }

    public Guid ItineraryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public int Position { get; set; }
}