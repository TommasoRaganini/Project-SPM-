namespace Domain.States;

public interface IItineraryState
{
    ItineraryStatus Status { get; }
    bool IsVisibleInPublicCatalog { get; }
    bool CanEditStops { get; }
    bool CanPublish(int validStopsCount);
}

