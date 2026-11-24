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

    // ============================================================
    // US01: ITINERARY MANAGEMENT
    // ============================================================

    public async Task<Result<ItineraryResponse>> CreateDraftAsync(
        CreateItineraryRequest request,
        string authenticatedEditorId,
        CancellationToken cancellationToken = default)
    {
        // AC01 & AC04: Domain creation and validation
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

        // AC03: Persist itinerary
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
        // AC05: Retrieve existing itinerary
        var itinerary = await _repository.GetByIdAsync(
            id, cancellationToken);

        if (itinerary == null)
        {
            return Result<ItineraryResponse>.Failure(
                "Itinerary not found.");
        }

        // Verify editor authorization
        if (itinerary.Status == Domain.States.ItineraryStatus.Draft && !IsAuthorizedEditor(itinerary, authenticatedEditorId))
        {
            return Result<ItineraryResponse>.Failure(
                "You are not authorized to access this itinerary.");
        }

        return Result<ItineraryResponse>.Success(
            ItineraryResponse.FromEntity(itinerary));
    }

    public async Task<IReadOnlyList<ItineraryResponse>> GetPublicCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        // AC02: Public catalog excludes drafts
        var list = await _repository.GetPublicCatalogAsync(
            cancellationToken);

        return list.Select(ItineraryResponse.FromEntity).ToList();
    }


    // ============================================================
    // NEW USER STORY: ITINERARY STOPS MANAGEMENT
    // ============================================================

    /// <summary>
    /// AC01, AC04, AC05: Adds a new geolocated fixed stop.
    /// </summary>
    public async Task<Result<ItineraryStopResponse>> AddStopAsync(
        Guid itineraryId,
        CreateItineraryStopRequest request,
        string authenticatedEditorId,
        CancellationToken cancellationToken = default)
    {
        // Retrieve itinerary with its stops
        var itinerary = await _repository.GetByIdAsync(
            itineraryId, cancellationToken);

        if (itinerary == null)
        {
            return Result<ItineraryStopResponse>.Failure(
                "Itinerary not found.");
        }

        // AC05: Verify editor authorization
        if (!IsAuthorizedEditor(itinerary, authenticatedEditorId))
        {
            return Result<ItineraryStopResponse>.Failure(
                "You are not authorized to modify this itinerary.");
        }

        // AC01, AC04: Domain creates and validates the stop
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

        // Persist the new stop
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ItineraryStopResponse>.Success(
            MapStopToResponse(result.Value!));
    }


    /// <summary>
    /// AC03, AC04, AC05: Updates an existing fixed stop.
    /// </summary>
    public async Task<Result<ItineraryStopResponse>> UpdateStopAsync(
        Guid itineraryId,
        Guid stopId,
        UpdateItineraryStopRequest request,
        string authenticatedEditorId,
        CancellationToken cancellationToken = default)
    {
        // Retrieve itinerary with its stops
        var itinerary = await _repository.GetByIdAsync(
            itineraryId, cancellationToken);

        if (itinerary == null)
        {
            return Result<ItineraryStopResponse>.Failure(
                "Itinerary not found.");
        }

        // AC05: Verify editor authorization
        if (!IsAuthorizedEditor(itinerary, authenticatedEditorId))
        {
            return Result<ItineraryStopResponse>.Failure(
                "You are not authorized to modify this itinerary.");
        }

        // AC03, AC04: Domain updates and validates the stop
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

        // Persist changes
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ItineraryStopResponse>.Success(
            MapStopToResponse(result.Value!));
    }


    /// <summary>
    /// AC02: Retrieves all fixed stops belonging to an itinerary.
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


    // ============================================================
    // US03: ITINERARY STOP ORDERING
    // ============================================================

    /// <summary>
    /// AC01, AC02, AC03, AC05: Validates and saves a new sequence for fixed stops.
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
    /// AC04: Derives the itinerary segments from the saved stop order.
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

    // ============================================================
    // US04: ITINERARY PUBLICATION
    // ============================================================

    /// <summary>
    /// AC01 - AC06: Validates and publishes a draft itinerary.
    /// Requires authorization of the responsible editor.
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

    // ============================================================
    // PRIVATE HELPER METHODS
    // ============================================================

    /// <summary>
    /// Checks whether the authenticated editor is responsible
    /// for the requested itinerary.
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
    /// Maps an ItineraryStop Domain entity to its Response DTO.
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
