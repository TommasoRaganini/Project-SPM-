
using Domain.Common;
using Domain.States;

namespace Domain.Entities;

public class Itinerary
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Theme { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string GeographicalArea { get; private set; } = string.Empty;
    public string ResponsibleEditorId { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public ItineraryStatus Status { get; private set; }
    public ParticipationStatus ParticipationStatus { get; private set; } = ParticipationStatus.Closed;

    private IItineraryState _state = null!;
    public IItineraryState State => _state ??= CreateState(Status);

    private IParticipationState _participationState = null!;
    public IParticipationState ParticipationState => _participationState ??= CreateParticipationState(ParticipationStatus);

    // Collection of fixed stops belonging to the itinerary
    private readonly List<ItineraryStop> _stops = new();

    public IReadOnlyCollection<ItineraryStop> Stops => _stops.AsReadOnly();

    // Private constructor for EF Core materialization
    private Itinerary()
    {
    }

    /// <summary>
    /// Creates a new thematic itinerary in draft status (US01 / AC01, AC02, AC03, AC04).
    /// </summary>
    public static Result<Itinerary> CreateDraft(
        string? title,
        string? theme,
        string? description,
        string? geographicalArea,
        string? responsibleEditorId)
    {
        var errors = new List<string>();

        // AC04: If a required field is missing or invalid,
        // the system reports the issue and does not save.
        if (string.IsNullOrWhiteSpace(title))
            errors.Add("Title is required.");

        if (string.IsNullOrWhiteSpace(theme))
            errors.Add("Theme is required.");

        if (string.IsNullOrWhiteSpace(description))
            errors.Add("Description is required.");

        if (string.IsNullOrWhiteSpace(geographicalArea))
            errors.Add("Geographical area is required.");

        if (string.IsNullOrWhiteSpace(responsibleEditorId))
            errors.Add("Responsible editor is required.");

        if (errors.Count > 0)
            return Result<Itinerary>.Failure(errors);

        var itinerary = new Itinerary
        {
            // AC03: System assigns a unique identifier
            Id = Guid.NewGuid(),
            Title = title!.Trim(),
            Theme = theme!.Trim(),
            Description = description!.Trim(),
            GeographicalArea = geographicalArea!.Trim(),

            // AC03: System records the responsible editor
            ResponsibleEditorId = responsibleEditorId!.Trim(),

            CreatedAtUtc = DateTime.UtcNow
        };

        // AC02: On creation, the itinerary is assigned Draft status
        itinerary.TransitionTo(new DraftState());
        itinerary.TransitionParticipationTo(new ClosedParticipationState());

        return Result<Itinerary>.Success(itinerary);
    }

    /// <summary>
    /// Adds a new fixed stop to the itinerary.
    /// Only draft itineraries can be modified (governed by the State pattern).
    /// </summary>
    public Result<ItineraryStop> AddStop(
        string? name,
        string? description,
        double latitude,
        double longitude)
    {
        if (!State.CanEditStops)
            return Result<ItineraryStop>.Failure(
                "Only draft itineraries can be modified.");

        // Create and validate the new stop
        var result = ItineraryStop.Create(
            name,
            description,
            latitude,
            longitude);

        if (result.IsFailure)
            return result;

        // Associate the stop with this itinerary
        result.Value!.SetItineraryId(Id);
        result.Value.SetPosition(_stops.Count + 1);

        // Add the stop to the itinerary collection
        _stops.Add(result.Value);

        return Result<ItineraryStop>.Success(result.Value);
    }

    /// <summary>
    /// Updates an existing stop belonging to this itinerary.
    /// Only draft itineraries can be modified (governed by the State pattern).
    /// </summary>
    public Result<ItineraryStop> UpdateStop(
        Guid stopId,
        string? name,
        string? description,
        double latitude,
        double longitude)
    {
        if (!State.CanEditStops)
            return Result<ItineraryStop>.Failure(
                "Only draft itineraries can be modified.");

        // Find the stop inside this itinerary
        var stop = _stops.FirstOrDefault(s => s.Id == stopId);

        if (stop == null)
            return Result<ItineraryStop>.Failure("Stop not found.");

        // Update and validate stop details
        return stop.Update(
            name,
            description,
            latitude,
            longitude);
    }

    public IEnumerable<ItineraryStop> GetOrderedStops() =>
        _stops.OrderBy(stop => stop.Position).ThenBy(stop => stop.Id);

    /// <summary>
    /// Publishes a draft itinerary (US04 / AC01, AC02, AC04, AC05, AC06).
    /// Validates mandatory prerequisites: title, theme, description, and at least two valid fixed stops.
    /// Does not automatically open participation to operators (AC06).
    /// </summary>
    public Result<Itinerary> Publish()
    {
        var errors = new List<string>();

        if (Status != ItineraryStatus.Draft)
            errors.Add("Only draft itineraries can be published.");

        if (string.IsNullOrWhiteSpace(Title))
            errors.Add("Title is required.");

        if (string.IsNullOrWhiteSpace(Theme))
            errors.Add("Theme is required.");

        if (string.IsNullOrWhiteSpace(Description))
            errors.Add("Description is required.");

        if (_stops.Count < 2)
            errors.Add("An itinerary must have at least two fixed stops to be published.");

        if (errors.Count > 0)
            return Result<Itinerary>.Failure(errors);

        TransitionTo(new PublishedState());
        // AC06: Publication does not automatically open the itinerary to participation by operators.
        TransitionParticipationTo(new ClosedParticipationState());

        return Result<Itinerary>.Success(this);
    }

    /// <summary>
    /// Replaces the order of all fixed stops in a draft itinerary (US03 / AC01, AC03, AC05).
    /// Validates the complete sequence before updating any positions.
    /// </summary>
    public Result<IReadOnlyList<ItineraryStop>> ReorderStops(IReadOnlyList<Guid> stopIds)
    {
        if (!State.CanEditStops)
            return Result<IReadOnlyList<ItineraryStop>>.Failure(
                "Only draft itineraries can be modified.");

        var stopsById = _stops.ToDictionary(stop => stop.Id);
        if (stopIds.Count != stopsById.Count ||
            stopIds.Distinct().Count() != stopIds.Count ||
            stopIds.Any(id => !stopsById.ContainsKey(id)))
        {
            return Result<IReadOnlyList<ItineraryStop>>.Failure(
                "Specify every itinerary stop exactly once.");
        }

        var orderedStops = stopIds.Select(id => stopsById[id]).ToList();
        for (var index = 0; index < orderedStops.Count; index++)
            orderedStops[index].SetPosition(index + 1);

        return Result<IReadOnlyList<ItineraryStop>>.Success(orderedStops);
    }

    public void TransitionTo(IItineraryState newState)
    {
        _state = newState ?? throw new ArgumentNullException(nameof(newState));
        Status = newState.Status;
    }

    public void TransitionParticipationTo(IParticipationState newState)
    {
        _participationState = newState ?? throw new ArgumentNullException(nameof(newState));
        ParticipationStatus = newState.Status;
    }

    public void RestoreState()
    {
        _state = CreateState(Status);
        _participationState = CreateParticipationState(ParticipationStatus);
    }

    private static IItineraryState CreateState(ItineraryStatus status) => status switch
    {
        ItineraryStatus.Draft => new DraftState(),
        ItineraryStatus.Published => new PublishedState(),
        _ => new DraftState()
    };

    private static IParticipationState CreateParticipationState(ParticipationStatus status) => status switch
    {
        ParticipationStatus.Closed => new ClosedParticipationState(),
        ParticipationStatus.Open => new OpenParticipationState(),
        _ => new ClosedParticipationState()
    };
}
