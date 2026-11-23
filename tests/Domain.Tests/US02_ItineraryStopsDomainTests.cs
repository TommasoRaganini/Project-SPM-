using Domain.Common;
using Domain.Entities;
using Domain.States;
using FluentAssertions;
using Xunit;

namespace Domain.Tests;

/// <summary>
/// Domain unit tests for US02: Add and edit geolocated fixed stops of an itinerary.
/// Covers AC01, AC02, AC03, AC04, and the State Pattern.
/// </summary>
public class US02_ItineraryStopsDomainTests
{
    private static Itinerary CreateSampleDraftItinerary(string editorId = "editor-01")
    {
        var result = Itinerary.CreateDraft(
            "Tuscan Hills Trail",
            "Nature & Trekking",
            "Scenic trekking across Chianti vineyards.",
            "Tuscany, Italy",
            editorId);

        return result.Value!;
    }

    [Fact]
    public void AC01_AddStop_ToDraftItinerary_WithValidData_ShouldSucceedAndAddStop()
    {
        // Arrange
        var itinerary = CreateSampleDraftItinerary();
        var name = "Piazza del Campo";
        var description = "Historic central square in Siena.";
        var latitude = 43.3188;
        var longitude = 11.3308;

        // Act
        var result = itinerary.AddStop(name, description, latitude, longitude);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        var stop = result.Value!;
        stop.Id.Should().NotBeEmpty();
        stop.ItineraryId.Should().Be(itinerary.Id, "stop must be linked to the parent itinerary");
        stop.Name.Should().Be(name);
        stop.Description.Should().Be(description);
        stop.Latitude.Should().Be(latitude);
        stop.Longitude.Should().Be(longitude);

        // AC02: Added stop appears in the itinerary's stops collection
        itinerary.Stops.Should().HaveCount(1);
        itinerary.Stops.Should().Contain(stop);
    }

    [Fact]
    public void AC01_AddStop_ToNonDraftItinerary_ShouldFailViaStatePattern()
    {
        // Arrange
        var itinerary = CreateSampleDraftItinerary();
        itinerary.TransitionTo(new PublishedState());

        // Act
        var result = itinerary.AddStop(
            "Duomo di Siena",
            "Cathedral of Siena",
            43.3177,
            11.3288);

        // Assert
        result.IsFailure.Should().BeTrue("State Pattern allows stop editing only in Draft state");
        result.Errors.Should().Contain("Only draft itineraries can be modified.");
        itinerary.Stops.Should().BeEmpty();
    }

    [Fact]
    public void AC02_AddMultipleStops_ShouldAllAppearInStopsCollection()
    {
        // Arrange
        var itinerary = CreateSampleDraftItinerary();

        // Act
        var result1 = itinerary.AddStop("Stop 1", "First location", 43.0, 11.0);
        var result2 = itinerary.AddStop("Stop 2", "Second location", 43.5, 11.5);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        itinerary.Stops.Should().HaveCount(2);
        itinerary.Stops.Select(s => s.Name).Should().ContainInOrder("Stop 1", "Stop 2");
    }

    [Fact]
    public void AC03_UpdateStop_InDraftItinerary_WithValidData_ShouldUpdateProperties()
    {
        // Arrange
        var itinerary = CreateSampleDraftItinerary();
        var addResult = itinerary.AddStop("Old Name", "Old Description", 43.0, 11.0);
        var stopId = addResult.Value!.Id;

        // Act
        var updateResult = itinerary.UpdateStop(
            stopId,
            "Updated Piazza",
            "Updated Description with new info",
            43.3188,
            11.3308);

        // Assert
        updateResult.IsSuccess.Should().BeTrue();
        var updatedStop = updateResult.Value!;
        updatedStop.Id.Should().Be(stopId);
        updatedStop.ItineraryId.Should().Be(itinerary.Id);
        updatedStop.Name.Should().Be("Updated Piazza");
        updatedStop.Description.Should().Be("Updated Description with new info");
        updatedStop.Latitude.Should().Be(43.3188);
        updatedStop.Longitude.Should().Be(11.3308);

        // Verify entity in collection reflects changes
        itinerary.Stops.First(s => s.Id == stopId).Name.Should().Be("Updated Piazza");
    }

    [Fact]
    public void AC03_UpdateStop_InNonDraftItinerary_ShouldFailViaStatePattern()
    {
        // Arrange
        var itinerary = CreateSampleDraftItinerary();
        var addResult = itinerary.AddStop("Original Stop", "Original Description", 43.0, 11.0);
        var stopId = addResult.Value!.Id;

        // Transition itinerary to Published state
        itinerary.TransitionTo(new PublishedState());

        // Act
        var updateResult = itinerary.UpdateStop(
            stopId,
            "New Name",
            "New Description",
            43.1,
            11.1);

        // Assert
        updateResult.IsFailure.Should().BeTrue("State Pattern prohibits stop modification in Published state");
        updateResult.Errors.Should().Contain("Only draft itineraries can be modified.");
    }

