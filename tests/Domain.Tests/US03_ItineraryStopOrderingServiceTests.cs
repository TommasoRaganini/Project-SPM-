using Application.DTOs.Responses;
using Application.Services;
using Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Domain.Tests;

public class US03_ItineraryStopOrderingServiceTests
{
    [Fact]
    public async Task AC02_AC04_ReorderStopsAsync_ShouldSaveOrderAndReturnConsecutiveSegments()
    {
        var (service, repository, itinerary) = await CreateSetupAsync();
        var originalStops = itinerary.GetOrderedStops().ToArray();
        var (first, second, third) = (originalStops[0], originalStops[1], originalStops[2]);

        var result = await service.ReorderStopsAsync(
            itinerary.Id, new(new[] { first.Id, third.Id, second.Id }), "editor-alpha");
        var stops = await service.GetStopsAsync(itinerary.Id);
        var segments = await service.GetSegmentsAsync(itinerary.Id);

        result.IsSuccess.Should().BeTrue();
        repository.SaveCount.Should().Be(1);
        stops.Value!.Select(stop => stop.Id).Should().Equal(first.Id, third.Id, second.Id);
        segments.Value.Should().Equal(
            new ItinerarySegmentResponse(first.Id, third.Id),
            new ItinerarySegmentResponse(third.Id, second.Id));
    }

    [Theory]
    [InlineData("editor-alpha", false)]
    [InlineData("another-editor", true)]
    public async Task ReorderStopsAsync_ShouldNotSaveRejectedOrder(string editor, bool validSequence)
    {
        var (service, repository, itinerary) = await CreateSetupAsync();
        var originalOrder = itinerary.GetOrderedStops().Select(stop => stop.Id).ToArray();
        var requestedOrder = validSequence ? originalOrder.Reverse().ToArray() : new[] { originalOrder[0] };

        var result = await service.ReorderStopsAsync(itinerary.Id, new(requestedOrder), editor);

        result.IsFailure.Should().BeTrue();
        repository.SaveCount.Should().Be(0);
        itinerary.GetOrderedStops().Select(stop => stop.Id).Should().Equal(originalOrder);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task AC04_GetSegmentsAsync_WithLessThanTwoStops_ShouldReturnEmptySegmentsList(int stopCount)
    {
        var repository = new InMemoryItineraryRepository();
        var itinerary = Itinerary.CreateDraft("Route", "Nature", "A trail", "Italy", "editor-alpha").Value!;
        if (stopCount == 1)
        {
            itinerary.AddStop("First", "First description", 41, 12);
        }
        await repository.AddAsync(itinerary);
        var service = new ItineraryService(repository);

        var segments = await service.GetSegmentsAsync(itinerary.Id);

        segments.IsSuccess.Should().BeTrue();
        segments.Value.Should().BeEmpty();
    }

    [Theory]
    [InlineData("reorder")]
    [InlineData("segments")]
    public async Task ServiceOperations_WhenItineraryNotFound_ShouldReturnFailure(string operation)
    {
        var repository = new InMemoryItineraryRepository();
        var service = new ItineraryService(repository);
        var missingId = Guid.NewGuid();

        var errors = operation switch
        {
            "reorder" => (await service.ReorderStopsAsync(missingId, new(new[] { Guid.NewGuid() }), "editor-alpha")).Errors,
            _ => (await service.GetSegmentsAsync(missingId)).Errors
        };

        errors.Should().Contain("Itinerary not found.");
        repository.SaveCount.Should().Be(0);
    }

    private static async Task<(ItineraryService, InMemoryItineraryRepository, Itinerary)> CreateSetupAsync()
    {
        var repository = new InMemoryItineraryRepository();
        var itinerary = Itinerary.CreateDraft("Route", "Nature", "A trail", "Italy", "editor-alpha").Value!;
        itinerary.AddStop("First", "First description", 41, 12);
        itinerary.AddStop("Second", "Second description", 42, 13);
        itinerary.AddStop("Third", "Third description", 43, 14);
        await repository.AddAsync(itinerary);
        return (new ItineraryService(repository), repository, itinerary);
    }
}
