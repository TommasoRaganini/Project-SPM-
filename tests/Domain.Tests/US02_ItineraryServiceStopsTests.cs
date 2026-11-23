using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Services;
using Domain.Entities;
using Domain.States;
using FluentAssertions;
using Xunit;

namespace Domain.Tests;

/// <summary>
/// Application Service tests for US02: Itinerary Stops Management.
/// Verifies use case orchestration, persistence, authorization, and error handling.
/// </summary>
public class US02_ItineraryServiceStopsTests
{
    private static (ItineraryService Service, InMemoryItineraryRepository Repo, Itinerary DraftItinerary) CreateSetup(string editorId = "editor-alpha")
    {
        var repo = new InMemoryItineraryRepository();
        var service = new ItineraryService(repo);
        var itinerary = Itinerary.CreateDraft(
            "Amalfi Panoramic Route",
            "Coast & Views",
            "A breathtaking tour through coastal cliffs.",
            "Campania, Italy",
            editorId).Value!;

        repo.AddAsync(itinerary).GetAwaiter().GetResult();
        return (service, repo, itinerary);
    }

    [Fact]
    public async Task AC01_AddStopAsync_WithAuthorizedEditor_ShouldSaveStopAndReturnResponse()
    {
        // Arrange
        var (service, repo, itinerary) = CreateSetup("editor-alpha");
        var request = new CreateItineraryStopRequest
        {
            Name = "Positano Viewpoint",
            Description = "Terrace overlooking the Mediterranean sea.",
            Latitude = 40.6281,
            Longitude = 14.4850
        };

        // Act
        var result = await service.AddStopAsync(itinerary.Id, request, "editor-alpha");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        var response = result.Value!;
        response.Id.Should().NotBeEmpty();
        response.ItineraryId.Should().Be(itinerary.Id);
        response.Name.Should().Be(request.Name);
        response.Description.Should().Be(request.Description);
        response.Latitude.Should().Be(request.Latitude);
        response.Longitude.Should().Be(request.Longitude);

        // Verify repository state
        var savedItinerary = repo.Items.First(i => i.Id == itinerary.Id);
        savedItinerary.Stops.Should().HaveCount(1);
        savedItinerary.Stops.First().Id.Should().Be(response.Id);
    }

