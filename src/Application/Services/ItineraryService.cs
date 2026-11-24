using Application.DTOs.Requests;
using Application.DTOs.Responses;
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
        var createResult = Itinerary.CreateDraft(
            request.Title,
            request.Theme,
            request.Description,
            request.GeographicalArea,
            authenticatedEditorId);

        if (createResult.IsFailure)
        {
            return Result<ItineraryResponse>.Failure(createResult.Errors);
        }

        var itinerary = createResult.Value!;

        await _repository.AddAsync(itinerary, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ItineraryResponse>.Success(
            ItineraryResponse.FromEntity(itinerary));
    }

    public async Task<Result<ItineraryResponse>> GetDraftByIdAsync(
        Guid id,
        string authenticatedEditorId,
        CancellationToken cancellationToken = default)
    {
        var itinerary = await _repository.GetByIdAsync(
            id, cancellationToken);

        if (itinerary == null)
        {
            return Result<ItineraryResponse>.Failure(
                "Itinerary not found.");
        }

        if (itinerary.Status == Domain.States.ItineraryStatus.Draft && !IsAuthorizedEditor(itinerary, authenticatedEditorId))
        {
            return Result<ItineraryResponse>.Failure(
                "You are not authorized to access this itinerary.");
        }

        return Result<ItineraryResponse>.Success(
            ItineraryResponse.FromEntity(itinerary));
    }

    public async Task<PublicCatalogResponse> GetPublicCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _repository.GetPublicCatalogAsync(
            cancellationToken);

        var items = list.Select(ItineraryResponse.FromEntity).ToList();

        var message = items.Count == 0
            ? "No published itineraries available at this time."
            : null;

        return new PublicCatalogResponse(items, message);
    }

    /// <summary>
    /// Retrieves a published itinerary with its ordered stops for public visualization.
    /// </summary>
    public async Task<Result<PublicItineraryDetailResponse>> GetPublishedItineraryByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var itinerary = await _repository.GetByIdAsync(id, cancellationToken);
        if (itinerary == null)
        {
            return Result<PublicItineraryDetailResponse>.Failure("Itinerary not found.");
        }

        if (itinerary.Status != Domain.States.ItineraryStatus.Published)
        {
            return Result<PublicItineraryDetailResponse>.Failure(
                "Itinerary is not published.");
        }

        return Result<PublicItineraryDetailResponse>.Success(
            PublicItineraryDetailResponse.FromEntity(itinerary));
    }

    /// <summary>
    /// Adds a new geolocated fixed stop to a draft itinerary.
    /// </summary>
    public async Task<Result<ItineraryStopResponse>> AddStopAsync(
        Guid itineraryId,
        CreateItineraryStopRequest request,
        string authenticatedEditorId,
        CancellationToken cancellationToken = default)
    {
        var itinerary = await _repository.GetByIdAsync(
            itineraryId, cancellationToken);

        if (itinerary == null)
        {
            return Result<ItineraryStopResponse>.Failure(
                "Itinerary not found.");
        }

        if (!IsAuthorizedEditor(itinerary, authenticatedEditorId))
        {
            return Result<ItineraryStopResponse>.Failure(
                "You are not authorized to modify this itinerary.");
        }

        var result = itinerary.AddStop(
            request.Name,
            request.Description,
            request.Latitude,
            request.Longitude);

        if (result.IsFailure)
        {
            return Result<ItineraryStopResponse>.Failure(
                result.Errors);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ItineraryStopResponse>.Success(
            MapStopToResponse(result.Value!));
    }

    /// <summary>
    /// Updates an existing fixed stop in a draft itinerary.
    /// </summary>
    public async Task<Result<ItineraryStopResponse>> UpdateStopAsync(
        Guid itineraryId,
        Guid stopId,
        UpdateItineraryStopRequest request,
        string authenticatedEditorId,
        CancellationToken cancellationToken = default)
    {
        var itinerary = await _repository.GetByIdAsync(
            itineraryId, cancellationToken);

        if (itinerary == null)
        {
            return Result<ItineraryStopResponse>.Failure(
                "Itinerary not found.");
        }

        if (!IsAuthorizedEditor(itinerary, authenticatedEditorId))
        {
            return Result<ItineraryStopResponse>.Failure(
                "You are not authorized to modify this itinerary.");
        }

        var result = itinerary.UpdateStop(
            stopId,
            request.Name,
            request.Description,
            request.Latitude,
            request.Longitude);

        if (result.IsFailure)
        {
            return Result<ItineraryStopResponse>.Failure(
                result.Errors);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ItineraryStopResponse>.Success(
            MapStopToResponse(result.Value!));
    }

    /// <summary>
    /// Retrieves all fixed stops belonging to an itinerary.
    /// </summary>
    public async Task<Result<IEnumerable<ItineraryStopResponse>>> GetStopsAsync(
        Guid itineraryId,
        CancellationToken cancellationToken = default)
    {
        var itinerary = await _repository.GetByIdAsync(
            itineraryId, cancellationToken);

        if (itinerary == null)
        {
            return Result<IEnumerable<ItineraryStopResponse>>.Failure(
                "Itinerary not found.");
        }

        var stops = itinerary.GetOrderedStops()
            .Select(MapStopToResponse)
            .ToList();

        return Result<IEnumerable<ItineraryStopResponse>>.Success(
            stops);
    }

    /// <summary>
    /// Validates and saves a new sequence for fixed stops in a draft itinerary.
    /// </summary>
    public async Task<Result<IReadOnlyList<ItineraryStopResponse>>> ReorderStopsAsync(
        Guid itineraryId,
        ReorderItineraryStopsRequest request,
        string authenticatedEditorId,
        CancellationToken cancellationToken = default)
    {
        var itinerary = await _repository.GetByIdAsync(itineraryId, cancellationToken);
        if (itinerary == null)
            return Result<IReadOnlyList<ItineraryStopResponse>>.Failure("Itinerary not found.");

        if (!IsAuthorizedEditor(itinerary, authenticatedEditorId))
            return Result<IReadOnlyList<ItineraryStopResponse>>.Failure(
                "You are not authorized to modify this itinerary.");

        var result = itinerary.ReorderStops(request.StopIds);
        if (result.IsFailure)
            return Result<IReadOnlyList<ItineraryStopResponse>>.Failure(result.Errors);

        await _repository.SaveChangesAsync(cancellationToken);
        return Result<IReadOnlyList<ItineraryStopResponse>>.Success(
            result.Value!.Select(MapStopToResponse).ToList());
    }

    /// <summary>
    /// Derives the itinerary segments connecting consecutive stops in saved order.
    /// </summary>
    public async Task<Result<IReadOnlyList<ItinerarySegmentResponse>>> GetSegmentsAsync(
        Guid itineraryId,
        CancellationToken cancellationToken = default)
    {
        var itinerary = await _repository.GetByIdAsync(itineraryId, cancellationToken);
        if (itinerary == null)
            return Result<IReadOnlyList<ItinerarySegmentResponse>>.Failure("Itinerary not found.");

        var stops = itinerary.GetOrderedStops().ToList();
        var segments = stops.Zip(stops.Skip(1),
            (from, to) => new ItinerarySegmentResponse(from.Id, to.Id)).ToList();
        return Result<IReadOnlyList<ItinerarySegmentResponse>>.Success(segments);
    }

    /// <summary>
    /// Validates prerequisites and publishes a draft itinerary.
    /// </summary>
    public async Task<Result<ItineraryResponse>> PublishItineraryAsync(
        Guid id,
        string authenticatedEditorId,
        CancellationToken cancellationToken = default)
    {
        var itinerary = await _repository.GetByIdAsync(id, cancellationToken);
        if (itinerary == null)
            return Result<ItineraryResponse>.Failure("Itinerary not found.");

        if (!IsAuthorizedEditor(itinerary, authenticatedEditorId))
            return Result<ItineraryResponse>.Failure(
                "You are not authorized to publish this itinerary.");

        var publishResult = itinerary.Publish();
        if (publishResult.IsFailure)
            return Result<ItineraryResponse>.Failure(publishResult.Errors);

        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ItineraryResponse>.Success(
            ItineraryResponse.FromEntity(itinerary));
    }

    /// <summary>
    /// Checks whether the authenticated editor matches the responsible editor.
    /// </summary>
    private static bool IsAuthorizedEditor(
        Itinerary itinerary,
        string authenticatedEditorId)
    {
        return !string.IsNullOrWhiteSpace(authenticatedEditorId)
            && string.Equals(
                itinerary.ResponsibleEditorId,
                authenticatedEditorId,
                StringComparison.Ordinal);
    }

    /// <summary>
    /// Maps an ItineraryStop domain entity to its response DTO.
    /// </summary>
    private static ItineraryStopResponse MapStopToResponse(
        ItineraryStop stop)
    {
        return new ItineraryStopResponse
        {
            Id = stop.Id,
            ItineraryId = stop.ItineraryId,
            Name = stop.Name,
            Description = stop.Description,
            Latitude = stop.Latitude,
            Longitude = stop.Longitude,
            Position = stop.Position
        };
    }
}
