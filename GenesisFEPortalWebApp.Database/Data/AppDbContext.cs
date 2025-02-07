using GenesisFEPortalWebApp.Models.Entities.Catalog;
using GenesisFEPortalWebApp.Models.Entities.Core;
using GenesisFEPortalWebApp.Models.Entities.Security;
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

      
        //public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        //{
        //    try
        //    {
        //        var tenantId = _tenantService.GetCurrentTenantId();

        //        // Si tenantId es 0, significa que estamos en proceso de login/registro
        //        if (tenantId != 0)
        //        {
        //            foreach (var entry in ChangeTracker.Entries<IHasTenant>().ToList())
        //            {
        //                switch (entry.State)
        //                {
        //                    case EntityState.Added:
        //                    case EntityState.Modified:
        //                        entry.Entity.TenantId = tenantId;
        //                        break;
        //                }
        //            }
        //        }
        //        return base.SaveChangesAsync(cancellationToken);
        //    }
        //    catch (Exception)
        //    {
        //        // Manejar el caso donde no hay tenant (login/registro)
        //        return base.SaveChangesAsync(cancellationToken);
        //    }
        //}
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //modelBuilder.Entity<IdentificationTypeModel>()
            //    .HasMany(e => e.Customers)
            //    .WithOne(e => e.IdentificationType)
            //    .OnDelete(DeleteBehavior.Restrict);

            //modelBuilder.Entity<DistrictModel>()
            //    .HasMany(e => e.CustomerModels)
            //    .WithOne(e => e.District)
            //    .OnDelete(DeleteBehavior.Restrict);

            //modelBuilder.Entity<ProvinceModel>()
            //    .HasMany(e => e.Cantons)
            //    .WithOne(e => e.Province)
            //    .OnDelete(DeleteBehavior.Restrict);

            //modelBuilder.Entity<CantonModel>().HasKey(c => c.CantonID);

            //modelBuilder.Entity<CantonModel>()
            //.HasMany(e => e.Districts)
            //.WithOne(e => e.Canton)
            //.OnDelete(DeleteBehavior.Restrict);

            //modelBuilder.Entity<RegionModel>()
            //    .HasMany(e => e.Districts)
            //    .WithOne(e => e.Region)
            //    .OnDelete(DeleteBehavior.Restrict);


            //modelBuilder.Entity<TenantModel>()
            //      .HasMany(e => e.Users)
            //      .WithOne(e => e.Tenant)
            //      .OnDelete(DeleteBehavior.Restrict);


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

            //modelBuilder.Entity<SecretModel>(entity =>
            //{
            //    entity.ToTable("Secrets", "Security");

            //    entity.HasIndex(e => new { e.TenantId, e.Key, e.UserId })
            //        .IsUnique()
            //        .HasFilter("[UserId] IS NOT NULL");

            //    entity.HasOne(e => e.Tenant)
            //        .WithMany()
            //        .HasForeignKey(e => e.TenantId)
            //        .OnDelete(DeleteBehavior.Restrict);

            //    entity.HasOne(e => e.User)
            //        .WithMany()
            //        .HasForeignKey(e => e.UserId)
            //        .OnDelete(DeleteBehavior.Restrict);
            //});

            modelBuilder.Entity<UserModel>(entity =>
            {
                entity.Property(e => e.SecurityStamp)
                    .HasMaxLength(450); // O el tamaño que prefieras

                entity.Property(e => e.LastPasswordChangeDate)
                    .IsRequired(false);

                entity.Property(e => e.LastSuccessfulLogin)
                    .IsRequired(false);
            });

            // Aplicar filtro global por tenant
            modelBuilder.Entity<CustomerModel>()
                .HasQueryFilter(x => x.TenantId == _tenantService.GetCurrentTenantId());

        }
    }
}
