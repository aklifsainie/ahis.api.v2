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
    public class AuditLogConfiguration: IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLog");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.UserId).HasMaxLength(450); // matches ASP.NET Identity Id length convention
            builder.Property(a => a.UserName).HasMaxLength(256);
            builder.Property(a => a.UserRole).HasMaxLength(100);

            builder.Property(a => a.EntityName).HasMaxLength(200).IsRequired();
            builder.Property(a => a.EntityId).HasMaxLength(200).IsRequired();

            builder.Property(a => a.Action).HasConversion<string>().HasMaxLength(50);

            builder.Property(a => a.OldValues).HasColumnType("nvarchar(max)");
            builder.Property(a => a.NewValues).HasColumnType("nvarchar(max)");
            builder.Property(a => a.Metadata).HasColumnType("nvarchar(max)");

            builder.Property(a => a.AffectedColumns).HasMaxLength(1000);
            builder.Property(a => a.IpAddress).HasMaxLength(45);     // covers IPv6
            builder.Property(a => a.UserAgent).HasMaxLength(500);
            builder.Property(a => a.CorrelationId).HasMaxLength(100);

            // The two query patterns you'll actually run: "history of this record"
            // and "what did this user do, and when".
            builder.HasIndex(a => new { a.EntityName, a.EntityId }).HasDatabaseName("IX_AuditLog_EntityName_EntityId");
            builder.HasIndex(a => new { a.UserId, a.TimestampUtc }).HasDatabaseName("IX_AuditLog_UserId_TimestampUtc");
            builder.HasIndex(a => a.TimestampUtc).HasDatabaseName("IX_AuditLog_TimestampUtc");

            // No FK relationships to User tables on purpose - audit rows
            // must survive even if the referenced record is deleted.
        }
    }
}
