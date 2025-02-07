using GenesisFEPortalWebApp.Models.Entities.Catalog;
using GenesisFEPortalWebApp.Models.Entities.Core;
using GenesisFEPortalWebApp.Models.Entities.Security;
using GenesisFEPortalWebApp.Models.Entities.Subscription;
using GenesisFEPortalWebApp.Models.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.Database.Data
{
    public class AppDbContext : DbContext
    {

        private readonly ITenantService _tenantService;

        public AppDbContext(
            DbContextOptions<AppDbContext> options,
            ITenantService tenantService) : base(options)
        {
            _tenantService = tenantService;
            Database.EnsureCreated();
        }

        public DbSet<UserModel> Users { get; set; }
        public DbSet<RoleModel> Roles { get; set; }
        public DbSet<TenantModel> Tenants { get; set; }
        public DbSet<CustomerModel> Customers { get; set; }
        public DbSet<RefreshTokenModel> RefreshTokens { get; set; }

        public DbSet<SecurityLogModel> SecurityLogs { get; set; }
        public DbSet<SecretModel> Secrets { get; set; }

        // Catálogos (solo lectura)
        public DbSet<IdentificationTypeModel> IdentificationTypes { get; set; }
        public DbSet<RegionModel> Region { get; set; }
        public DbSet<ProvinceModel> Provinces { get; set; }
        public DbSet<CantonModel> Cantons { get; set; }
        public DbSet<DistrictModel> Districts { get; set; }

        //Subscription

        public DbSet<SubscriptionTypeModel> SubscriptionTypes { get; set; }
        public DbSet<SubscriptionHistoryModel> SubscriptionHistory { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

           

            modelBuilder.Entity<SecurityLogModel>(entity =>
            {
                entity.Property(e => e.EventType)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.Email)
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(e => e.IpAddress)
                    .HasMaxLength(50);
            });
          

            modelBuilder.Entity<UserModel>(entity =>
            {
                entity.Property(e => e.SecurityStamp)
                    .HasMaxLength(450); // O el tamaño que prefieras

                entity.Property(e => e.LastPasswordChangeDate)
                    .IsRequired(false);

                entity.Property(e => e.LastSuccessfulLogin)
                    .IsRequired(false);
            });

            modelBuilder.Entity<SubscriptionTypeModel>(entity =>
            {
                entity.ToTable("SubscriptionTypes", "Subscription");
                entity.Property(e => e.Features).HasColumnType("nvarchar(max)");
            });

            modelBuilder.Entity<SubscriptionHistoryModel>(entity =>
            {
                entity.ToTable("SubscriptionHistory", "Subscription");
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            });


            // Aplicar filtro global por tenant
            modelBuilder.Entity<CustomerModel>()
                .HasQueryFilter(x => x.TenantId == _tenantService.GetCurrentTenantId());

        }
    }
}
