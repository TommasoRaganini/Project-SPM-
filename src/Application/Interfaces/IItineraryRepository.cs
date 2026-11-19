using Domain.Entities;

namespace Application.Interfaces;

public interface IItineraryRepository
{
    Task AddAsync(Itinerary itinerary, CancellationToken cancellationToken = default);
    Task<Itinerary?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Itinerary>> GetPublicCatalogAsync(CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

