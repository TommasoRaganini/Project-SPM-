namespace Application.DTOs.Requests;

public class UpdateItineraryStopRequest
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }
}