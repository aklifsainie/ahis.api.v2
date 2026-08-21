using ahis.template.domain.Models.Entities;
using ahis.template.domain.Models.Entities.ApiKey;
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
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        ////////
        /// API KEY RELATED
        /// 
        public DbSet<ApiClient> ApiClients => Set<ApiClient>();
        public DbSet<ApiClientKey> ApiClientKey => Set<ApiClientKey>();
        public DbSet<ApiClientPermission> ApiClientPermission => Set<ApiClientPermission>();



        public DbSet<Country> Country => Set<Country>();
    }
}
