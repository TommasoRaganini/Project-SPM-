using Application.DTOs;
using Application.Interfaces;
using Domain.Common;
using Domain.Entities;

namespace Application.Services;

public class ItineraryService : IItineraryService
{
    private readonly IItineraryRepository _repository;

    public ItineraryService(IItineraryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ItineraryResponse>> CreateDraftAsync(
        CreateItineraryRequest request,
        string authenticatedEditorId,
        CancellationToken cancellationToken = default)
    {
        // AC01 & AC04: Domain creation and validation with authenticated editor
        var createResult = Itinerary.CreateDraft(
            request.Title,
            request.Theme,
            request.Description,
            request.GeographicalArea,
            authenticatedEditorId);

        if (createResult.IsFailure)
        {
            // AC04: Does not save itinerary and reports validation errors
            return Result<ItineraryResponse>.Failure(createResult.Errors);
        }

        var itinerary = createResult.Value!;

        // AC03: Persist itinerary with unique identifier and responsible editor
        await _repository.AddAsync(itinerary, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ItineraryResponse>.Success(ItineraryResponse.FromEntity(itinerary));
    }

    public async Task<Result<ItineraryResponse>> GetDraftByIdAsync(
        Guid id,
        string authenticatedEditorId,
        CancellationToken cancellationToken = default)
    {
        // AC05: Editor can reopen draft and retrieve previously saved data
        var itinerary = await _repository.GetByIdAsync(id, cancellationToken);
        if (itinerary == null)
        {
            return Result<ItineraryResponse>.Failure("Itinerary not found.");
        }

        return Result<ItineraryResponse>.Success(ItineraryResponse.FromEntity(itinerary));
    }

    public async Task<IReadOnlyList<ItineraryResponse>> GetPublicCatalogAsync(CancellationToken cancellationToken = default)
    {
        // AC02: Public catalog verification (drafts are excluded)
        var list = await _repository.GetPublicCatalogAsync(cancellationToken);
        return list.Select(ItineraryResponse.FromEntity).ToList();
    }
}
