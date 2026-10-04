// Adriaan
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using LogiSyn.Model;

namespace LogiSyn.Services
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
        public ApiClient(string baseUrl = "https://localhost:7274")
        {
            // Determine the primary base URL and add a fallback URL based on the port number
            // TODO: replace connection string with render.com string after deployment
            var primary = baseUrl.TrimEnd('/');
            _baseUrls = primary.Contains("7274")
                ? new[] { primary, "http://localhost:5109" }
                : new[] { primary, "https://localhost:7274" };

            // Configure HttpClient to ignore SSL certificate validation for development purposes
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(2)
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
            foreach (var baseUrl in _baseUrls)
            {
                try
                {
                    // Attempt to fetch the order by ID from the current base URL
                    var response = await _httpClient.GetAsync($"{baseUrl}/api/orders/{orderId}");
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
            var json = JsonSerializer.Serialize(status);
            foreach (var baseUrl in _baseUrls)
            {
                try
                {
                    // Attempt to update the order status at the current base URL
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    var response = await _httpClient.PutAsync($"{baseUrl}/api/orders/{orderId}/status", content);
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

