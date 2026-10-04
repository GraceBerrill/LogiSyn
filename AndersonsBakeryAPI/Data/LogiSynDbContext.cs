// Adriaan
using System.Collections.Generic;
using System.Text.Json;
using SharedLibrary.Model;
using Microsoft.EntityFrameworkCore;

namespace AndersonsBakeryAPI.Data
{
    /// <summary>
    /// Entity Framework Core DbContext for LogiSyn local SQL Server database
    /// </summary>
    public class LogiSynDbContext : DbContext
    {
        public LogiSynDbContext(DbContextOptions<LogiSynDbContext> options) : base(options)
        {
        }

        //------------------------------------------------------------------------------------------------//

        //Orders DbSet
        public DbSet<OrderScaled> Orders { get; set; }
        public DbSet<Product> products { get; set; }
        public DbSet<UserRow> users { get; set; }


        //------------------------------------------------------------------------------------------------//

        // Configure the model using Fluent API
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Adriaan - Order Model Configuration
            // Configure the OrderScaled entity
            modelBuilder.Entity<OrderScaled>()
                .HasKey(o => o.OrderId);
            // Configure properties with appropriate constraints
            modelBuilder.Entity<OrderScaled>()
                .Property(o => o.OrderId)
                .IsRequired()
                .HasMaxLength(255);
            // Configure other properties as needed
            modelBuilder.Entity<OrderScaled>()
                .Property(o => o.Customer)
                .IsRequired()
                .HasMaxLength(255);
            //  Configure the OrderDate property
            modelBuilder.Entity<OrderScaled>()
                .Property(o => o.OrderDate)
                .IsRequired();
            // Configure the Status property
            modelBuilder.Entity<OrderScaled>()
                .Property(o => o.Status)
                .IsRequired()
                .HasMaxLength(50);

            // Persist ProductionItems as serialized JSON in SQL Server
            modelBuilder.Entity<OrderScaled>()
                .Property(o => o.ProductionItems)
                .IsRequired(false)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => string.IsNullOrWhiteSpace(v) 
                        ? new List<ProductionItem>() 
                        : JsonSerializer.Deserialize<List<ProductionItem>>(v, (JsonSerializerOptions?)null) ?? new List<ProductionItem>()
                );

            modelBuilder.Entity<OrderScaled>()
                .Ignore(o => o.productionItems);

            // Persist RawMaterials as serialized JSON in SQL Server
            modelBuilder.Entity<OrderScaled>()
                .Property(o => o.RawMaterials)
                .IsRequired(false)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => string.IsNullOrWhiteSpace(v) 
                        ? new Dictionary<string, RawMaterialValue>() 
                        : JsonSerializer.Deserialize<Dictionary<string, RawMaterialValue>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, RawMaterialValue>()
                );

            modelBuilder.Entity<Product>(entity =>
            {
                entity.ToTable("Products");
                entity.HasKey(p => p.ProductID);
                entity.Property(p => p.ProductName).IsRequired().HasMaxLength(255);
                entity.Property(p => p.PricePerUnit).HasPrecision(18, 2);
                entity.Property(p => p.StorageLocation).HasMaxLength(50);
                entity.Property(p => p.Method).HasMaxLength(500);
                // Serialize Ingredients as JSON in SQL Server (resolves IngredientRequirement key error)
                entity.Property(p => p.Ingredients)
                    .IsRequired(false)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => string.IsNullOrWhiteSpace(v)
                            ? new List<IngredientRequirement>()
                            : JsonSerializer.Deserialize<List<IngredientRequirement>>(v, (JsonSerializerOptions?)null) ?? new List<IngredientRequirement>()
                    );
            });

            modelBuilder.Entity<UserRow>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Id).HasMaxLength(50);
                entity.Property(u => u.Name).IsRequired().HasMaxLength(100);
                entity.Property(u => u.Role).IsRequired().HasMaxLength(50);
                entity.Property(u => u.Password).HasMaxLength(255);
                entity.Property(u => u.DateAdded).HasMaxLength(50);
                entity.Ignore(u => u.SqlId);
            });
        }
    }
}

//--------------------------------------End of File----------------------------------------------------------//
