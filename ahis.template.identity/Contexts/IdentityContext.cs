using ahis.template.identity.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ahis.template.identity.Contexts
{
    public class IdentityContext : IdentityDbContext<ApplicationUser>
    {
        public IdentityContext(DbContextOptions<IdentityContext> options) : base(options)
        {
        }

        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<RefreshSession> RefreshSessions { get; set; }
        public DbSet<AccountRecoveryChallenge> AccountRecoveryChallenges { get; set; }
        public DbSet<AccountRecoveryThrottle> AccountRecoveryThrottles { get; set; }
        public DbSet<IdentityUserRestriction> IdentityUserRestrictions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Rename default AspNet* tables to remove AspNet prefix
            builder.Entity<ApplicationUser>(b =>
            {
                b.ToTable("IdentityUsers");
            });

            builder.Entity<IdentityRole<string>>(b =>
            {
                b.ToTable("IdentityRoles");
            });

            builder.Entity<IdentityUserRole<string>>(b =>
            {
                b.ToTable("IdentityUserRoles");
            });

            builder.Entity<IdentityUserClaim<string>>(b =>
            {
                b.ToTable("IdentityUserClaims");
            });

            builder.Entity<IdentityUserLogin<string>>(b =>
            {
                b.ToTable("IdentityUserLogins");
            });

            builder.Entity<IdentityRoleClaim<string>>(b =>
            {
                b.ToTable("IdentityRoleClaims");
            });


            builder.Entity<IdentityUserToken<string>>(b =>
            {
                b.ToTable("IdentityUserTokens");
            });

            builder.Entity<RefreshToken>(b =>
            {
                b.ToTable("RefreshTokens");
                b.HasKey(x => x.Id);
                b.Property(x => x.TokenHash).IsRequired().HasColumnType("binary(32)");
                b.Property(x => x.SecurityVersion).HasMaxLength(64);
                b.Property(x => x.IsRevoked).HasDefaultValue(false);
                b.HasIndex(x => x.UserId);
                b.HasIndex(x => x.TokenHash).IsUnique();
                b.HasIndex(x => x.ParentTokenId).IsUnique().HasFilter("[ParentTokenId] IS NOT NULL");
                b.HasIndex(x => new { x.SessionId, x.IsRevoked, x.ExpiresAt });
                b.HasOne<RefreshSession>()
                    .WithMany()
                    .HasForeignKey(x => x.SessionId)
                    .OnDelete(DeleteBehavior.Restrict);
                b.HasOne<RefreshToken>()
                    .WithMany()
                    .HasForeignKey(x => x.ParentTokenId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<RefreshSession>(b =>
            {
                b.ToTable("RefreshSessions");
                b.HasKey(x => x.Id);
                b.Property(x => x.PublicId).IsRequired();
                b.Property(x => x.UserId).IsRequired().HasMaxLength(450);
                b.Property(x => x.IsRevoked).HasDefaultValue(false);
                b.HasIndex(x => x.PublicId).IsUnique();
                b.HasIndex(x => new { x.UserId, x.IsRevoked, x.ExpiresAt });
            });

            builder.Entity<AccountRecoveryChallenge>(b =>
            {
                b.ToTable("AccountRecoveryChallenges");
                b.HasKey(x => x.Id);
                b.Property(x => x.ChallengeHash).IsRequired().HasColumnType("binary(32)");
                b.Property(x => x.SecurityVersion).IsRequired().HasMaxLength(64);
                b.HasIndex(x => x.ChallengeHash).IsUnique();
                b.HasIndex(x => new { x.UserId, x.ConsumedAt, x.ExpiresAt });
            });

            builder.Entity<AccountRecoveryThrottle>(b =>
            {
                b.ToTable("AccountRecoveryThrottles");
                b.HasKey(x => x.Id);
                b.Property(x => x.SubjectHash).IsRequired().HasMaxLength(64);
                b.HasIndex(x => x.SubjectHash).IsUnique();
            });

            builder.Entity<IdentityUserRestriction>(b =>
            {
                b.ToTable("IdentityUserRestrictions");
                b.HasKey(x => x.Id);
                b.Property(x => x.UserId).IsRequired().HasMaxLength(450);
                b.Property(x => x.Category).IsRequired();
                b.Property(x => x.StartedAtUtc).IsRequired();
                b.Property(x => x.Origin).IsRequired().HasMaxLength(100);
                b.Property(x => x.PlacedByUserId).HasMaxLength(450);
                b.Property(x => x.EndedByUserId).HasMaxLength(450);
                b.Property(x => x.ReviewedByUserId).HasMaxLength(450);
                b.Property(x => x.EvidenceReference).HasMaxLength(500);
                b.Property(x => x.InternalReasonCode).HasMaxLength(100);
                b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
                b.HasIndex(x => x.UserId).HasFilter("[EndedAtUtc] IS NULL").IsUnique();
                b.HasIndex(x => new { x.UserId, x.StartedAtUtc });
                b.HasIndex(x => new { x.Category, x.EndedAtUtc });
                b.ToTable(t => t.HasCheckConstraint("CK_IdentityUserRestrictions_CategoryExpiry",
                    "([Category] = 1 AND [ExpiresAtUtc] IS NOT NULL) OR ([Category] IN (2, 3) AND [ExpiresAtUtc] IS NULL)"));
                b.ToTable(t => t.HasCheckConstraint("CK_IdentityUserRestrictions_EndConsistency",
                    "[EndedAtUtc] IS NULL OR [EndedAtUtc] >= [StartedAtUtc]"));
            });
        }
    }
}
