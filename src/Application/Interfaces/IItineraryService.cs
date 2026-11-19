using Application.DTOs;
using Domain.Common;

namespace Application.Interfaces;

public interface IItineraryService
{
    Task<Result<ItineraryResponse>> CreateDraftAsync(CreateItineraryRequest request, string authenticatedEditorId, CancellationToken cancellationToken = default);
    Task<Result<ItineraryResponse>> GetDraftByIdAsync(Guid id, string authenticatedEditorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ItineraryResponse>> GetPublicCatalogAsync(CancellationToken cancellationToken = default);
}

