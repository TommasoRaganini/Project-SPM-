using Application.DTOs.Responses;
using Application.Services;
using Domain.Entities;
using Domain.States;
using FluentAssertions;
using Xunit;

namespace Domain.Tests;

public class US04_ItineraryPublicationServiceTests
{
    [Fact]
    public async Task AC01_AC05_PublishItineraryAsync_WithLessThanTwoStops_ShouldFailAndNotPersist()
    {
        // Arrange
        var repository = new InMemoryItineraryRepository();
        var itinerary = Itinerary.CreateDraft(
            "Amalfi Walk", "Nature", "Cliffside paths", "Campania", "editor-01").Value!;
        itinerary.AddStop("Amalfi Center", "Historic town center", 40.6340, 14.6027);
        await repository.AddAsync(itinerary);

        var service = new ItineraryService(repository);

        // Act
        var result = await service.PublishItineraryAsync(itinerary.Id, "editor-01");

        // Assert
        result.IsFailure.Should().BeTrue("AC01/AC05: An itinerary with only 1 stop cannot be published");
        result.Errors.Should().Contain("An itinerary must have at least two fixed stops to be published.");
        repository.SaveCount.Should().Be(0, "AC05: Database changes must not be committed on failure");

        var stored = await repository.GetByIdAsync(itinerary.Id);
        stored!.Status.Should().Be(ItineraryStatus.Draft);
    }

    [Fact]
    public async Task AC02_AC03_PublishItineraryAsync_WhenValid_ShouldPersistPublished_AndAppearInPublicCatalog()
    {
        // Arrange
        var (service, repository, itinerary) = await CreateServiceSetupAsync();

        // Act
        var result = await service.PublishItineraryAsync(itinerary.Id, "editor-alpha");

        // Assert
        result.IsSuccess.Should().BeTrue();
        repository.SaveCount.Should().Be(1);

        var response = result.Value!;
        response.Id.Should().Be(itinerary.Id);
        response.Status.Should().Be("Published", "AC02: Status changes to Published");
        response.IsVisibleInPublicCatalog.Should().BeTrue("AC03: Published itinerary is visible in public catalog");
        response.ParticipationStatus.Should().Be("Closed", "AC06: Participation remains Closed");

        // AC03: Check public catalog
        var publicCatalog = await service.GetPublicCatalogAsync();
        publicCatalog.Items.Should().ContainSingle(i => i.Id == itinerary.Id);
    }

    [Fact]
    public async Task AC04_PublishItineraryAsync_ShouldPreserveExactStopsAndOrder()
    {
        // Arrange
        var (service, repository, itinerary) = await CreateServiceSetupAsync();

        // Act
        var publishResult = await service.PublishItineraryAsync(itinerary.Id, "editor-alpha");
        publishResult.IsSuccess.Should().BeTrue();

        // Retrieve stops as a tourist / consumer
        var stopsResult = await service.GetStopsAsync(itinerary.Id);

        // Assert
        stopsResult.IsSuccess.Should().BeTrue();
        var stops = stopsResult.Value!.ToList();
        stops.Should().HaveCount(3);
        stops[0].Name.Should().Be("Stop Alpha");
        stops[0].Position.Should().Be(1);
        stops[1].Name.Should().Be("Stop Beta");
        stops[1].Position.Should().Be(2);
        stops[2].Name.Should().Be("Stop Gamma");
        stops[2].Position.Should().Be(3);
    }

    [Theory]
    [InlineData("unauthorized-editor")]
    [InlineData("")]
    public async Task AC05_PublishItineraryAsync_WhenEditorUnauthorized_ShouldReturnFailureAndNotPersist(string editorId)
    {
        // Arrange
        var (service, repository, itinerary) = await CreateServiceSetupAsync();

        // Act
        var result = await service.PublishItineraryAsync(itinerary.Id, editorId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("You are not authorized to publish this itinerary.");
        repository.SaveCount.Should().Be(0);

        var stored = await repository.GetByIdAsync(itinerary.Id);
        stored!.Status.Should().Be(ItineraryStatus.Draft);
    }

    [Fact]
    public async Task AC05_PublishItineraryAsync_WhenItineraryNotFound_ShouldReturnFailure()
    {
        // Arrange
        var repository = new InMemoryItineraryRepository();
        var service = new ItineraryService(repository);
        var randomId = Guid.NewGuid();

        // Act
        var result = await service.PublishItineraryAsync(randomId, "editor-alpha");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Itinerary not found.");
        repository.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task AC05_PublishItineraryAsync_WhenAlreadyPublished_ShouldReturnFailureAndNotPersist()
    {
        // Arrange
        var (service, repository, itinerary) = await CreateServiceSetupAsync();
        var firstPublish = await service.PublishItineraryAsync(itinerary.Id, "editor-alpha");
        firstPublish.IsSuccess.Should().BeTrue();
        var saveCountAfterFirst = repository.SaveCount;

        // Act
        var secondPublish = await service.PublishItineraryAsync(itinerary.Id, "editor-alpha");

        // Assert
        secondPublish.IsFailure.Should().BeTrue();
        secondPublish.Errors.Should().Contain("Only draft itineraries can be published.");
        repository.SaveCount.Should().Be(saveCountAfterFirst);
    }

    [Fact]
    public async Task AC06_PublishItineraryAsync_ShouldNotOpenParticipationToOperators()
    {
        // Arrange
        var (service, repository, itinerary) = await CreateServiceSetupAsync();

        // Act
        var result = await service.PublishItineraryAsync(itinerary.Id, "editor-alpha");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ParticipationStatus.Should().Be("Closed",
            "AC06: Publication does not automatically open the itinerary to participation by operators");

        var stored = await repository.GetByIdAsync(itinerary.Id);
        stored!.ParticipationStatus.Should().Be(ParticipationStatus.Closed);
        stored.ParticipationState.Should().BeOfType<ClosedParticipationState>();
        stored.ParticipationState.CanAcceptProposals.Should().BeFalse();
    }

    [Fact]
    public async Task PublicBrowsing_GetDraftByIdAsync_WhenPublished_ShouldAllowViewingWithoutEditorHeader()
    {
        // Arrange
        var (service, repository, itinerary) = await CreateServiceSetupAsync();
        await service.PublishItineraryAsync(itinerary.Id, "editor-alpha");

        // Act (Tourist visits published itinerary without providing editor identity)
        var result = await service.GetDraftByIdAsync(itinerary.Id, authenticatedEditorId: string.Empty);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Published");
        result.Value.IsVisibleInPublicCatalog.Should().BeTrue();
    }

    private static async Task<(ItineraryService, InMemoryItineraryRepository, Itinerary)> CreateServiceSetupAsync()
    {
        var repository = new InMemoryItineraryRepository();
        var itinerary = Itinerary.CreateDraft(
            "Lombard Lakes",
            "Nature & Culture",
            "Scenic route across Lake Como and surroundings",
            "Lombardy, Italy",
            "editor-alpha").Value!;

        itinerary.AddStop("Stop Alpha", "Description Alpha", 45.9, 9.2);
        itinerary.AddStop("Stop Beta", "Description Beta", 45.95, 9.25);
        itinerary.AddStop("Stop Gamma", "Description Gamma", 46.0, 9.3);

        await repository.AddAsync(itinerary);
        return (new ItineraryService(repository), repository, itinerary);
    }
}

