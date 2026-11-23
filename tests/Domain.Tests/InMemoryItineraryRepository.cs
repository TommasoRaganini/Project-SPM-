using Application.Interfaces;
using Domain.Entities;
using Domain.States;

namespace Domain.Tests;

internal sealed class InMemoryItineraryRepository : IItineraryRepository
{
    public readonly List<Itinerary> Items = new();

    public int SaveCount { get; private set; }

    public Task AddAsync(Itinerary itinerary, CancellationToken cancellationToken = default)
    {
        Items.Add(itinerary);
        return Task.CompletedTask;
    }

    public Task<Itinerary?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var found = Items.FirstOrDefault(i => i.Id == id);
        found?.RestoreState();
        return Task.FromResult(found);
    }

    public Task<IReadOnlyList<Itinerary>> GetPublicCatalogAsync(CancellationToken cancellationToken = default)
    {
        var list = Items.Where(i => i.Status == ItineraryStatus.Published).ToList();
        list.ForEach(i => i.RestoreState());
        return Task.FromResult<IReadOnlyList<Itinerary>>(list);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
