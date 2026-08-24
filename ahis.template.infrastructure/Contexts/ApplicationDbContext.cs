using ahis.template.domain.Models.Entities;
using ahis.template.domain.Models.Entities.ApiKey;
using ahis.template.infrastructure.Persistences.Interceptors;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.infrastructure.Contexts
{
    public class ApplicationDbContext : DbContext
    {

        private readonly AuditSaveChangesInterceptor _auditSaveChangesInterceptor;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options,
            AuditSaveChangesInterceptor auditSaveChangesInterceptor
            ) : base(options)
        {
            _auditSaveChangesInterceptor = auditSaveChangesInterceptor;
        }

        ////////
        /// API KEY RELATED
        /// 
        public DbSet<ApiClient> ApiClients => Set<ApiClient>();
        public DbSet<ApiClientKey> ApiClientKey => Set<ApiClientKey>();
        public DbSet<ApiClientPermission> ApiClientPermission => Set<ApiClientPermission>();

        /// <summary>
        /// Audit Log table for tracking changes to entities. This table is used for auditing purposes and should not be modified directly by application code.
        /// </summary>
        public DbSet<AuditLog> AuditLog => Set<AuditLog>();



        public DbSet<Country> Country => Set<Country>();


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Register the interceptor to fires on every SaveChanges call
            optionsBuilder.AddInterceptors(_auditSaveChangesInterceptor);
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Auto-discover every IEntityTypeConfiguration EXCEPT CitizenConfiguration,
            // which needs a constructor argument the assembly scanner can't supply.
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(ApplicationDbContext).Assembly);

        }
    }
}
