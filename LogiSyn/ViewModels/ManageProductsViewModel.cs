using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AndersonsBakeryAPI.Services;
using Microsoft.Extensions.DependencyInjection;
using SharedLibrary.Model;

namespace LogiSyn.ViewModels
{
    // ViewModel for product management, handling inventory display, search, and storage location filtering.
    public class ManageProductsViewModel : ViewModelBase
    {
        private readonly ProductService _productService;
        private readonly ApiClient _apiClient;

        private List<ProductRow> _allProducts = new();
        private ObservableCollection<ProductRow> _filteredProducts = new();
        private string _searchQuery = string.Empty;
        private string _selectedStorageFilter = "Storage";
        private string _dateText = DateTime.Now.ToString("yyyy-MM-dd");
        private bool _isLoading;

        public ManageProductsViewModel(ProductService? productService = null, ApiClient? apiClient = null)
        {
            _productService = productService ?? App.ServiceProvider?.GetService<ProductService>() ?? new ProductService();
            _apiClient = apiClient ?? App.ServiceProvider?.GetService<ApiClient>() ?? new ApiClient();
        }

        public ObservableCollection<ProductRow> FilteredProducts
        {
            get => _filteredProducts;
            set => SetProperty(ref _filteredProducts, value);
        }

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value))
                {
                    ApplyFilters();
                }
            }
        }

        public string SelectedStorageFilter
        {
            get => _selectedStorageFilter;
            set
            {
                if (SetProperty(ref _selectedStorageFilter, value))
                {
                    ApplyFilters();
                }
            }
        }

        public string DateText
        {
            get => _dateText;
            set => SetProperty(ref _dateText, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public List<ProductRow> AllProducts => _allProducts;

        // Loads products from API or local service.
        public async Task LoadProductsAsync()
        {
            IsLoading = true;
            try
            {
                try
                {
                    var products = await _apiClient.GetProductsAsync();
                    if (products != null && products.Count > 0)
                    {
                        _allProducts = products.Select(p => new ProductRow
                        {
                            Id = p.Id,
                            ProductId = p.ProductID,
                            Name = p.ProductName,
                            Price = p.PricePerUnit > 0 ? ("R" + p.PricePerUnit.ToString("0.00")) : string.Empty,
                            SellBy = p.SellBy.ToString(),
                            BestBefore = p.BestBefore.ToString(),
                            Storage = p.StorageLocation
                        }).ToList();
                    }
                    else
                    {
                        _allProducts = _productService.GetAll() ?? new List<ProductRow>();
                    }
                }
                catch
                {
                    try { _allProducts = _productService.GetAll() ?? new List<ProductRow>(); }
                    catch { _allProducts = new List<ProductRow>(); }
                }

                ApplyFilters();
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Filters cached products based on search query and storage location.
        public void ApplyFilters()
        {
            string q = (_searchQuery ?? string.Empty).Trim();
            string storage = _selectedStorageFilter ?? "Storage";

            var filtered = _allProducts.Where(p =>
                (q.Length == 0
                    || (p.Name ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                    || p.ProductId.ToString().IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                && (storage == "Storage" || string.Equals(p.Storage, storage, StringComparison.OrdinalIgnoreCase))
            ).ToList();

            FilteredProducts = new ObservableCollection<ProductRow>(filtered);
        }

        // Resolves a ProductDetail record for a given product row.
        public async Task<ProductDetail> GetProductDetailAsync(ProductRow row)
        {
            Product? prod = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(row.Name))
                    prod = await _apiClient.GetProductByIdAsync(row.Name);
            }
            catch { }

            if (prod == null && !string.IsNullOrWhiteSpace(row.Name))
            {
                prod = _productService.GetProductByName(row.Name);
            }

            if (prod != null)
            {
                var ingredients = new List<IngredientLine>();
                if (prod.Ingredients != null)
                {
                    foreach (var ing in prod.Ingredients)
                    {
                        var qtyText = string.Empty;
                        try
                        {
                            if (ing.Quantity > 0) qtyText = ing.Quantity.ToString();
                        }
                        catch { }

                        if (!string.IsNullOrEmpty(ing.Unit))
                        {
                            qtyText = string.IsNullOrEmpty(qtyText) ? ing.Unit : qtyText + " " + ing.Unit;
                        }

                        ingredients.Add(new IngredientLine
                        {
                            Name = ing.IngredientName ?? string.Empty,
                            Quantity = qtyText
                        });
                    }
                }

                return new ProductDetail
                {
                    Id = prod.Id,
                    ProductId = prod.ProductID,
                    Name = prod.ProductName ?? row.Name ?? string.Empty,
                    OriginalName = prod.ProductName ?? row.Name ?? string.Empty,
                    DateAdded = string.Empty,
                    Ingredients = ingredients,
                    Method = prod.Method ?? string.Empty,
                    Storage = prod.StorageLocation ?? row.Storage ?? string.Empty
                };
            }

            return new ProductDetail
            {
                Id = row.Id,
                ProductId = row.ProductId,
                Name = row.Name ?? string.Empty,
                OriginalName = row.Name ?? string.Empty,
                DateAdded = string.Empty,
                Ingredients = new List<IngredientLine>(),
                Method = string.Empty,
                Storage = row.Storage ?? string.Empty
            };
        }

        // Deletes a product from the remote API and local storage.
        public async Task<bool> DeleteProductAsync(ProductRow row)
        {
            bool deleted = false;
            try
            {
                string target = !string.IsNullOrWhiteSpace(row.Name) ? row.Name : row.ProductId.ToString();
                deleted = await _apiClient.DeleteProductAsync(target);
            }
            catch { }

            if (!deleted && !string.IsNullOrWhiteSpace(row.Name))
            {
                _productService.DeleteByName(row.Name);
                deleted = true;
            }

            await LoadProductsAsync();
            return deleted;
        }
    }
}
