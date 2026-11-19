using Domain.Common;
using Domain.Entities;
using Domain.States;
using FluentAssertions;
using Xunit;

namespace Domain.Tests;

public class US01_ThematicItineraryCreationTests
{
    [Fact]
    public void AC01_And_AC03_CreateDraft_WithValidFields_ShouldAssignUniqueIdAndResponsibleEditor()
    {
        // Arrange
        var title = "Florentine Renaissance Itinerary";
        var theme = "Art and History";
        var description = "Artistic journey through Renaissance masterpieces in Florence.";
        var geoArea = "Florence, Tuscany";
        var editorId = "editor-rossi-01";

        // Act
        var result = Itinerary.CreateDraft(title, theme, description, geoArea, editorId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        var itinerary = result.Value!;
        itinerary.Id.Should().NotBeEmpty("AC03: System assigns a unique identifier");
        itinerary.Title.Should().Be(title);
        itinerary.Theme.Should().Be(theme);
        itinerary.Description.Should().Be(description);
        itinerary.GeographicalArea.Should().Be(geoArea);
        itinerary.ResponsibleEditorId.Should().Be(editorId, "AC03: System records the responsible editor");
        itinerary.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void AC02_CreateDraft_ShouldAssignDraftStatus_AndNotBeVisibleInPublicCatalog()
    {
        // Arrange & Act
        var result = Itinerary.CreateDraft(
            "Path of the Gods",
            "Nature and Hiking",
            "Panoramic trekking route along the Amalfi Coast.",
            "Amalfi Coast, Campania",
            "editor-bianchi-02");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var itinerary = result.Value!;

        // AC02: On creation, the itinerary is assigned Draft status
        itinerary.Status.Should().Be(ItineraryStatus.Draft);
        itinerary.State.Should().BeOfType<DraftState>();

        // AC02: Does not appear in the public catalog
        itinerary.State.IsVisibleInPublicCatalog.Should().BeFalse(
            "AC02: An itinerary in Draft status must not appear in the public catalog");
    }

    [Theory]
    [InlineData(null, "Theme", "Description", "Area", "editor-1", "Title is required.")]
    [InlineData("", "Theme", "Description", "Area", "editor-1", "Title is required.")]
    [InlineData("   ", "Theme", "Description", "Area", "editor-1", "Title is required.")]
    [InlineData("Title", null, "Description", "Area", "editor-1", "Theme is required.")]
    [InlineData("Title", "", "Description", "Area", "editor-1", "Theme is required.")]
    [InlineData("Title", "Theme", null, "Area", "editor-1", "Description is required.")]
    [InlineData("Title", "Theme", "", "Area", "editor-1", "Description is required.")]
    [InlineData("Title", "Theme", "Description", null, "editor-1", "Geographical area is required.")]
    [InlineData("Title", "Theme", "Description", "", "editor-1", "Geographical area is required.")]
    [InlineData("Title", "Theme", "Description", "Area", null, "Responsible editor is required.")]
    [InlineData("Title", "Theme", "Description", "Area", "", "Responsible editor is required.")]
    public void AC04_CreateDraft_WithMissingOrInvalidFields_ShouldFailAndReportIssues(
        string? title,
        string? theme,
        string? description,
        string? geoArea,
        string? editorId,
        string expectedError)
    {
        // Act
        var result = Itinerary.CreateDraft(title, theme, description, geoArea, editorId);

        // Assert
        result.IsFailure.Should().BeTrue("AC04: If a required field is missing or invalid, system does not save itinerary");
        result.Errors.Should().Contain(expectedError, "AC04: System reports the issue");
        result.Value.Should().BeNull();
    }

    [Fact]
    public void StatePattern_DraftState_CannotPublish_WithoutMinimumTwoStops()
    {
        // Domain rule verification (Explicit hypothesis i: itinerary must have at least two stops to be published)
        var result = Itinerary.CreateDraft("Castle Tour", "History", "Description", "Trentino", "editor-1");
        result.IsSuccess.Should().BeTrue();
        var itinerary = result.Value!;

        itinerary.State.CanPublish(validStopsCount: 0).Should().BeFalse();
        itinerary.State.CanPublish(validStopsCount: 1).Should().BeFalse();
        itinerary.State.CanPublish(validStopsCount: 2).Should().BeTrue();
    }
}
