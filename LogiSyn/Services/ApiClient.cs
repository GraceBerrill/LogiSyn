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
            var primary = baseUrl.TrimEnd('/');
            // Support both HTTPS (7274) and HTTP (5109) development profiles
            _baseUrls = primary.Contains("7274")
                ? new[] { primary, "http://localhost:5109" }
                : new[] { primary, "https://localhost:7274" };

            // Ignore SSL certificate errors for localhost development
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
                    var response = await _httpClient.GetAsync($"{baseUrl}/api/orders");
                    if (!response.IsSuccessStatusCode)
                    {
                        continue;
                    }

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
                    // Continue to next candidate URL
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
                    var response = await _httpClient.GetAsync($"{baseUrl}/api/orders/{orderId}");
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
                    // Continue to next candidate URL
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
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync($"{baseUrl}/api/orders", content);
                    if (response.IsSuccessStatusCode)
                        return true;
                }
                catch
                {
                    // Continue to next candidate URL
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
                    // Continue to next candidate URL
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
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    var response = await _httpClient.PutAsync($"{baseUrl}/api/orders/{orderId}/status", content);
                    if (response.IsSuccessStatusCode)
                        return true;
                }
                catch
                {
                    // Continue to next candidate URL
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
                    var response = await _httpClient.GetAsync($"{baseUrl}/api/orders/debug/ping");
                    if (response.IsSuccessStatusCode)
                        return true;
                }
                catch
                {
                    // Continue to next candidate URL
                }
            }

            return false;
        }
    }
}

//--------------------------------------End of File----------------------------------------------------------//

