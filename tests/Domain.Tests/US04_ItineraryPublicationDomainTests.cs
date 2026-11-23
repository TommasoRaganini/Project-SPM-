using Domain.Common;
using Domain.Entities;
using Domain.States;
using FluentAssertions;
using Xunit;

namespace Domain.Tests;

public class US04_ItineraryPublicationDomainTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void AC01_AC05_Publish_WithFewerThanTwoStops_ShouldFailAndPreserveDraft(int stopCount)
    {
        // Arrange
        var itinerary = CreateDraft();
        for (int i = 0; i < stopCount; i++)
        {
            itinerary.AddStop($"Stop {i + 1}", $"Description {i + 1}", 45.0 + i, 9.0 + i);
        }

        // Act
        var result = itinerary.Publish();

        // Assert
        result.IsFailure.Should().BeTrue("AC01/AC05: An itinerary with fewer than two stops cannot be published");
        result.Errors.Should().Contain("An itinerary must have at least two fixed stops to be published.");
        itinerary.Status.Should().Be(ItineraryStatus.Draft);
        itinerary.State.Should().BeOfType<DraftState>();
        itinerary.State.IsVisibleInPublicCatalog.Should().BeFalse();
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public void AC01_AC02_Publish_WithAtLeastTwoStops_ShouldSucceedAndTransitionToPublished(int stopCount)
    {
        // Arrange
        var itinerary = CreateDraft();
        for (int i = 0; i < stopCount; i++)
        {
            itinerary.AddStop($"Stop {i + 1}", $"Description {i + 1}", 45.0 + i, 9.0 + i);
        }

        // Act
        var result = itinerary.Publish();

        // Assert
        result.IsSuccess.Should().BeTrue();
        itinerary.Status.Should().Be(ItineraryStatus.Published, "AC02: Status changes from Draft to Published");
        itinerary.State.Should().BeOfType<PublishedState>();
    }

    [Fact]
    public void AC03_Publish_WhenPublished_ShouldBeVisibleInPublicCatalog()
    {
        // Arrange
        var itinerary = CreateDraftWithTwoStops();

        // Act
        var result = itinerary.Publish();

        // Assert
        result.IsSuccess.Should().BeTrue();
        itinerary.State.IsVisibleInPublicCatalog.Should().BeTrue(
            "AC03: A published itinerary must be visible in the public catalog");
    }

    [Fact]
    public void AC04_PublishedItinerary_ShouldPreserveStopsAndTheirOrder()
    {
        // Arrange
        var itinerary = CreateDraft();
        var stop1 = itinerary.AddStop("First Stop", "Description 1", 45.4642, 9.1900).Value!;
        var stop2 = itinerary.AddStop("Second Stop", "Description 2", 45.4650, 9.1910).Value!;
        var stop3 = itinerary.AddStop("Third Stop", "Description 3", 45.4660, 9.1920).Value!;

        itinerary.ReorderStops(new[] { stop3.Id, stop1.Id, stop2.Id });

        // Act
        var result = itinerary.Publish();

        // Assert
        result.IsSuccess.Should().BeTrue();
        var orderedStops = itinerary.GetOrderedStops().ToList();
        orderedStops.Should().HaveCount(3);
        orderedStops[0].Id.Should().Be(stop3.Id);
        orderedStops[0].Position.Should().Be(1);
        orderedStops[0].Name.Should().Be("Third Stop");
        orderedStops[1].Id.Should().Be(stop1.Id);
        orderedStops[1].Position.Should().Be(2);
        orderedStops[1].Name.Should().Be("First Stop");
        orderedStops[2].Id.Should().Be(stop2.Id);
        orderedStops[2].Position.Should().Be(3);
        orderedStops[2].Name.Should().Be("Second Stop");
    }

    [Fact]
    public void AC04_PublishedItinerary_ShouldRejectStopModificationsAndReordering()
    {
        // Arrange
        var itinerary = CreateDraftWithTwoStops();
        var stops = itinerary.GetOrderedStops().ToList();
        itinerary.Publish().IsSuccess.Should().BeTrue();

        // Act & Assert 1: Cannot add stops
        var addResult = itinerary.AddStop("New Stop", "Desc", 45.0, 9.0);
        addResult.IsFailure.Should().BeTrue();
        addResult.Errors.Should().Contain("Only draft itineraries can be modified.");

        // Act & Assert 2: Cannot update stops
        var updateResult = itinerary.UpdateStop(stops[0].Id, "Updated Name", "Updated Desc", 45.1, 9.1);
        updateResult.IsFailure.Should().BeTrue();
        updateResult.Errors.Should().Contain("Only draft itineraries can be modified.");

        // Act & Assert 3: Cannot reorder stops
        var reorderResult = itinerary.ReorderStops(new[] { stops[1].Id, stops[0].Id });
        reorderResult.IsFailure.Should().BeTrue();
        reorderResult.Errors.Should().Contain("Only draft itineraries can be modified.");
    }

    [Fact]
    public void AC05_Publish_WhenAlreadyPublished_ShouldFail()
    {
        // Arrange
        var itinerary = CreateDraftWithTwoStops();
        var firstPublish = itinerary.Publish();
        firstPublish.IsSuccess.Should().BeTrue();

        // Act
        var secondPublish = itinerary.Publish();

        // Assert
        secondPublish.IsFailure.Should().BeTrue("AC05: Cannot publish an itinerary that is already published");
        secondPublish.Errors.Should().Contain("Only draft itineraries can be published.");
    }

    [Fact]
    public void AC06_Publish_ShouldKeepParticipationStatusClosed()
    {
        // Arrange
        var itinerary = CreateDraftWithTwoStops();
        itinerary.ParticipationStatus.Should().Be(ParticipationStatus.Closed);
        itinerary.ParticipationState.Should().BeOfType<ClosedParticipationState>();
        itinerary.ParticipationState.CanAcceptProposals.Should().BeFalse();

        // Act
        var result = itinerary.Publish();

        // Assert
        result.IsSuccess.Should().BeTrue();
        itinerary.ParticipationStatus.Should().Be(ParticipationStatus.Closed,
            "AC06: Publication does not automatically open the itinerary to participation by operators");
        itinerary.ParticipationState.Should().BeOfType<ClosedParticipationState>();
        itinerary.ParticipationState.CanAcceptProposals.Should().BeFalse(
            "AC06: Participation state must be Closed with proposals disabled");
    }

    [Fact]
    public void ParticipationState_TransitionToOpen_AndRestoreState_ShouldFunctionCorrectly()
    {
        // Arrange
        var itinerary = CreateDraftWithTwoStops();
        itinerary.Publish().IsSuccess.Should().BeTrue();

        // Act
        itinerary.TransitionParticipationTo(new OpenParticipationState());

        // Assert
        itinerary.ParticipationStatus.Should().Be(ParticipationStatus.Open);
        itinerary.ParticipationState.Should().BeOfType<OpenParticipationState>();
        itinerary.ParticipationState.CanAcceptProposals.Should().BeTrue();

        // Test RestoreState
        itinerary.RestoreState();
        itinerary.ParticipationState.Should().BeOfType<OpenParticipationState>();
        itinerary.ParticipationState.CanAcceptProposals.Should().BeTrue();
    }

    private static Itinerary CreateDraft() =>
        Itinerary.CreateDraft(
            "Tuscan Hills",
            "Landscape",
            "Scenic route through Val d'Orcia",
            "Tuscany, Italy",
            "editor-alpha").Value!;

    private static Itinerary CreateDraftWithTwoStops()
    {
        var itinerary = CreateDraft();
        itinerary.AddStop("Pienza", "Renaissance ideal city", 43.0768, 11.6787);
        itinerary.AddStop("San Quirico d'Orcia", "Historic village", 43.0583, 11.6033);
        return itinerary;
    }
}

