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
                b.Property(x => x.Token).IsRequired().HasMaxLength(450);
                b.Property(x => x.IsRevoked).HasDefaultValue(false);
                b.HasIndex(x => x.UserId);
            });
        }
    }
}
