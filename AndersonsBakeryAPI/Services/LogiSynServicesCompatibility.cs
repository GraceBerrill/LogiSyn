// Compatibility namespace alignment between AndersonsBakeryAPI.Services and LogiSyn.Services
namespace LogiSyn.Services
{
    public class OrderService : AndersonsBakeryAPI.Services.OrderService
    {
        public OrderService() : base() { }
        public OrderService(SharedLibrary.Interface.IProductService productService) : base(productService) { }
        public OrderService(AndersonsBakeryAPI.Repositories.MongoOrderRepository? mongoRepository, AndersonsBakeryAPI.Repositories.SqlOrderRepository? sqlRepository) : base(mongoRepository, sqlRepository) { }
        public OrderService(SharedLibrary.Interface.IProductService productService, AndersonsBakeryAPI.Repositories.MongoOrderRepository? mongoRepository, AndersonsBakeryAPI.Repositories.SqlOrderRepository? sqlRepository) : base(productService, mongoRepository, sqlRepository) { }
    }

    public class ProductService : AndersonsBakeryAPI.Services.ProductService { }
    public class UserService : AndersonsBakeryAPI.Services.UserService { }
    public class LoginService : AndersonsBakeryAPI.Services.LoginService { }
    public class UserServiceRouter : AndersonsBakeryAPI.Services.UserServiceRouter { }
    public class LoginServiceRouter : AndersonsBakeryAPI.Services.LoginServiceRouter { }
    public class ApiClient : AndersonsBakeryAPI.Services.ApiClient { }
    public class TempRecipeService : AndersonsBakeryAPI.Services.TempRecipeService { }
    public class ExcelOrderService : AndersonsBakeryAPI.Services.ExcelOrderService { }
    public class SyncService : AndersonsBakeryAPI.Services.SyncService { }
}

