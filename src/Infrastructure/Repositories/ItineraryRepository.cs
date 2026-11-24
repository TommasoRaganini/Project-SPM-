
using Application.Interfaces;
using Domain.Entities;
using Domain.States;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ItineraryRepository : IItineraryRepository
{
    private readonly ApplicationDbContext _context;

    public ItineraryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        Itinerary itinerary,
        CancellationToken cancellationToken = default)
    {
        await _context.Itineraries.AddAsync(
            itinerary, cancellationToken);
    }

    public async Task<Itinerary?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var itinerary = await _context.Itineraries
            .Include(i => i.Stops)
            .FirstOrDefaultAsync(
                i => i.Id == id,
                cancellationToken);

        itinerary?.RestoreState();

        return itinerary;
    }

    public async Task<IReadOnlyList<Itinerary>> GetPublicCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        var items = await _context.Itineraries
            .AsNoTracking()
            .Where(i => i.Status == ItineraryStatus.Published)
            .ToListAsync(cancellationToken);

        items.ForEach(i => i.RestoreState());

        return items;
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
