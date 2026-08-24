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
    public sealed class CountryConfiguration: IEntityTypeConfiguration<Country>
    {
        public void Configure(EntityTypeBuilder<Country> builder)
        {
            builder.ToTable("Country");
            builder.HasKey(country => country.Id);
            builder.Property(country => country.CountryFullname).HasMaxLength(200).IsRequired();
            builder.Property(country => country.CountryShortname).HasMaxLength(100).IsRequired();
            builder.Property(country => country.CountryDescription).HasMaxLength(1000);
            builder.Property(country => country.CountryCode2).HasMaxLength(2).IsUnicode(false).IsRequired();
            builder.Property(country => country.CountryCode3).HasMaxLength(3).IsUnicode(false).IsRequired();
            builder.Property(country => country.CountryIsoCode).HasMaxLength(3).IsUnicode(false).IsRequired();

            builder.HasIndex(country => country.CountryFullname).IsUnique();
            builder.HasIndex(country => country.CountryCode2).IsUnique();
            builder.HasIndex(country => country.CountryCode3).IsUnique();
            builder.HasIndex(country => country.CountryIsoCode).IsUnique();
        }
    }
}
