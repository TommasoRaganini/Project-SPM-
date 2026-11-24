using Application.DTOs.Responses;
using Application.DTOs.Requests;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.States;
using FluentAssertions;
using Xunit;

namespace Domain.Tests;

public class US01_ItineraryServiceTests
{
    private class InMemoryItineraryRepository : IItineraryRepository
    {
        public readonly List<Itinerary> Items = new();

        public Task AddAsync(Itinerary itinerary, CancellationToken cancellationToken = default)
        {
            Items.Add(itinerary);
            return Task.CompletedTask;
        }

        public Task<Itinerary?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var found = Items.FirstOrDefault(i => i.Id == id);
            found?.RestoreState();
            return Task.FromResult(found);
        }

        public Task<IReadOnlyList<Itinerary>> GetPublicCatalogAsync(CancellationToken cancellationToken = default)
        {
            var list = Items.Where(i => i.Status == ItineraryStatus.Published).ToList();
            list.ForEach(i => i.RestoreState());
            return Task.FromResult<IReadOnlyList<Itinerary>>(list);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task AC01_AC02_AC03_CreateDraftAsync_ShouldSaveDraftAndReturnResponse()
    {
        // Arrange
        var repo = new InMemoryItineraryRepository();
        var service = new ItineraryService(repo);
        var request = new CreateItineraryRequest(
            "Barolo Wine Route",
            "Food and Wine",
            "Historic winery tour in the Langhe hills.",
            "Piedmont, Cuneo"
        );
        var editorId = "editor-langhe";

        // Act
        var result = await service.CreateDraftAsync(request, editorId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        var response = result.Value!;
        response.Id.Should().NotBeEmpty();
        response.Title.Should().Be("Barolo Wine Route");
        response.Status.Should().Be("Draft");
        response.IsVisibleInPublicCatalog.Should().BeFalse();
        response.ResponsibleEditorId.Should().Be(editorId);

        // Repository should contain the saved itinerary
        repo.Items.Should().HaveCount(1);
        repo.Items[0].Id.Should().Be(response.Id);
    }

    [Fact]
    public async Task AC02_PublicCatalog_ShouldNotContainDraftItineraries()
    {
        // Arrange
        var repo = new InMemoryItineraryRepository();
        var service = new ItineraryService(repo);
        var request = new CreateItineraryRequest(
            "Brianza Lakes Tour",
            "Nature",
            "Cycling scenic route",
            "Lombardy"
        );

        await service.CreateDraftAsync(request, "editor-1");

        // Act
        var publicCatalog = await service.GetPublicCatalogAsync();

        // Assert (AC02: Does not appear in the public catalog)
        publicCatalog.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task AC04_CreateDraftAsync_InvalidFields_ShouldNotSaveAndReturnErrors()
    {
        // Arrange
        var repo = new InMemoryItineraryRepository();
        var service = new ItineraryService(repo);
        var request = new CreateItineraryRequest(
            "", // Missing title
            "", // Missing theme
            "Valid Description",
            "Tuscany"
        );

        // Act
        var result = await service.CreateDraftAsync(request, "editor-1");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Title is required.");
        result.Errors.Should().Contain("Theme is required.");
        repo.Items.Should().BeEmpty("AC04: Must not save itinerary to the database if required fields are missing or invalid");
    }

    [Fact]
    public async Task AC05_GetDraftByIdAsync_ShouldRetrievePreviouslySavedData()
    {
        // Arrange
        var repo = new InMemoryItineraryRepository();
        var service = new ItineraryService(repo);
        var createRequest = new CreateItineraryRequest(
            "Etruscan Trail",
            "Archaeology",
            "Visit to Etruscan necropolises",
            "Lazio, Tarquinia"
        );
        var created = await service.CreateDraftAsync(createRequest, "editor-tarquinia");
        var savedId = created.Value!.Id;

        // Act (AC05: Editor can reopen draft and retrieve previously saved data)
        var retrievedResult = await service.GetDraftByIdAsync(savedId, "editor-tarquinia");

        // Assert
        retrievedResult.IsSuccess.Should().BeTrue();
        var retrieved = retrievedResult.Value!;
        retrieved.Id.Should().Be(savedId);
        retrieved.Title.Should().Be("Etruscan Trail");
        retrieved.Theme.Should().Be("Archaeology");
        retrieved.Description.Should().Be("Visit to Etruscan necropolises");
        retrieved.GeographicalArea.Should().Be("Lazio, Tarquinia");
        retrieved.ResponsibleEditorId.Should().Be("editor-tarquinia");
        retrieved.Status.Should().Be("Draft");
    }
}
