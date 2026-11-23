using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Represents a geolocated fixed stop belonging to an itinerary.
/// </summary>
public class ItineraryStop
{
    public Guid Id { get; private set; }

    public Guid ItineraryId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public double Latitude { get; private set; }

    public double Longitude { get; private set; }

    public int Position { get; private set; }

    // Private constructor for EF Core materialization
    private ItineraryStop()
    {
    }

    /// <summary>
    /// Creates a new geolocated fixed stop.
    /// Validates required fields and coordinate ranges (-90 to 90 lat, -180 to 180 lon).
    /// </summary>
    public static Result<ItineraryStop> Create(
        string? name,
        string? description,
        double latitude,
        double longitude)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(name))
            errors.Add("Stop name is required.");

        if (string.IsNullOrWhiteSpace(description))
            errors.Add("Stop description is required.");

        if (double.IsNaN(latitude) || double.IsInfinity(latitude)
            || latitude < -90 || latitude > 90)
            errors.Add("Latitude must be between -90 and 90.");

        if (double.IsNaN(longitude) || double.IsInfinity(longitude)
            || longitude < -180 || longitude > 180)
            errors.Add("Longitude must be between -180 and 180.");

        if (errors.Count > 0)
            return Result<ItineraryStop>.Failure(errors);

        var stop = new ItineraryStop
        {
            Id = Guid.NewGuid(),
            Name = name!.Trim(),
            Description = description!.Trim(),
            Latitude = latitude,
            Longitude = longitude
        };

        return Result<ItineraryStop>.Success(stop);
    }

    /// <summary>
    /// Updates the stop details with validation.
    /// </summary>
    public Result<ItineraryStop> Update(
        string? name,
        string? description,
        double latitude,
        double longitude)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(name))
            errors.Add("Stop name is required.");

        if (string.IsNullOrWhiteSpace(description))
            errors.Add("Stop description is required.");

        if (double.IsNaN(latitude) || double.IsInfinity(latitude)
            || latitude < -90 || latitude > 90)
            errors.Add("Latitude must be between -90 and 90.");

        if (double.IsNaN(longitude) || double.IsInfinity(longitude)
            || longitude < -180 || longitude > 180)
            errors.Add("Longitude must be between -180 and 180.");

        if (errors.Count > 0)
            return Result<ItineraryStop>.Failure(errors);

        Name = name!.Trim();
        Description = description!.Trim();
        Latitude = latitude;
        Longitude = longitude;

        return Result<ItineraryStop>.Success(this);
    }

    internal void SetPosition(int position) => Position = position;

    /// <summary>
    /// Associates the stop with an itinerary.
    /// </summary>
    internal void SetItineraryId(Guid itineraryId)
    {
        ItineraryId = itineraryId;
    }
}
