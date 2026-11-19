namespace Domain.States;

public class DraftState : IItineraryState
{
    public ItineraryStatus Status => ItineraryStatus.Draft;
    
    // AC02: On creation, the itinerary is assigned Draft status and does not appear in the public catalog
    public bool IsVisibleInPublicCatalog => false;
    
    // Stop editing is permitted while the itinerary is in draft
    public bool CanEditStops => true;
    
    // Publication prerequisite (US04 / explicit hypothesis i: at least two valid stops)
    public bool CanPublish(int validStopsCount) => validStopsCount >= 2;
}
