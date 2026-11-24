namespace Domain.States;

public class DraftState : IItineraryState
{
    public ItineraryStatus Status => ItineraryStatus.Draft;
    
    public bool IsVisibleInPublicCatalog => false;
    
    public bool CanEditStops => true;
    
    public bool CanPublish(int validStopsCount) => validStopsCount >= 2;
}
