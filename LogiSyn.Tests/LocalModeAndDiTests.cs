using System;
using System.Collections.Generic;
using System.IO;
using AndersonsBakeryAPI.Data;
using AndersonsBakeryAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedLibrary.Interface;
using SharedLibrary.Model;
using Xunit;

namespace LogiSyn.Tests
{
    public class LocalModeAndDiTests
    {
        [Fact]
        public void LocalMode_DiContainer_ResolvesServicesWithoutMongo()
        {
            // Arrange
            var services = new ServiceCollection();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\MSSQLLocalDB;Database=LogiSynDbTest;Trusted_Connection=True;",
                    ["MongoConnection"] = null,
                    ["MongoDatabase"] = "LogiSynDb"
                })
                .Build();

            services.AddSingleton<IConfiguration>(configuration);

            services.AddDbContext<LogiSynDbContext>(options =>
                options.UseInMemoryDatabase("TestDb_LocalMode"));

            var mongoConn = configuration.GetConnectionString("MongoConnection")
                ?? configuration["MongoConnection"];

            if (!string.IsNullOrWhiteSpace(mongoConn))
            {
                // In local mode without Mongo, this should NOT be registered
                Assert.Fail("MongoConnection was expected to be empty in local mode");
            }

            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<TempRecipeService>();
            services.AddScoped<UserServiceRouter>();
            services.AddScoped<LoginServiceRouter>();

            // Act
            var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();

            var orderService = scope.ServiceProvider.GetService<IOrderService>();
            var productService = scope.ServiceProvider.GetService<IProductService>();
            var recipeService = scope.ServiceProvider.GetService<TempRecipeService>();
            var dbContext = scope.ServiceProvider.GetService<LogiSynDbContext>();

            // Assert
            Assert.NotNull(orderService);
            Assert.NotNull(productService);
            Assert.NotNull(recipeService);
            Assert.NotNull(dbContext);
        }

        [Fact]
        public void LocalMode_Routers_ConstructGracefullyWithoutMongo()
        {
            // Act & Assert - neither should throw InvalidOperationException
            var userException = Record.Exception(() => new UserServiceRouter());
            Assert.Null(userException);

            var loginException = Record.Exception(() => new LoginServiceRouter());
            Assert.Null(loginException);
        }

        [Fact]
        public void LocalMode_ProductService_LoadsProductsWithIngredients()
        {
            // Arrange
            IProductService service = new ProductService();

            // Act
            var products = service.GetAllProducts();

            // Assert
            Assert.NotNull(products);
            Assert.NotEmpty(products);
            Assert.All(products, p =>
            {
                Assert.False(string.IsNullOrWhiteSpace(p.ProductName));
                Assert.NotNull(p.Ingredients);
            });
        }

        [Fact]
        public void LocalMode_OrderService_InstantiatesWithoutMongoConnection()
        {
            // Act
            var service = new OrderService();

            // Assert
            Assert.NotNull(service);
        }
    }
}
