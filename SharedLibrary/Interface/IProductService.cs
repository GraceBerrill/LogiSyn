using SharedLibrary.Model;
using System.Collections.Generic;

namespace SharedLibrary.Interface
{
    public interface IProductService
    {
        List<Product> GetAllProducts();
        List<ProductRow> GetAll();
        void Add(Product product);
        void Add(ProductRow row);
        void UpdateFromDetail(Product product);
        Product? GetProductByName(string name);
        void DeleteByName(string name);
    }
}

