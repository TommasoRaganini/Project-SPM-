namespace Application.DTOs.Requests;

public class CreateItineraryStopRequest
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public CreateItineraryStopRequest() { }

    public CreateItineraryStopRequest(string name, string description, double latitude, double longitude)
    {
        Name = name;
        Description = description;
        Latitude = latitude;
        Longitude = longitude;
    }
}