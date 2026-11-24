using System.Net;
using System.Net.Http.Json;
using Api.IntegrationTests.Fixtures;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using FluentAssertions;
using Xunit;

namespace Api.IntegrationTests;

[Collection("IntegrationTests")]
public class PublicCatalogIntegrationTests
{
    private readonly HttpClient _client;

    public PublicCatalogIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PublicCatalog_WhenEmpty_ShouldReturnUnderstandableMessage()
    {
        // When no itineraries are published, catalog returns 200 with an informative message
        var response = await _client.GetAsync("/api/itineraries/public-catalog");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var catalog = await response.Content.ReadFromJsonAsync<PublicCatalogResponse>();
        catalog.Should().NotBeNull();
        if (catalog!.Items.Count == 0)
        {
            catalog.Message.Should().Be("No published itineraries available at this time.");
        }
    }

    [Fact]
    public async Task TouristBrowsing_ShouldAccessPublishedItineraries_AndBlockDrafts()
    {
        var editorId = "editor-tourist-test";

        // 1. Create a draft itinerary
        var createRequest = new CreateItineraryRequest(
            "Laghi Briantei",
            "Nature & Water",
            "Cycling scenic trail around lakes",
            "Brianza, Lombardy"
        );
        using var createMsg = new HttpRequestMessage(HttpMethod.Post, "/api/itineraries");
        createMsg.Headers.Add("X-Editor-Id", editorId);
        createMsg.Content = JsonContent.Create(createRequest);

        var createResponse = await _client.SendAsync(createMsg);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ItineraryResponse>();
        var itineraryId = created!.Id;

        // 2. Drafts must not be accessible through the public catalog detail endpoint
        var draftPublicResponse = await _client.GetAsync($"/api/itineraries/public-catalog/{itineraryId}");
        draftPublicResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // 3. Add 2 stops and publish
        var stop1 = new CreateItineraryStopRequest("Lago di Annone", "Waterfront promenade", 45.8167, 9.3500);
        using var stop1Msg = new HttpRequestMessage(HttpMethod.Post, $"/api/itineraries/{itineraryId}/stops");
        stop1Msg.Headers.Add("X-Editor-Id", editorId);
        stop1Msg.Content = JsonContent.Create(stop1);
        await _client.SendAsync(stop1Msg);

        var stop2 = new CreateItineraryStopRequest("Lago di Pusiano", "Island of cypresses", 45.8167, 9.2833);
        using var stop2Msg = new HttpRequestMessage(HttpMethod.Post, $"/api/itineraries/{itineraryId}/stops");
        stop2Msg.Headers.Add("X-Editor-Id", editorId);
        stop2Msg.Content = JsonContent.Create(stop2);
        await _client.SendAsync(stop2Msg);

        // Publish itinerary
        using var pubMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/itineraries/{itineraryId}/publish");
        pubMsg.Headers.Add("X-Editor-Id", editorId);
        var pubResponse = await _client.SendAsync(pubMsg);
        pubResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Tourist browses public catalog without authentication
        var catalogResponse = await _client.GetAsync("/api/itineraries/public-catalog");
        catalogResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var catalog = await catalogResponse.Content.ReadFromJsonAsync<PublicCatalogResponse>();
        catalog!.Items.Should().Contain(i => i.Id == itineraryId);
        var item = catalog.Items.First(i => i.Id == itineraryId);
        item.Title.Should().Be("Laghi Briantei");
        item.Theme.Should().Be("Nature & Water");
        item.Description.Should().Be("Cycling scenic trail around lakes");

        // 5. Tourist opens itinerary and receives unified detail with map coordinates
        var detailResponse = await _client.GetAsync($"/api/itineraries/public-catalog/{itineraryId}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await detailResponse.Content.ReadFromJsonAsync<PublicItineraryDetailResponse>();
        detail.Should().NotBeNull();
        detail!.Id.Should().Be(itineraryId);
        detail.Title.Should().Be("Laghi Briantei");
        detail.Stops.Should().HaveCount(2);
        detail.Stops[0].Position.Should().Be(1);
        detail.Stops[0].Latitude.Should().Be(45.8167);
        detail.Stops[0].Longitude.Should().Be(9.3500);
        detail.Stops[1].Position.Should().Be(2);
        detail.Stops[1].Latitude.Should().Be(45.8167);
        detail.Stops[1].Longitude.Should().Be(9.2833);
    }
}
