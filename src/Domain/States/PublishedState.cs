namespace Domain.States;

public class PublishedState : IItineraryState
{
    public ItineraryStatus Status => ItineraryStatus.Published;
    
    public bool IsVisibleInPublicCatalog => true;
    
    public bool CanEditStops => false;
    
    public bool CanPublish(int validStopsCount) => false;
}
