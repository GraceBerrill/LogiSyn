using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AndersonsBakeryAPI.Data;
using AndersonsBakeryAPI.Services;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Model;
using Xunit;

namespace LogiSyn.Tests
{
    public class OnlineSyncTests
    {
        [Fact]
        public void OfflineUser_SupportsNullMongoId_TargetingIdPrimaryKey()
        {
            // Arrange
            var offlineUser = new UserRow
            {
                SqlId = "42",
                Id = string.Empty,
                Name = "offline_baker",
                Role = "Baker"
            };

            // Act & Assert
            Assert.False(string.IsNullOrWhiteSpace(offlineUser.SqlId));
            Assert.True(string.IsNullOrEmpty(offlineUser.Id));
            Assert.Equal("offline_baker", offlineUser.Name);
            Assert.Equal("Baker", offlineUser.Role);
        }

        [Fact]
        public void PackagingModel_SupportsIntAndDoubleConversions_Seamlessly()
        {
            // Arrange
            var packaging = new Packaging
            {
                Pans = 12.0,
                Trolleys = 6.0,
                PansUsed = 10.0,
                TrolleysUsed = 5.0
            };

            // Assert
            Assert.Equal(12.0, packaging.Pans);
            Assert.Equal(6.0, packaging.Trolleys);
            Assert.Equal(12, packaging.IntPans);
            Assert.Equal(6, packaging.IntTrolleys);
            Assert.Equal(10.0, packaging.PansUsed);
            Assert.Equal(5.0, packaging.TrolleysUsed);
            Assert.Equal(10, packaging.IntPansUsed);
            Assert.Equal(5, packaging.IntTrolleysUsed);

            // Test integer constructor
            var intPackaging = new Packaging(pans: 24, trolleys: 4, pansUsed: 20, trolleysUsed: 3);

            Assert.Equal(24.0, intPackaging.Pans);
            Assert.Equal(4.0, intPackaging.Trolleys);
            Assert.Equal(24, intPackaging.IntPans);
            Assert.Equal(4, intPackaging.IntTrolleys);
            Assert.Equal(20, intPackaging.IntPansUsed);
            Assert.Equal(3, intPackaging.IntTrolleysUsed);
        }

        [Fact]
        public void ProductionItem_Amount_IsExplicitlySetAndRetained()
        {
            // Arrange & Act
            var item = new ProductionItem
            {
                ProductName = "White Sandwich Bread",
                Amount = 150,
                ProductionLine = "Line 1",
                Packaging = new Packaging(pans: 30, trolleys: 5)
            };

            // Assert
            Assert.Equal("White Sandwich Bread", item.ProductName);
            Assert.Equal(150, item.Amount);
            Assert.Equal(150.0, item.DoubleAmount);
            Assert.Equal(30, item.Packaging.IntPans);
            Assert.Equal(5, item.Packaging.IntTrolleys);
        }

        [Fact]
        public async Task LogiSynDbContext_InitializesWithStandardizedEntities()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<LogiSynDbContext>()
                .UseInMemoryDatabase($"TestDb_SchemaSync_{Guid.NewGuid():N}")
                .Options;

            using var db = new LogiSynDbContext(options);

            // Act
            var user = new UserRow
            {
                SqlId = "1",
                Id = "507f1f77bcf86cd799439011",
                Name = "admin_test",
                Role = "Admin"
            };

            var order = new OrderScaled
            {
                OrderId = "#1001",
                Customer = "Bakery Customer",
                Status = "Pending",
                OrderDate = DateTime.UtcNow
            };

            var product = new Product
            {
                ProductID = 1,
                ProductName = "Sourdough",
                PricePerUnit = 4.50m
            };

            db.Users.Add(user);
            db.Orders.Add(order);
            db.Products.Add(product);
            await db.SaveChangesAsync();

            // Assert
            Assert.Equal(1, await db.Users.CountAsync());
            Assert.Equal(1, await db.Orders.CountAsync());
            Assert.Equal(1, await db.Products.CountAsync());
        }
    }
}
