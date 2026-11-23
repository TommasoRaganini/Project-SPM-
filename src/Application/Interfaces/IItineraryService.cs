using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Domain.Common;

namespace Application.Interfaces;

public interface IItineraryService
{
    // US01: Itinerary management

    Task<Result<ItineraryResponse>> CreateDraftAsync(
        CreateItineraryRequest request,
        string authenticatedEditorId,
        CancellationToken cancellationToken = default);

    Task<Result<ItineraryResponse>> GetDraftByIdAsync(
        Guid id,
        string authenticatedEditorId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ItineraryResponse>> GetPublicCatalogAsync(
        CancellationToken cancellationToken = default);


    // New User Story: Itinerary Stops management

    // AC01, AC04, AC05: Add a fixed stop
    Task<Result<ItineraryStopResponse>> AddStopAsync(
        Guid itineraryId,
        CreateItineraryStopRequest request,
        string authenticatedEditorId,
        CancellationToken cancellationToken = default);

    // AC03, AC04, AC05: Update an existing stop
    Task<Result<ItineraryStopResponse>> UpdateStopAsync(
        Guid itineraryId,
        Guid stopId,
        UpdateItineraryStopRequest request,
        string authenticatedEditorId,
        CancellationToken cancellationToken = default);

    // AC02: Retrieve all stops belonging to an itinerary
    Task<Result<IEnumerable<ItineraryStopResponse>>> GetStopsAsync(
        Guid itineraryId,
        CancellationToken cancellationToken = default);

    // US03: Itinerary stop ordering

    // AC01, AC02, AC03, AC05: Replace the order of fixed stops.
    Task<Result<IReadOnlyList<ItineraryStopResponse>>> ReorderStopsAsync(
        Guid itineraryId,
        ReorderItineraryStopsRequest request,
        string authenticatedEditorId,
        CancellationToken cancellationToken = default);

    // AC04: Retrieve segments between consecutive stops.
    Task<Result<IReadOnlyList<ItinerarySegmentResponse>>> GetSegmentsAsync(
        Guid itineraryId,
        CancellationToken cancellationToken = default);
}
