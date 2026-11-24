using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Domain.Common;

namespace Application.Interfaces;

public interface IItineraryService
{
    /// <summary>
    /// Creates a new draft itinerary associated with the authenticated editor.
    /// </summary>
    Task<Result<ItineraryResponse>> CreateDraftAsync(
        CreateItineraryRequest request,
        string authenticatedEditorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a draft itinerary by its identifier, verifying editor authorization.
    /// </summary>
    Task<Result<ItineraryResponse>> GetDraftByIdAsync(
        Guid id,
        string authenticatedEditorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all published itineraries for the public catalog.
    /// </summary>
    Task<PublicCatalogResponse> GetPublicCatalogAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the public details of a published itinerary, including ordered stops.
    /// </summary>
    Task<Result<PublicItineraryDetailResponse>> GetPublishedItineraryByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new geolocated fixed stop to an existing draft itinerary.
    /// </summary>
    Task<Result<ItineraryStopResponse>> AddStopAsync(
        Guid itineraryId,
        CreateItineraryStopRequest request,
        string authenticatedEditorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing geolocated fixed stop in a draft itinerary.
    /// </summary>
    Task<Result<ItineraryStopResponse>> UpdateStopAsync(
        Guid itineraryId,
        Guid stopId,
        UpdateItineraryStopRequest request,
        string authenticatedEditorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all fixed stops for the specified itinerary.
    /// </summary>
    Task<Result<IEnumerable<ItineraryStopResponse>>> GetStopsAsync(
        Guid itineraryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reorders the fixed stops of a draft itinerary.
    /// </summary>
    Task<Result<IReadOnlyList<ItineraryStopResponse>>> ReorderStopsAsync(
        Guid itineraryId,
        ReorderItineraryStopsRequest request,
        string authenticatedEditorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the consecutive segments formed by the ordered stops.
    /// </summary>
    Task<Result<IReadOnlyList<ItinerarySegmentResponse>>> GetSegmentsAsync(
        Guid itineraryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a draft itinerary once prerequisites are satisfied.
    /// </summary>
    Task<Result<ItineraryResponse>> PublishItineraryAsync(
        Guid id,
        string authenticatedEditorId,
        CancellationToken cancellationToken = default);
}