    [Fact]
    public async Task AC02_GetStopsAsync_ShouldReturnAllStopsForItinerary()
    {
        // Arrange
        var (service, _, itinerary) = CreateSetup("editor-alpha");

        await service.AddStopAsync(itinerary.Id, new CreateItineraryStopRequest
        {
            Name = "Stop 1",
            Description = "Desc 1",
            Latitude = 40.0,
            Longitude = 14.0
        }, "editor-alpha");

        await service.AddStopAsync(itinerary.Id, new CreateItineraryStopRequest
        {
            Name = "Stop 2",
            Description = "Desc 2",
            Latitude = 40.5,
            Longitude = 14.5
        }, "editor-alpha");

        // Act
        var result = await service.GetStopsAsync(itinerary.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var stops = result.Value!.ToList();
        stops.Should().HaveCount(2);
        stops.Select(s => s.Name).Should().ContainInOrder("Stop 1", "Stop 2");
    }

    [Fact]
    public async Task AC02_GetStopsAsync_WhenItineraryNotFound_ShouldReturnFailure()
    {
        // Arrange
        var repo = new InMemoryItineraryRepository();
        var service = new ItineraryService(repo);

        // Act
        var result = await service.GetStopsAsync(Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Itinerary not found.");
    }

    [Fact]
    public async Task AC03_UpdateStopAsync_WithAuthorizedEditor_ShouldUpdateAndPersist()
    {
        // Arrange
        var (service, repo, itinerary) = CreateSetup("editor-alpha");
        var addResult = await service.AddStopAsync(itinerary.Id, new CreateItineraryStopRequest
        {
            Name = "Original Name",
            Description = "Original Description",
            Latitude = 40.0,
            Longitude = 14.0
        }, "editor-alpha");
        var stopId = addResult.Value!.Id;

        var updateRequest = new UpdateItineraryStopRequest
        {
            Name = "Updated Name",
            Description = "Updated Description",
            Latitude = 40.6,
            Longitude = 14.6
        };

        // Act
        var updateResult = await service.UpdateStopAsync(
            itinerary.Id,
            stopId,
            updateRequest,
            "editor-alpha");

        // Assert
        updateResult.IsSuccess.Should().BeTrue();
        var updatedResponse = updateResult.Value!;
        updatedResponse.Id.Should().Be(stopId);
        updatedResponse.Name.Should().Be("Updated Name");
        updatedResponse.Description.Should().Be("Updated Description");
        updatedResponse.Latitude.Should().Be(40.6);
        updatedResponse.Longitude.Should().Be(14.6);

        // Verify persisted state in repository
        var savedStop = repo.Items.First(i => i.Id == itinerary.Id).Stops.First(s => s.Id == stopId);
        savedStop.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task AC03_UpdateStopAsync_WhenStopNotFound_ShouldReturnFailure()
    {
        // Arrange
        var (service, _, itinerary) = CreateSetup("editor-alpha");
        var updateRequest = new UpdateItineraryStopRequest
        {
            Name = "Updated Name",
            Description = "Updated Description",
            Latitude = 40.0,
            Longitude = 14.0
        };

        // Act
        var result = await service.UpdateStopAsync(
            itinerary.Id,
            Guid.NewGuid(),
            updateRequest,
            "editor-alpha");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Stop not found.");
    }

    [Theory]
    [InlineData(120.0, 14.0, "Latitude must be between -90 and 90.")]
    [InlineData(40.0, 200.0, "Longitude must be between -180 and 180.")]
    public async Task AC04_AddStopAsync_WithInvalidCoordinates_ShouldFailAndNotPersist(
        double latitude,
        double longitude,
        string expectedError)
    {
        // Arrange
        var (service, repo, itinerary) = CreateSetup("editor-alpha");
        var invalidRequest = new CreateItineraryStopRequest
        {
            Name = "Invalid Coordinates Stop",
            Description = "Valid Description",
            Latitude = latitude,
            Longitude = longitude
        };

        // Act
        var result = await service.AddStopAsync(itinerary.Id, invalidRequest, "editor-alpha");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(expectedError);
        repo.Items.First(i => i.Id == itinerary.Id).Stops.Should().BeEmpty();
    }

    [Fact]
    public async Task AC04_UpdateStopAsync_WithInvalidFields_ShouldFailAndNotPersist()
    {
        // Arrange
        var (service, repo, itinerary) = CreateSetup("editor-alpha");
        var addResult = await service.AddStopAsync(itinerary.Id, new CreateItineraryStopRequest
        {
            Name = "Initial Stop",
            Description = "Initial Description",
            Latitude = 40.0,
            Longitude = 14.0
        }, "editor-alpha");
        var stopId = addResult.Value!.Id;

        var invalidUpdateRequest = new UpdateItineraryStopRequest
        {
            Name = "", // Empty name
            Description = "Updated",
            Latitude = 40.0,
            Longitude = 14.0
        };

        // Act
        var result = await service.UpdateStopAsync(itinerary.Id, stopId, invalidUpdateRequest, "editor-alpha");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Stop name is required.");
        repo.Items.First(i => i.Id == itinerary.Id).Stops.First(s => s.Id == stopId).Name.Should().Be("Initial Stop");
    }

    [Theory]
    [InlineData("unauthorized-editor")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task AC05_AddStopAsync_WithUnauthorizedEditor_ShouldFailWithForbiddenMessage(string? unauthorizedEditor)
    {
        // Arrange
        var (service, repo, itinerary) = CreateSetup("editor-alpha");
        var request = new CreateItineraryStopRequest
        {
            Name = "New Stop",
            Description = "Stop description",
            Latitude = 40.0,
            Longitude = 14.0
        };

        // Act
        var result = await service.AddStopAsync(itinerary.Id, request, unauthorizedEditor!);

        // Assert
        result.IsFailure.Should().BeTrue("AC05: An unauthorized editor cannot modify the itinerary's stops");
        result.Errors.Should().Contain("You are not authorized to modify this itinerary.");
        repo.Items.First(i => i.Id == itinerary.Id).Stops.Should().BeEmpty();
    }

    [Theory]
    [InlineData("unauthorized-editor")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task AC05_UpdateStopAsync_WithUnauthorizedEditor_ShouldFailWithForbiddenMessage(string? unauthorizedEditor)
    {
        // Arrange
        var (service, repo, itinerary) = CreateSetup("editor-alpha");
        var addResult = await service.AddStopAsync(itinerary.Id, new CreateItineraryStopRequest
        {
            Name = "Initial Stop",
            Description = "Initial Description",
            Latitude = 40.0,
            Longitude = 14.0
        }, "editor-alpha");
        var stopId = addResult.Value!.Id;

        var updateRequest = new UpdateItineraryStopRequest
        {
            Name = "Hacked Stop",
            Description = "Hacked Description",
            Latitude = 40.0,
            Longitude = 14.0
        };

        // Act
        var result = await service.UpdateStopAsync(
            itinerary.Id,
            stopId,
            updateRequest,
            unauthorizedEditor!);

        // Assert
        result.IsFailure.Should().BeTrue("AC05: An unauthorized editor cannot modify the itinerary's stops");
        result.Errors.Should().Contain("You are not authorized to modify this itinerary.");
        repo.Items.First(i => i.Id == itinerary.Id).Stops.First(s => s.Id == stopId).Name.Should().Be("Initial Stop");
    }

    [Fact]
    public async Task AddStopAsync_WhenItineraryNotFound_ShouldReturnFailure()
    {
        // Arrange
        var repo = new InMemoryItineraryRepository();
        var service = new ItineraryService(repo);
        var request = new CreateItineraryStopRequest
        {
            Name = "Stop",
            Description = "Description",
            Latitude = 40.0,
            Longitude = 14.0
        };

        // Act
        var result = await service.AddStopAsync(Guid.NewGuid(), request, "editor-alpha");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Itinerary not found.");
    }

    [Fact]
    public async Task UpdateStopAsync_WhenItineraryNotFound_ShouldReturnFailure()
    {
        // Arrange
        var repo = new InMemoryItineraryRepository();
        var service = new ItineraryService(repo);
        var request = new UpdateItineraryStopRequest
        {
            Name = "Stop",
            Description = "Description",
            Latitude = 40.0,
            Longitude = 14.0
        };

        // Act
        var result = await service.UpdateStopAsync(Guid.NewGuid(), Guid.NewGuid(), request, "editor-alpha");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Itinerary not found.");
    }
}

