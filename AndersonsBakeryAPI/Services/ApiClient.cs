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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AndersonsBakeryAPI.Services
{
    public class ApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string[] _baseUrls;

        //------------------------------------------------------------------------------------------------//



        // Constructor for the ApiClient class - HttpClient provided by IHttpClientFactory
        private readonly ILogger<ApiClient>? _logger;

        public ApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<ApiClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
            _logger = logger;

            var configured = configuration?.GetSection("ApiClient:BaseUrls").Get<string[]>();
            if (configured != null && configured.Length > 0)
            {
                _baseUrls = configured.Select(u => u?.TrimEnd('/') ?? string.Empty).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            }
            else
            {
                _baseUrls = new[]
                {
                    "https://andersons-bakery-api.onrender.com",
                    "https://localhost:7274",
                    "http://localhost:5109"
                };
            }
        }

        // Legacy / convenience constructor: parameterless for desktop client usage
        public ApiClient()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            _baseUrls = new[] { "https://andersons-bakery-api.onrender.com" };
        }

        // Legacy / convenience constructor: allow creating with a single base URL (used by WPF startup)
        public ApiClient(string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl)) baseUrl = "https://andersons-bakery-api.onrender.com";
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            _baseUrls = new[] { baseUrl.TrimEnd('/') };
        }

        public static event Action<ApiConnectionState, string?>? OnStatusChanged;
        public static ApiConnectionState CurrentState { get; private set; } = ApiConnectionState.Online;
        public static string CurrentMessage { get; private set; } = "API Connected";

        // JWT token stored after successful login; attached as Bearer header to every request
        private static string? _jwtToken;

        /// <summary>Stores the JWT token and applies it to the HttpClient default headers.</summary>
        public void SetAuthToken(string token)
        {
            _jwtToken = token;
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        /// <summary>Clears the stored JWT token (call on logout).</summary>
        public static void ClearAuthToken() => _jwtToken = null;

        private static int _activeRequestCount = 0;

        public static void NotifyStatus(ApiConnectionState state, string? message = null)
        {
            CurrentState = state;
            CurrentMessage = message ?? (state switch
            {
                ApiConnectionState.CallingApi => "Calling API…",
                ApiConnectionState.Online => "API Connected",
                ApiConnectionState.FallbackLocal => "Local Storage (Fallback)",
                _ => "Ready"
            });

            try
            {
                OnStatusChanged?.Invoke(state, CurrentMessage);
            }
            catch { }
        }

        private static void BeginRequest(string message = "Calling API…")
        {
            System.Threading.Interlocked.Increment(ref _activeRequestCount);
            NotifyStatus(ApiConnectionState.CallingApi, message);
        }

        private static void EndRequest(bool success, string? successMessage = null, string? failureMessage = null)
        {
            int remaining = System.Threading.Interlocked.Decrement(ref _activeRequestCount);
            if (remaining > 0)
            {
                return;
            }

            if (success)
            {
                NotifyStatus(ApiConnectionState.Online, successMessage ?? "API Connected");
            }
            else
            {
                NotifyStatus(ApiConnectionState.FallbackLocal, failureMessage ?? "Local Storage (Fallback)");
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Fetches all orders from the API
        public async Task<List<OrderScaled>> GetOrdersAsync()
        {
            BeginRequest("Fetching orders from API…");
            bool succeeded = false;
            try
            {
                foreach (var baseUrl in _baseUrls)
                {
                    try
                    {
                        // Attempt to fetch orders from the current base URL
                        var response = await _httpClient.GetAsync($"{baseUrl}/api/orders");
                        if (!response.IsSuccessStatusCode)
                        {
                            _logger?.LogWarning("Failed to fetch orders from {BaseUrl} (Status: {Status}). Trying next candidate URL.", baseUrl, (int)response.StatusCode);
                            continue;
                        }

                        // Read the response content and deserialize it into a list of OrderScaled objects
                        var content = await response.Content.ReadAsStringAsync();
                        var orders = JsonSerializer.Deserialize<List<OrderScaled>>(content, new JsonSerializerOptions 
                        { 
                            PropertyNameCaseInsensitive = true 
                        });
                        if (orders != null)
                        {
                            succeeded = true;
                            return orders;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to fetch orders from {BaseUrl}. Trying next candidate URL.", baseUrl);
                    }
                }

                return new List<OrderScaled>();
            }
            finally
            {
                EndRequest(succeeded, "API Connected", "Local Storage (Fallback)");
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Fetches a specific order by ID from the API
        public async Task<OrderScaled?> GetOrderByIdAsync(string orderId)
        {
            BeginRequest("Loading order from API…");
            bool succeeded = false;
            try
            {
                string safeId = Uri.EscapeDataString(orderId ?? string.Empty);
                foreach (var baseUrl in _baseUrls)
                {
                    try
                    {
                        // Attempt to fetch the order by ID from the current base URL
                        var response = await _httpClient.GetAsync($"{baseUrl}/api/orders/{safeId}");
                        if (!response.IsSuccessStatusCode)
                        {
                            _logger?.LogDebug("GetOrderById failed at {BaseUrl} with status {Status}", baseUrl, (int)response.StatusCode);
                            continue;
                        }

                        // Read the response content and deserialize it into an OrderScaled object
                        var content = await response.Content.ReadAsStringAsync();
                        var order = JsonSerializer.Deserialize<OrderScaled>(content, new JsonSerializerOptions 
                        { 
                            PropertyNameCaseInsensitive = true 
                        });
                        if (order != null)
                        {
                            succeeded = true;
                            return order;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to fetch order {OrderId} from {BaseUrl}. Trying next candidate URL.", orderId, baseUrl);
                    }
                }

                return null;
            }
            finally
            {
                EndRequest(succeeded, "API Connected", "Local Storage (Fallback)");
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Saves a new order to the API
        public async Task<bool> SaveOrderAsync(OrderScaled order)
        {
            BeginRequest("Saving order to API…");
            bool succeeded = false;
            try
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
                        {
                            succeeded = true;
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to save order {OrderId} to {BaseUrl}. Trying next candidate URL.", order.OrderId, baseUrl);
                    }
                }

                return false;
            }
            finally
            {
                EndRequest(succeeded, "API Connected", "Local Storage (Fallback)");
            }
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
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger?.LogDebug("GetProductById returned non-success {Status} at {BaseUrl}", (int)response.StatusCode, baseUrl);
                        continue;
                    }
                    var body = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<Product>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error fetching product by id {ProductId} from {BaseUrl}", productId, baseUrl);
                }
            }
            return null;
        }

        // Fetch a single product by name
        public async Task<Product?> GetProductByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            foreach (var baseUrl in _baseUrls)
            {
                try
                {
                    string encodedName = Uri.EscapeDataString(name.Trim());
                    var response = await _httpClient.GetAsync($"{baseUrl}/api/products/{encodedName}");
                    if (!response.IsSuccessStatusCode) continue;
                    var body = await response.Content.ReadAsStringAsync();
                    if (string.IsNullOrWhiteSpace(body)) continue;
                    return JsonSerializer.Deserialize<Product>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error fetching product by name '{Name}' from {BaseUrl}", name, baseUrl);
                }
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
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error creating product at {BaseUrl}", baseUrl);
                }
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
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error updating product {ProductId} at {BaseUrl}", productId, baseUrl);
                }
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
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error deleting product {ProductId} at {BaseUrl}", productId, baseUrl);
                }
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
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger?.LogDebug("GetUserById returned non-success {Status} at {BaseUrl}", (int)response.StatusCode, baseUrl);
                        continue;
                    }
                    var body = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<UserRow>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error fetching user by id {UserId} from {BaseUrl}", userId, baseUrl);
                }
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
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error creating user at {BaseUrl}", baseUrl);
                }
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
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error updating user {UserId} at {BaseUrl}", userId, baseUrl);
                }
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
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error deleting user {UserId} at {BaseUrl}", userId, baseUrl);
                }
            }
            return false;
        }

        //------------------------------------------------------------------------------------------------//

        // Parses a PDF order file via the API
        public async Task<OrderScaled?> ParsePdfOrderAsync(string filePath)
        {
            if (!File.Exists(filePath)) return null;

            BeginRequest("Parsing PDF via API…");
            bool succeeded = false;
            try
            {
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
                        {
                            succeeded = true;
                            return order;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to parse PDF order from {BaseUrl}. Trying next candidate URL.", baseUrl);
                    }
                }

                return null;
            }
            finally
            {
                EndRequest(succeeded, "API Connected", "Local Storage (Fallback)");
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Updates the status of an order via the API
        public async Task<bool> UpdateOrderStatusAsync(string orderId, string status)
        {
            BeginRequest("Updating order status via API…");
            bool succeeded = false;
            try
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
                        {
                            succeeded = true;
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to update status for order {OrderId} at {BaseUrl}. Trying next candidate URL.", orderId, baseUrl);
                    }
                }

                return false;
            }
            finally
            {
                EndRequest(succeeded, "API Connected", "Local Storage (Fallback)");
            }
        }

        //------------------------------------------------------------------------------------------------//

        // Authenticates a user against the API and stores the JWT token for subsequent requests
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
                        // Response shape: { "token": "...", "user": { "id", "sqlId", "name", "role" } }
                        using var doc = System.Text.Json.JsonDocument.Parse(body);
                        var root = doc.RootElement;

                        string? token = root.TryGetProperty("token", out var tokenEl) ? tokenEl.GetString() : null;
                        if (!string.IsNullOrWhiteSpace(token))
                            SetAuthToken(token);

                        if (root.TryGetProperty("user", out var userEl))
                        {
                            return new UserRow
                            {
                                Id     = userEl.TryGetProperty("id",    out var idEl)    ? idEl.GetString()    ?? string.Empty : string.Empty,
                                SqlId  = userEl.TryGetProperty("sqlId", out var sqEl)    ? sqEl.GetString()    ?? string.Empty : string.Empty,
                                Name   = userEl.TryGetProperty("name",  out var nameEl)  ? nameEl.GetString()  ?? string.Empty : string.Empty,
                                Role   = userEl.TryGetProperty("role",  out var roleEl)  ? roleEl.GetString()  ?? string.Empty : string.Empty,
                            };
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error authenticating against {BaseUrl}", baseUrl);
                }
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
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error fetching products from {BaseUrl}", baseUrl);
                }
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
                    // Throw on auth failure so the caller's catch can fall back to local data
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                        response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        throw new UnauthorizedAccessException($"API returned {(int)response.StatusCode} for /api/users.");
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    throw; // propagate auth errors to trigger local fallback
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error fetching users from {BaseUrl}", baseUrl);
                }
            }
            throw new InvalidOperationException("Could not reach any API endpoint for /api/users.");
        }

        //------------------------------------------------------------------------------------------------//

        // Checks if the API is available
        public async Task<bool> IsApiAvailableAsync()
        {
            BeginRequest("Checking API status…");
            bool succeeded = false;
            try
            {
                foreach (var baseUrl in _baseUrls)
                {
                    try
                    {
                        // Ping /health — unauthenticated, safe to call before login
                        var response = await _httpClient.GetAsync($"{baseUrl}/health");
                        if (response.IsSuccessStatusCode)
                        {
                            succeeded = true;
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to ping API at {BaseUrl}. Trying next candidate URL.", baseUrl);
                    }
                }

                return false;
            }
            finally
            {
                EndRequest(succeeded, "API Connected", "Local Storage (Fallback)");
            }
        }
    }
}

//--------------------------------------End of File----------------------------------------------------------//