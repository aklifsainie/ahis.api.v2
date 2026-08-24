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
    public sealed class ApiClientPermissionConfiguration : IEntityTypeConfiguration<ApiClientPermission>
    {
        public void Configure(
            EntityTypeBuilder<ApiClientPermission> builder)
        {
            builder.ToTable("ApiClientPermissions");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.ApiClientId).IsRequired();
            builder.Property(x => x.PermissionCode).HasMaxLength(100).IsRequired();
            builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
            builder.Property(x => x.CreatedBy).HasMaxLength(100);
            /*
            * Prevent the same permission from being assigned
            * more than once to the same API client.
            */
            builder.HasIndex(x => new { x.ApiClientId, x.PermissionCode }).IsUnique();
        }

    }
}
