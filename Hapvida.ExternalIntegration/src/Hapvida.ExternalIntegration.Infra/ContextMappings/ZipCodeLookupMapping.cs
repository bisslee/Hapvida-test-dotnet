using Hapvida.ExternalIntegration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hapvida.ExternalIntegration.Infra.ContextMappings;

public class ZipCodeLookupMapping : IEntityTypeConfiguration<ZipCodeLookup>
{
    public void Configure(EntityTypeBuilder<ZipCodeLookup> builder)
    {
        builder.ToTable("ZipCodeLookups");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ZipCode)
            .IsRequired()
            .HasMaxLength(8);

        builder.Property(x => x.Street)
            .HasMaxLength(200);

        builder.Property(x => x.District)
            .HasMaxLength(100);

        builder.Property(x => x.City)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.State)
            .IsRequired()
            .HasMaxLength(2);

        builder.Property(x => x.Ibge)
            .HasMaxLength(10);

        builder.Property(x => x.Provider)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(100);

        builder.Property(x => x.UpdatedAt);

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(100);
    }
}

