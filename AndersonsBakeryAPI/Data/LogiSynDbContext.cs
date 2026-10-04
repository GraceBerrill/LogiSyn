// Adriaan
using System.Collections.Generic;
using System.Text.Json;
using LogiSyn.Model;
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
        }
    }
}

//--------------------------------------End of File----------------------------------------------------------//
