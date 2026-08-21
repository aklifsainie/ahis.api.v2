using ahis.template.domain.Models.Entities.ApiKey;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.infrastructure.Configurations.ApiKey
{
    public sealed class ApiClientKeyConfiguration : IEntityTypeConfiguration<ApiClientKey>
    {
        public void Configure(EntityTypeBuilder<ApiClientKey> builder)
        {
            builder.ToTable("ApiClientKeys");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.ApiClientId).IsRequired();
            builder.Property(x => x.KeyName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.KeyPrefix).HasMaxLength(32).IsRequired();
            /*
            * SHA-256 represented in hexadecimal is exactly
            * 64 characters.
            */
            builder.Property(x => x.KeyHash).HasColumnType("char(64)").IsRequired();
            builder.HasIndex(x => x.KeyHash).IsUnique();
            builder.HasIndex(x => x.KeyPrefix);
            builder.HasIndex(x => x.ApiClientId);
            builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
            builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
            builder.Property(x => x.CreatedBy).HasMaxLength(100);
            builder.Property(x => x.ExpiresAt).HasColumnType("datetime2");
            builder.Property(x => x.LastUsedAt).HasColumnType("datetime2");
            builder.Property(x => x.RevokedAt).HasColumnType("datetime2");
            builder.Property(x => x.RevokedBy).HasMaxLength(100);
            builder.Property(x => x.RevocationReason).HasMaxLength(500);
        }
    }
}
