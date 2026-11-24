using System.Net;
using System.Net.Http.Json;
using Api.IntegrationTests.Fixtures;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using FluentAssertions;
using Xunit;

namespace Api.IntegrationTests;

[Collection("IntegrationTests")]
public class ItineraryEditorWorkflowIntegrationTests
{
    private readonly HttpClient _client;

    public ItineraryEditorWorkflowIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CompleteEditorWorkflow_FromDraftToPublication_ShouldSucceedAndEnforceRules()
    {
        var editorId = "editor-integration-01";
        var unauthorizedEditorId = "editor-hacker";

        // 1. Create a draft itinerary
        var createRequest = new CreateItineraryRequest(
            "Val d'Orcia Heritage",
            "Landscape & Culture",
            "Scenic trail across hills and historical towns",
            "Siena, Tuscany"
        );

        using var createMsg = new HttpRequestMessage(HttpMethod.Post, "/api/itineraries");
        createMsg.Headers.Add("X-Editor-Id", editorId);
        createMsg.Content = JsonContent.Create(createRequest);

        var createResponse = await _client.SendAsync(createMsg);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var itinerary = await createResponse.Content.ReadFromJsonAsync<ItineraryResponse>();
        itinerary.Should().NotBeNull();
        var itineraryId = itinerary!.Id;
        itinerary.Status.Should().Be("Draft");
        itinerary.IsVisibleInPublicCatalog.Should().BeFalse();

        // 2. Reject creation when required fields are missing
        var invalidDraft = new CreateItineraryRequest("", "", "", "");
        using var invalidMsg = new HttpRequestMessage(HttpMethod.Post, "/api/itineraries");
        invalidMsg.Headers.Add("X-Editor-Id", editorId);
        invalidMsg.Content = JsonContent.Create(invalidDraft);

        var invalidResponse = await _client.SendAsync(invalidMsg);
        invalidResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // 3. Add first stop as authorized editor
        var stop1Request = new CreateItineraryStopRequest(
            "Pienza",
            "Renaissance ideal square",
            43.0768,
            11.6787
        );
        using var stop1Msg = new HttpRequestMessage(HttpMethod.Post, $"/api/itineraries/{itineraryId}/stops");
        stop1Msg.Headers.Add("X-Editor-Id", editorId);
        stop1Msg.Content = JsonContent.Create(stop1Request);

        var stop1Response = await _client.SendAsync(stop1Msg);
        stop1Response.StatusCode.Should().Be(HttpStatusCode.Created);
        var stop1 = await stop1Response.Content.ReadFromJsonAsync<ItineraryStopResponse>();
        stop1!.Position.Should().Be(1);

        // 4. Reject stop creation when coordinates are out of valid bounds
        var invalidCoordStop = new CreateItineraryStopRequest("Invalid", "Out of bounds", 150.0, 300.0);
        using var invalidCoordMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/itineraries/{itineraryId}/stops");
        invalidCoordMsg.Headers.Add("X-Editor-Id", editorId);
        invalidCoordMsg.Content = JsonContent.Create(invalidCoordStop);

        var invalidCoordResponse = await _client.SendAsync(invalidCoordMsg);
        invalidCoordResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // 5. Reject stop creation when editor is not authorized
        var stop2Request = new CreateItineraryStopRequest(
            "Monticchiello",
            "Medieval fortified village",
            43.0683,
            11.7225
        );
        using var unauthStopMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/itineraries/{itineraryId}/stops");
        unauthStopMsg.Headers.Add("X-Editor-Id", unauthorizedEditorId);
        unauthStopMsg.Content = JsonContent.Create(stop2Request);

        var unauthStopResponse = await _client.SendAsync(unauthStopMsg);
        unauthStopResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 6. Add second stop as authorized editor
        using var stop2Msg = new HttpRequestMessage(HttpMethod.Post, $"/api/itineraries/{itineraryId}/stops");
        stop2Msg.Headers.Add("X-Editor-Id", editorId);
        stop2Msg.Content = JsonContent.Create(stop2Request);

        var stop2Response = await _client.SendAsync(stop2Msg);
        stop2Response.StatusCode.Should().Be(HttpStatusCode.Created);
        var stop2 = await stop2Response.Content.ReadFromJsonAsync<ItineraryStopResponse>();
        stop2!.Position.Should().Be(2);

        // 7. Reorder stops
        var reorderRequest = new ReorderItineraryStopsRequest(new[] { stop2.Id, stop1.Id });
        using var reorderMsg = new HttpRequestMessage(HttpMethod.Put, $"/api/itineraries/{itineraryId}/stops/order");
        reorderMsg.Headers.Add("X-Editor-Id", editorId);
        reorderMsg.Content = JsonContent.Create(reorderRequest);

        var reorderResponse = await _client.SendAsync(reorderMsg);
        reorderResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var reorderedStops = await reorderResponse.Content.ReadFromJsonAsync<List<ItineraryStopResponse>>();
        reorderedStops![0].Id.Should().Be(stop2.Id);
        reorderedStops[0].Position.Should().Be(1);
        reorderedStops[1].Id.Should().Be(stop1.Id);
        reorderedStops[1].Position.Should().Be(2);

        // 8. Reject publication from unauthorized editor
        using var unauthPublishMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/itineraries/{itineraryId}/publish");
        unauthPublishMsg.Headers.Add("X-Editor-Id", unauthorizedEditorId);

        var unauthPublishResponse = await _client.SendAsync(unauthPublishMsg);
        unauthPublishResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 9. Authorized editor publishes itinerary
        using var publishMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/itineraries/{itineraryId}/publish");
        publishMsg.Headers.Add("X-Editor-Id", editorId);

        var publishResponse = await _client.SendAsync(publishMsg);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var published = await publishResponse.Content.ReadFromJsonAsync<ItineraryResponse>();
        published!.Status.Should().Be("Published");
        published.IsVisibleInPublicCatalog.Should().BeTrue();
        published.ParticipationStatus.Should().Be("Closed");

        // 10. Modifying stops on a published itinerary is prevented
        var newStopAttempt = new CreateItineraryStopRequest("Post-Publish Stop", "Should fail", 43.1, 11.5);
        using var blockedStopMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/itineraries/{itineraryId}/stops");
        blockedStopMsg.Headers.Add("X-Editor-Id", editorId);
        blockedStopMsg.Content = JsonContent.Create(newStopAttempt);

        var blockedStopResponse = await _client.SendAsync(blockedStopMsg);
        blockedStopResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
