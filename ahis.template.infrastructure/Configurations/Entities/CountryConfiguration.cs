using ahis.template.domain.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.infrastructure.Configurations.Entities
{
    public class CountryConfiguration : IEntityTypeConfiguration<Country>
    {
        public void Configure(EntityTypeBuilder<Country> builder)
        {
            builder.ToTable("Country");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.CountryFullname).IsRequired();
            builder.Property(x => x.CountryShortname).IsRequired();
            builder.Property(x => x.CountryDescription);
            builder.Property(x => x.CountryCode2).IsRequired();
            builder.Property(x => x.CountryCode3).IsRequired();
            builder.Property(x => x.CountryIsoCode).IsRequired();
            builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
            builder.Property(x => x.IsDelete).HasDefaultValue(false).IsRequired();
            builder.Property(x => x.RegisterBy).HasMaxLength(100);
            builder.Property(x => x.RegisterDate).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
            builder.Property(x => x.UpdatedBy).HasMaxLength(100);
            builder.Property(x => x.UpdatedDate);
            builder.Property(x => x.Remarks).HasMaxLength(500);
            builder.HasIndex(x => x.CountryIsoCode).IsUnique();
            builder.HasIndex(x => x.CountryCode2).IsUnique();
            builder.HasIndex(x => x.CountryCode3).IsUnique();
        }
    }
}
