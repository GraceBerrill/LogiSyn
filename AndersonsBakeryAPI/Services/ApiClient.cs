// Adriaan
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using SharedLibrary.Model;

namespace AndersonsBakeryAPI.Services
{
    /// <summary>
    /// HTTP client service for communicating with AndersonsBakeryAPI
    /// </summary>
    public class ApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string[] _baseUrls;

        //------------------------------------------------------------------------------------------------//



        // Constructor for the ApiClient class
        public ApiClient(string baseUrl = "https://andersons-bakery-api.onrender.com")
        {

            var primary = baseUrl.TrimEnd('/');

            // Prioritize the live Render cloud API, with local ports as offline fallbacks
            _baseUrls = new[]
            {
                primary,                   
                "https://localhost:7274",  
                "http://localhost:5109"    
            };            
            
            // Configure HttpClient with a longer timeout. Do not disable SSL validation.
            _httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(60)
            };
        }

        //------------------------------------------------------------------------------------------------//

        // Fetches all orders from the API
        public async Task<List<OrderScaled>> GetOrdersAsync()
        {
            foreach (var baseUrl in _baseUrls)
            {
                try
                {
                    // Attempt to fetch orders from the current base URL
                    var response = await _httpClient.GetAsync($"{baseUrl}/api/orders");
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Failed to fetch orders from {baseUrl}. Trying next candidate URL.");
                        continue;
                    }

                    // Read the response content and deserialize it into a list of OrderScaled objects
                    var content = await response.Content.ReadAsStringAsync();
                    var orders = JsonSerializer.Deserialize<List<OrderScaled>>(content, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });
                    if (orders != null)
                        return orders;
                }
                catch
                {
                    Console.WriteLine($"Failed to fetch orders from {baseUrl}. Trying next candidate URL.");
                }
            }

            return new List<OrderScaled>();
        }

        //------------------------------------------------------------------------------------------------//

        // Fetches a specific order by ID from the API
        public async Task<OrderScaled?> GetOrderByIdAsync(string orderId)
        {
            string safeId = Uri.EscapeDataString(orderId ?? string.Empty);
            foreach (var baseUrl in _baseUrls)
            {
                try
                {
                    // Attempt to fetch the order by ID from the current base URL
                    var response = await _httpClient.GetAsync($"{baseUrl}/api/orders/{safeId}");
                    if (!response.IsSuccessStatusCode)
                        continue;

                    // Read the response content and deserialize it into an OrderScaled object
                    var content = await response.Content.ReadAsStringAsync();
                    var order = JsonSerializer.Deserialize<OrderScaled>(content, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });
                    if (order != null)
                        return order;
                }
                catch
                {
                    Console.WriteLine($"Failed to fetch order {orderId} from {baseUrl}. Trying next candidate URL.");
                }
            }

            return null;
        }

        //------------------------------------------------------------------------------------------------//

        // Saves a new order to the API
        public async Task<bool> SaveOrderAsync(OrderScaled order)
        {
            var json = JsonSerializer.Serialize(order);
            foreach (var baseUrl in _baseUrls)
            {
                try
                {
                    // Attempt to save the order to the current base URL
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync($"{baseUrl}/api/orders", content);
                    if (response.IsSuccessStatusCode)
                        return true;
                }
                catch
                {
                    Console.WriteLine($"Failed to save order {order.OrderId} to {baseUrl}. Trying next candidate URL.");
                }
            }

            return false;
        }

        //------------------------------------------------------------------------------------------------//

        // Fetch a single product by id
        public async Task<Product?> GetProductByIdAsync(string productId)
        {
            string safeId = Uri.EscapeDataString(productId ?? string.Empty);
            foreach (var baseUrl in _baseUrls)
            {
                try
                {
                    var response = await _httpClient.GetAsync($"{baseUrl}/api/products/{safeId}");
                    if (!response.IsSuccessStatusCode) continue;
                    var body = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<Product>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch { }
            }
            return null;
        }

        // Create a new product via the API
        public async Task<bool> CreateProductAsync(Product product)
        {
            foreach (var baseUrl in _baseUrls)
            {
                try
                {
                    var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/api/products", product);
                    if (response.IsSuccessStatusCode) return true;
                }
                catch { }
            }
            return false;
        }

        // Update an existing product via the API
        public async Task<bool> UpdateProductAsync(string productId, Product product)
        {
            string safeId = Uri.EscapeDataString(productId ?? string.Empty);
            foreach (var baseUrl in _baseUrls)
            {
                try
                {
                    var response = await _httpClient.PutAsJsonAsync($"{baseUrl}/api/products/{safeId}", product);
                    if (response.IsSuccessStatusCode) return true;
                }
                catch { }
            }
            return false;
        }

        // Delete a product via the API
        public async Task<bool> DeleteProductAsync(string productId)
        {
            string safeId = Uri.EscapeDataString(productId ?? string.Empty);
            foreach (var baseUrl in _baseUrls)
            {
                try
                {
                    var response = await _httpClient.DeleteAsync($"{baseUrl}/api/products/{safeId}");
                    if (response.IsSuccessStatusCode) return true;
                }
                catch { }
            }
            return false;
        }

        //------------------------------------------------------------------------------------------------//

        // Fetch a single user by id
        public async Task<UserRow?> GetUserByIdAsync(string userId)
        {
            string safeId = Uri.EscapeDataString(userId ?? string.Empty);
            foreach (var baseUrl in _baseUrls)
            {
                try
                {
                    var response = await _httpClient.GetAsync($"{baseUrl}/api/users/{safeId}");
                    if (!response.IsSuccessStatusCode) continue;
                    var body = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<UserRow>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch { }
            }
            return null;
        }

        // Create a new user via the API
        public async Task<bool> CreateUserAsync(UserRow user)
        {
            foreach (var baseUrl in _baseUrls)
            {
                try
                {
                    var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/api/users", user);
                    if (response.IsSuccessStatusCode) return true;
                }
                catch { }
            }
            return false;
        }

        // Update an existing user via the API
        public async Task<bool> UpdateUserAsync(string userId, UserRow user)
        {
            string safeId = Uri.EscapeDataString(userId ?? string.Empty);
            foreach (var baseUrl in _baseUrls)
            {
                try
                {
                    var response = await _httpClient.PutAsJsonAsync($"{baseUrl}/api/users/{safeId}", user);
                    if (response.IsSuccessStatusCode) return true;
                }
                catch { }
            }
            return false;
        }

        // Delete a user via the API
        public async Task<bool> DeleteUserAsync(string userId)
        {
            string safeId = Uri.EscapeDataString(userId ?? string.Empty);
            foreach (var baseUrl in _baseUrls)
            {
                try
                {
                    var response = await _httpClient.DeleteAsync($"{baseUrl}/api/users/{safeId}");
                    if (response.IsSuccessStatusCode) return true;
                }
                catch { }
            }
            return false;
        }

        //------------------------------------------------------------------------------------------------//

        // Parses a PDF order file via the API
        public async Task<OrderScaled?> ParsePdfOrderAsync(string filePath)
        {
            if (!File.Exists(filePath)) return null;

            foreach (var baseUrl in _baseUrls)
            {
                try
                {
                    // Prepare the multipart form data content for the PDF file
                    using var form = new MultipartFormDataContent();
                    await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    using var streamContent = new StreamContent(fileStream);
                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
                    form.Add(streamContent, "file", Path.GetFileName(filePath));

                    var response = await _httpClient.PostAsync($"{baseUrl}/api/orders/parse", form);
                    if (!response.IsSuccessStatusCode)
                        continue;

                    var content = await response.Content.ReadAsStringAsync();
                    var order = JsonSerializer.Deserialize<OrderScaled>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (order != null)
                        return order;
                }
                catch
                {
                    Console.WriteLine($"Failed to parse PDF order from {baseUrl}. Trying next candidate URL.");
                }
            }

            return null;
        }

        //------------------------------------------------------------------------------------------------//

        // Updates the status of an order via the API
        public async Task<bool> UpdateOrderStatusAsync(string orderId, string status)
        {
            string safeId = Uri.EscapeDataString(orderId ?? string.Empty);
            var json = JsonSerializer.Serialize(status);
            foreach (var baseUrl in _baseUrls)
            {
                try
                {
                    // Attempt to update the order status at the current base URL
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    var response = await _httpClient.PutAsync($"{baseUrl}/api/orders/{safeId}/status", content);
                    if (response.IsSuccessStatusCode)
                        return true;
                }
                catch
                {
                    Console.WriteLine($"Failed to update status for order {orderId} at {baseUrl}. Trying next candidate URL.");
                }
            }

            return false;
        }

        //------------------------------------------------------------------------------------------------//

        // Authenticates a user against the API
        public async Task<UserRow?> AuthenticateAsync(string username, string password)
        {
            var payload = JsonSerializer.Serialize(new { username, password });
            foreach (var baseUrl in _baseUrls)
            {
                try
                {
                    var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync($"{baseUrl}/api/auth/login", content);
                    if (response.IsSuccessStatusCode)
                    {
                        var body = await response.Content.ReadAsStringAsync();
                        return JsonSerializer.Deserialize<UserRow>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                }
                catch { }
            }
            return null;
        }

        // Fetches all products from the API
        public async Task<List<Product>> GetProductsAsync()
        {
            foreach (var baseUrl in _baseUrls)
            {
                try
                {
                    var response = await _httpClient.GetAsync($"{baseUrl}/api/products");
                    if (response.IsSuccessStatusCode)
                    {
                        var body = await response.Content.ReadAsStringAsync();
                        return JsonSerializer.Deserialize<List<Product>>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<Product>();
                    }
                }
                catch { }
            }
            return new List<Product>();
        }

        // Fetches all users from the API
        public async Task<List<UserRow>> GetUsersAsync()
        {
            foreach (var baseUrl in _baseUrls)
            {
                try
                {
                    var response = await _httpClient.GetAsync($"{baseUrl}/api/users");
                    if (response.IsSuccessStatusCode)
                    {
                        var body = await response.Content.ReadAsStringAsync();
                        return JsonSerializer.Deserialize<List<UserRow>>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<UserRow>();
                    }
                }
                catch { }
            }
            return new List<UserRow>();
        }

        //------------------------------------------------------------------------------------------------//

        // Checks if the API is available
        public async Task<bool> IsApiAvailableAsync()
        {
            foreach (var baseUrl in _baseUrls)
            {
                try
                {
                    // Attempt to ping the API at the current base URL
                    var response = await _httpClient.GetAsync($"{baseUrl}/api/orders/debug/ping");
                    if (response.IsSuccessStatusCode)
                        return true;
                }
                catch
                {
                    Console.WriteLine($"Failed to ping API at {baseUrl}. Trying next candidate URL.");
                }
            }

            return false;
        }
    }
}

//--------------------------------------End of File----------------------------------------------------------//

