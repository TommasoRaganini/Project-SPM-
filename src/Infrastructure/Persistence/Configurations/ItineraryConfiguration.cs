using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ItineraryConfiguration : IEntityTypeConfiguration<Itinerary>
{
    public void Configure(EntityTypeBuilder<Itinerary> builder)
    {
        builder.ToTable("itineraries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Theme)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.GeographicalArea)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.ResponsibleEditorId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // The State Pattern is managed in-memory as behavior and ignored for direct column mapping
        builder.Ignore(x => x.State);
    }
}