    [Fact]
    public void AC03_UpdateStop_WithNonExistentStopId_ShouldFailWithStopNotFound()
    {
        // Arrange
        var itinerary = CreateSampleDraftItinerary();
        itinerary.AddStop("Existing Stop", "Description", 43.0, 11.0);
        var unknownId = Guid.NewGuid();

        // Act
        var result = itinerary.UpdateStop(
            unknownId,
            "Valid Name",
            "Valid Description",
            43.0,
            11.0);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Stop not found.");
    }

    [Theory]
    [InlineData(90.001, 10.0, "Latitude must be between -90 and 90.")]
    [InlineData(-90.001, 10.0, "Latitude must be between -90 and 90.")]
    [InlineData(150.0, 10.0, "Latitude must be between -90 and 90.")]
    [InlineData(-120.0, 10.0, "Latitude must be between -90 and 90.")]
    [InlineData(45.0, 180.001, "Longitude must be between -180 and 180.")]
    [InlineData(45.0, -180.001, "Longitude must be between -180 and 180.")]
    [InlineData(45.0, 200.0, "Longitude must be between -180 and 180.")]
    [InlineData(45.0, -250.0, "Longitude must be between -180 and 180.")]
    [InlineData(double.NaN, 10.0, "Latitude must be between -90 and 90.")]
    [InlineData(45.0, double.NaN, "Longitude must be between -180 and 180.")]
    [InlineData(double.PositiveInfinity, 10.0, "Latitude must be between -90 and 90.")]
    [InlineData(45.0, double.NegativeInfinity, "Longitude must be between -180 and 180.")]
    public void AC04_AddStop_WithInvalidCoordinates_ShouldFailAndReject(
        double latitude,
        double longitude,
        string expectedError)
    {
        // Arrange
        var itinerary = CreateSampleDraftItinerary();

        // Act
        var result = itinerary.AddStop("Valid Name", "Valid Description", latitude, longitude);

        // Assert
        result.IsFailure.Should().BeTrue("AC04: The system rejects coordinates outside valid ranges");
        result.Errors.Should().Contain(expectedError);
        itinerary.Stops.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null, "Description", "Stop name is required.")]
    [InlineData("", "Description", "Stop name is required.")]
    [InlineData("   ", "Description", "Stop name is required.")]
    [InlineData("Valid Name", null, "Stop description is required.")]
    [InlineData("Valid Name", "", "Stop description is required.")]
    [InlineData("Valid Name", "   ", "Stop description is required.")]
    public void AC04_AddStop_WithMissingRequiredFields_ShouldFailAndReportErrors(
        string? name,
        string? description,
        string expectedError)
    {
        // Arrange
        var itinerary = CreateSampleDraftItinerary();

        // Act
        var result = itinerary.AddStop(name, description, 43.0, 11.0);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(expectedError);
        itinerary.Stops.Should().BeEmpty();
    }

    [Fact]
    public void AC04_AddStop_WithMultipleInvalidFields_ShouldCollectAllErrors()
    {
        // Arrange
        var itinerary = CreateSampleDraftItinerary();

        // Act (Empty name, empty description, out-of-range latitude, out-of-range longitude)
        var result = itinerary.AddStop("", "", 100.0, 200.0);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().HaveCount(4);
        result.Errors.Should().Contain("Stop name is required.");
        result.Errors.Should().Contain("Stop description is required.");
        result.Errors.Should().Contain("Latitude must be between -90 and 90.");
        result.Errors.Should().Contain("Longitude must be between -180 and 180.");
    }

    [Fact]
    public void AC04_UpdateStop_WithInvalidData_ShouldFailAndKeepOriginalValues()
    {
        // Arrange
        var itinerary = CreateSampleDraftItinerary();
        var addResult = itinerary.AddStop("Original", "Original Desc", 43.0, 11.0);
        var stopId = addResult.Value!.Id;

        // Act
        var updateResult = itinerary.UpdateStop(stopId, "", "Updated Desc", 999.0, 11.0);

        // Assert
        updateResult.IsFailure.Should().BeTrue();
        updateResult.Errors.Should().Contain("Stop name is required.");
        updateResult.Errors.Should().Contain("Latitude must be between -90 and 90.");

        var originalStop = itinerary.Stops.First(s => s.Id == stopId);
        originalStop.Name.Should().Be("Original");
        originalStop.Latitude.Should().Be(43.0);
    }
}

