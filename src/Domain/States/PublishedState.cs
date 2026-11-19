namespace Domain.States;

public class PublishedState : IItineraryState
{
    public ItineraryStatus Status => ItineraryStatus.Published;
    
    // AC03 (US04) / AC01 (US05): Published itineraries appear in the public catalog
    public bool IsVisibleInPublicCatalog => true;
    
    public bool CanEditStops => false;
    
    public bool CanPublish(int validStopsCount) => false;
}
