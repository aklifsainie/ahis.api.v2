using ahis.template.domain.Models.Entities.ApiKey;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;



namespace ahis.template.infrastructure.Configurations.ApiKey
{
    public sealed class ApiClientConfiguration : IEntityTypeConfiguration<ApiClient>
    {
        public void Configure(EntityTypeBuilder<ApiClient> builder)
        {
            builder.ToTable("ApiClients");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.ClientId).HasMaxLength(100).IsRequired();
            builder.HasIndex(x => x.ClientId).IsUnique();
            builder.Property(x => x.ClientName).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(500);
            builder.Property(x => x.ContactName).HasMaxLength(200);
            builder.Property(x => x.ContactEmail).HasMaxLength(320);
            builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
            builder.Property(x => x.RateLimitPerMinute).HasDefaultValue(60).IsRequired();
            builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
            builder.Property(x => x.CreatedBy).HasMaxLength(100);
            builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
            builder.Property(x => x.UpdatedBy).HasMaxLength(100);
            builder.Property(x => x.DeactivatedAt).HasColumnType("datetime2");
            builder.Property(x => x.DeactivationReason).HasMaxLength(500);
            builder.HasMany(x => x.Keys).WithOne(x => x.ApiClient).HasForeignKey(x => x.ApiClientId).OnDelete(DeleteBehavior.Restrict);
            builder.HasMany(x => x.Permissions).WithOne(x => x.ApiClient).HasForeignKey(x => x.ApiClientId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
