using Application.Services;
using Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Domain.Tests;

public class US05_PublicCatalogServiceTests
{
    [Fact]
    public async Task AC01_GetPublicCatalogAsync_ShouldReturnPublishedItinerariesWithoutAuthentication()
    {
        // Arrange: create an in-memory repository and a published itinerary (at least 2 stops required)
        var repository = new InMemoryItineraryRepository();
        var service = new ItineraryService(repository);

        var itinerary = Itinerary.CreateDraft(
            "Sapori della Toscana",
            "Enogastronomia",
            "Percorso tra cantine e borghi",
            "Toscana",
            "editor-alpha").Value!;

        itinerary.AddStop("Cantina A", "Prima tappa", 43.467, 11.316);
        itinerary.AddStop("Borgo B", "Seconda tappa", 43.512, 11.298);
        itinerary.Publish();

        await repository.AddAsync(itinerary);

        // Act: fetch the public catalog without any editorId (no authentication)
        var catalog = await service.GetPublicCatalogAsync(CancellationToken.None);

        // Assert: the published itinerary is returned in the public catalog
        catalog.Items.Should().ContainSingle(item => item.Id == itinerary.Id);
    }

    [Fact]
    public async Task AC02_GetPublicCatalogAsync_ShouldReturnTitleThemeAndDescriptionForEachItinerary()
    {
        // Arrange: create and publish an itinerary with title, theme, and description
        var repository = new InMemoryItineraryRepository();
        var service = new ItineraryService(repository);

        var itinerary = Itinerary.CreateDraft(
            "Sapori della Toscana",
            "Enogastronomia",
            "Percorso tra cantine storiche e borghi medievali",
            "Toscana",
            "editor-alpha").Value!;

        itinerary.AddStop("Cantina A", "Prima tappa", 43.467, 11.316);
        itinerary.AddStop("Borgo B", "Seconda tappa", 43.512, 11.298);
        itinerary.Publish();

        await repository.AddAsync(itinerary);

        // Act: fetch the public catalog
        var catalog = await service.GetPublicCatalogAsync(CancellationToken.None);

        // Assert: title, theme, and description are returned for the itinerary
        var item = catalog.Items.Should().ContainSingle(i => i.Id == itinerary.Id).Subject;
        item.Title.Should().Be("Sapori della Toscana");
        item.Theme.Should().Be("Enogastronomia");
        item.Description.Should().Be("Percorso tra cantine storiche e borghi medievali");
    }

    [Fact]
    public async Task AC03_OpeningPublishedItinerary_ShouldReturnDescriptionOrderedStopsAndMapCoordinates()
    {
        // Arrange: create an itinerary, add 2 stops, reverse their order, and publish it
        var repository = new InMemoryItineraryRepository();
        var service = new ItineraryService(repository);

        var itinerary = Itinerary.CreateDraft(
            "Sapori della Toscana",
            "Enogastronomia",
            "Percorso tra cantine storiche e borghi medievali",
            "Toscana",
            "editor-alpha").Value!;

        var stop1 = itinerary.AddStop("Cantina A", "Prima tappa", 43.467, 11.316).Value!;
        var stop2 = itinerary.AddStop("Borgo B", "Seconda tappa", 43.512, 11.298).Value!;
        itinerary.ReorderStops(new[] { stop2.Id, stop1.Id });
        itinerary.Publish();

        await repository.AddAsync(itinerary);

        // Act: tourist opens the published itinerary in public view via unified method (AC03)
        var result = await service.GetPublishedItineraryByIdAsync(itinerary.Id, CancellationToken.None);

        // Assert: unified response returns title, description, and stops in order with map coordinates (latitude/longitude)
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be("Sapori della Toscana");
        result.Value.Theme.Should().Be("Enogastronomia");
        result.Value.Description.Should().Be("Percorso tra cantine storiche e borghi medievali");

        var stops = result.Value.Stops;
        stops.Should().HaveCount(2);

        stops[0].Id.Should().Be(stop2.Id);
        stops[0].Position.Should().Be(1);
        stops[0].Latitude.Should().Be(43.512);
        stops[0].Longitude.Should().Be(11.298);

        stops[1].Id.Should().Be(stop1.Id);
        stops[1].Position.Should().Be(2);
        stops[1].Latitude.Should().Be(43.467);
        stops[1].Longitude.Should().Be(11.316);
    }

    [Fact]
    public async Task AC04_DraftItineraries_ShouldNotAppearInPublicCatalogAndCannotBeAccessedPublicly()
    {
        // Arrange: create one draft itinerary and one published itinerary
        var repository = new InMemoryItineraryRepository();
        var service = new ItineraryService(repository);

        var draft = Itinerary.CreateDraft(
            "Bozza Segreta",
            "Cultura",
            "Itinerario ancora in lavorazione",
            "Umbria",
            "editor-alpha").Value!;

        var published = Itinerary.CreateDraft(
            "Sapori della Toscana",
            "Enogastronomia",
            "Percorso tra cantine storiche e borghi medievali",
            "Toscana",
            "editor-alpha").Value!;
        published.AddStop("Cantina A", "Prima tappa", 43.467, 11.316);
        published.AddStop("Borgo B", "Seconda tappa", 43.512, 11.298);
        published.Publish();

        await repository.AddAsync(draft);
        await repository.AddAsync(published);

        // Act: fetch the public catalog and try to open the draft through public view
        var catalog = await service.GetPublicCatalogAsync(CancellationToken.None);
        var publicAccessResult = await service.GetPublishedItineraryByIdAsync(draft.Id, CancellationToken.None);
        var editorAccessResult = await service.GetDraftByIdAsync(draft.Id, "editor-alpha", CancellationToken.None);

        // Assert: the draft is excluded from the public list and cannot be accessed publicly
        catalog.Items.Should().NotContain(
            i => i.Id == draft.Id,
            "AC04: Draft itineraries must not appear in the public catalog");
        catalog.Items.Should().ContainSingle(i => i.Id == published.Id);

        publicAccessResult.IsFailure.Should().BeTrue(
            "AC04: Draft itineraries cannot be accessed through public views");
        publicAccessResult.Errors.Should().Contain("Itinerary is not published.");

        editorAccessResult.IsSuccess.Should().BeTrue(
            "The responsible editor can still access their draft");
    }

    [Fact]
    public async Task GetPublishedItineraryByIdAsync_WhenNotFound_ShouldReturnFailure()
    {
        var repository = new InMemoryItineraryRepository();
        var service = new ItineraryService(repository);

        var result = await service.GetPublishedItineraryByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Itinerary not found.");
    }

    [Fact]
    public async Task AC05_WhenNoItinerariesArePublished_ShouldReturnUnderstandableMessageInsteadOfEmptyListWithoutExplanation()
    {
        // Arrange: repository has only a draft itinerary (none published)
        var repository = new InMemoryItineraryRepository();
        var service = new ItineraryService(repository);

        var draft = Itinerary.CreateDraft(
            "Solo Bozza",
            "Storia",
            "Non ancora pubblicato",
            "Lazio",
            "editor-alpha").Value!;
        await repository.AddAsync(draft);

        // Act: fetch the public catalog
        var catalog = await service.GetPublicCatalogAsync(CancellationToken.None);

        // Assert: an understandable message is shown instead of an empty list without explanation (AC05)
        catalog.Items.Should().BeEmpty("AC05: No published itineraries exist");
        catalog.Message.Should().NotBeNullOrWhiteSpace("AC05: System must provide an understandable message");
        catalog.Message.Should().Be("No published itineraries available at this time.");
    }
}