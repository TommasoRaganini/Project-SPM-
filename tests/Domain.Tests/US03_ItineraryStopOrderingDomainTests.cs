using Domain.Entities;
using Domain.States;
using FluentAssertions;
using Xunit;

namespace Domain.Tests;

public class US03_ItineraryStopOrderingDomainTests
{
    [Fact]
    public void AC01_AC05_ReorderStops_ShouldChangeOnlyPositionsAndAppendNewStops()
    {
        var itinerary = CreateDraft();
        var first = itinerary.AddStop("First", "First description", 41, 12).Value!;
        var second = itinerary.AddStop("Second", "Second description", 42, 13).Value!;

        var result = itinerary.ReorderStops(new[] { second.Id, first.Id });
        var third = itinerary.AddStop("Third", "Third description", 43, 14).Value!;

        result.IsSuccess.Should().BeTrue();
        itinerary.GetOrderedStops().Should().Equal(second, first, third);
        itinerary.GetOrderedStops().Select(stop => stop.Position).Should().Equal(1, 2, 3);
        first.Name.Should().Be("First");
        first.Description.Should().Be("First description");
        first.Latitude.Should().Be(41);
        first.Longitude.Should().Be(12);
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("missing")]
    [InlineData("foreign")]
    public void AC03_ReorderStops_ShouldRejectInvalidSequenceWithoutChangingPositions(string invalidSequence)
    {
        var itinerary = CreateDraft();
        var first = itinerary.AddStop("First", "First description", 41, 12).Value!;
        var second = itinerary.AddStop("Second", "Second description", 42, 13).Value!;
        var foreign = CreateDraft().AddStop("Foreign", "Other itinerary", 43, 14).Value!;
        var stopIds = invalidSequence switch
        {
            "duplicate" => new[] { first.Id, first.Id },
            "missing" => new[] { second.Id },
            _ => new[] { second.Id, foreign.Id }
        };

        var result = itinerary.ReorderStops(stopIds);

        result.IsFailure.Should().BeTrue();
        itinerary.GetOrderedStops().Should().Equal(first, second);
        itinerary.GetOrderedStops().Select(stop => stop.Position).Should().Equal(1, 2);
    }

    [Fact]
    public void AC01_ReorderStops_ShouldRejectPublishedItinerary()
    {
        var itinerary = CreateDraft();
        var first = itinerary.AddStop("First", "First description", 41, 12).Value!;
        var second = itinerary.AddStop("Second", "Second description", 42, 13).Value!;
        itinerary.TransitionTo(new PublishedState());

        var result = itinerary.ReorderStops(new[] { second.Id, first.Id });

        result.IsFailure.Should().BeTrue();
        itinerary.GetOrderedStops().Should().Equal(first, second);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void AC01_ReorderStops_WithSmallNumberOfStops_ShouldSucceed(int stopCount)
    {
        var itinerary = CreateDraft();
        var stopIds = new List<Guid>();
        if (stopCount == 1)
        {
            var stop = itinerary.AddStop("Only", "Description", 41, 12).Value!;
            stopIds.Add(stop.Id);
        }

        var result = itinerary.ReorderStops(stopIds);

        result.IsSuccess.Should().BeTrue();
        itinerary.GetOrderedStops().Count().Should().Be(stopCount);
    }

    private static Itinerary CreateDraft() =>
        Itinerary.CreateDraft("Route", "Nature", "A trail", "Italy", "editor-alpha").Value!;
}
