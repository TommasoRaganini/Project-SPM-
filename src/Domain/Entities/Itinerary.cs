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

    private IItineraryState _state = null!;
    public IItineraryState State => _state ??= CreateState(Status);

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

        // AC04: If a required field is missing or invalid, the system reports the issue and does not save
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

        return Result<Itinerary>.Success(itinerary);
    }

    public void TransitionTo(IItineraryState newState)
    {
        _state = newState ?? throw new ArgumentNullException(nameof(newState));
        Status = newState.Status;
    }

    public void RestoreState()
    {
        _state = CreateState(Status);
    }

    private static IItineraryState CreateState(ItineraryStatus status) => status switch
    {
        ItineraryStatus.Draft => new DraftState(),
        ItineraryStatus.Published => new PublishedState(),
        _ => new DraftState()
    };
}
