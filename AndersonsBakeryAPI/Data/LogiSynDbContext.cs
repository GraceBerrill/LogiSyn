using System.Collections.Generic;
using System.Text.Json;
using SharedLibrary.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

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
        public DbSet<Product> Products { get; set; }
        public DbSet<UserRow> Users { get; set; }


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

            modelBuilder.Entity<OrderScaled>()
                .Property(o => o.Customer)
                .IsRequired()
                .HasMaxLength(255);

            modelBuilder.Entity<OrderScaled>()
                .Property(o => o.OrderDate)
                .IsRequired();

            modelBuilder.Entity<OrderScaled>()
                .Property(o => o.Status)
                .IsRequired()
                .HasMaxLength(50);

            // Database index for frequent date & status filtering
            modelBuilder.Entity<OrderScaled>()
                .HasIndex(o => new { o.OrderDate, o.Status });

            // Persist ProductionItems as serialized JSON in SQL Server
            modelBuilder.Entity<OrderScaled>()
                .Property(o => o.ProductionItems)
                .IsRequired(false)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => string.IsNullOrWhiteSpace(v)
                        ? new List<ProductionItem>()
                        : JsonSerializer.Deserialize<List<ProductionItem>>(v, (JsonSerializerOptions?)null) ?? new List<ProductionItem>()
                )
                .Metadata.SetValueComparer(new ValueComparer<List<ProductionItem>>(
                    (c1, c2) => JsonSerializer.Serialize(c1, (JsonSerializerOptions?)null) ==
                                JsonSerializer.Serialize(c2, (JsonSerializerOptions?)null),
                    c => c == null ? 0 : JsonSerializer.Serialize(c, (JsonSerializerOptions?)null).GetHashCode(),
                    c => JsonSerializer.Deserialize<List<ProductionItem>>(
                            JsonSerializer.Serialize(c, (JsonSerializerOptions?)null),
                            (JsonSerializerOptions?)null) ?? new List<ProductionItem>()
                ));

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
                           )
                           .Metadata.SetValueComparer(new ValueComparer<Dictionary<string, RawMaterialValue>>(
                               (d1, d2) => JsonSerializer.Serialize(d1, (JsonSerializerOptions?)null) ==
                                           JsonSerializer.Serialize(d2, (JsonSerializerOptions?)null),
                               d => d == null ? 0 : JsonSerializer.Serialize(d, (JsonSerializerOptions?)null).GetHashCode(),
                               d => JsonSerializer.Deserialize<Dictionary<string, RawMaterialValue>>(
                                       JsonSerializer.Serialize(d, (JsonSerializerOptions?)null),
                                       (JsonSerializerOptions?)null) ?? new Dictionary<string, RawMaterialValue>()
                           ));
            modelBuilder.Entity<Product>(entity =>
            {
                entity.ToTable("Product");
                entity.HasKey(p => p.ProductID);
                entity.Property(p => p.ProductID).ValueGeneratedOnAdd();
                entity.Property(p => p.ProductName).IsRequired().HasMaxLength(255);
                entity.HasIndex(p => p.ProductName).IsUnique();
                entity.Property(p => p.PricePerUnit).HasPrecision(18, 2);
                entity.Property(p => p.SellBy);
                entity.Property(p => p.BestBefore);
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
                entity.ToTable("User");
                entity.HasKey(u => u.SqlId);
                entity.Property(u => u.SqlId)
                    .HasColumnName("Id")
                    .HasConversion(v => string.IsNullOrEmpty(v) ? 0 : int.Parse(v), v => v.ToString())
                    .ValueGeneratedOnAdd();
                entity.Property(u => u.Name).HasColumnName("Username").IsRequired().HasMaxLength(50);
                entity.HasIndex(u => u.Name).IsUnique();
                entity.Property(u => u.Role).IsRequired().HasMaxLength(50);
                entity.Property(u => u.Password).HasMaxLength(100);
                entity.Property(u => u.Id).HasColumnName("MongoId").HasMaxLength(50).IsRequired(false);
                entity.Property(u => u.DateAdded).HasMaxLength(50);
            });
        }
    }
}//--------------------------------------End of File----------------------------------------------------------//