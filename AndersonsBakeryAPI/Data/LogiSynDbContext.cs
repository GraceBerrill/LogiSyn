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

        public DbSet<OrderScaled> Orders { get; set; }

        //------------------------------------------------------------------------------------------------//

        // Configure the model using Fluent API
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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

            // Ignore the ProductionItems and productionItems properties for now
            modelBuilder.Entity<OrderScaled>()
                .Ignore(o => o.ProductionItems);

            modelBuilder.Entity<OrderScaled>()
                .Ignore(o => o.productionItems);

            // Ignore the RawMaterials dictionary for now
            modelBuilder.Entity<OrderScaled>()
                .Ignore(o => o.RawMaterials);
        }
    }
}

//--------------------------------------End of File----------------------------------------------------------//
