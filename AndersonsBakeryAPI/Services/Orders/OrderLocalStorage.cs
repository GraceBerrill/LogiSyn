using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using SharedLibrary.Model;

namespace AndersonsBakeryAPI.Services
{
    // Local JSON cache and persistence manager for production orders
    public class OrderLocalStorage
    {
        private readonly List<OrderScaled> _orders = new();
        private volatile bool _localLoaded = false;
        private readonly object _lock = new();

        // Returns the file path for storing local orders in a JSON file
        public string OrdersFilePath()
        {
            string dataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (!Directory.Exists(dataFolder)) Directory.CreateDirectory(dataFolder);
            return Path.Combine(dataFolder, "orders.json");
        }

        // Loads orders from the local JSON file into memory, ensuring no duplicates are added
        public void EnsureLoaded()
        {
            if (_localLoaded) return;

            lock (_lock)
            {
                if (_localLoaded) return;
                try
                {
                    string path = OrdersFilePath();
                    if (File.Exists(path))
                    {
                        string json = File.ReadAllText(path);
                        var items = JsonSerializer.Deserialize<List<OrderScaled>>(json, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        if (items != null)
                        {
                            foreach (var item in items)
                            {
                                if (!_orders.Any(o => o.OrderId.Equals(item.OrderId, StringComparison.OrdinalIgnoreCase)))
                                {
                                    _orders.Add(item);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"[LOCAL] Error loading local orders.json: {ex.Message}");
                }
                _localLoaded = true;
            }
        }

        // Saves the current in-memory orders to the local JSON file, ensuring thread safety
        public void SaveLocalOrders()
        {
            try
            {
                string path = OrdersFilePath();
                string json;
                lock (_lock)
                {
                    json = JsonSerializer.Serialize(_orders, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                }
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[LOCAL] Error saving local orders.json: {ex.Message}");
            }
        }

        // Helper to merge incoming orders into the in-memory cache without duplicates
        public void MergeOrders(IEnumerable<OrderScaled> incomingOrders)
        {
            lock (_lock)
            {
                foreach (var incoming in incomingOrders)
                {
                    int index = _orders.FindIndex(o => o.OrderId.Equals(incoming.OrderId, StringComparison.OrdinalIgnoreCase));
                    if (index >= 0)
                    {
                        _orders[index] = incoming;
                    }
                    else
                    {
                        _orders.Add(incoming);
                    }
                }
            }
        }

        // Replaces or adds an order in memory
        public void UpsertOrder(OrderScaled order)
        {
            lock (_lock)
            {
                _orders.RemoveAll(o => o.OrderId.Equals(order.OrderId, StringComparison.OrdinalIgnoreCase));
                _orders.Add(order);
            }
        }

        // Returns a snapshot copy of all in-memory orders
        public List<OrderScaled> GetSnapshot()
        {
            EnsureLoaded();
            lock (_lock)
            {
                return _orders.ToList();
            }
        }

        // Invalidates the in-memory cache
        public void InvalidateCache()
        {
            lock (_lock)
            {
                _orders.Clear();
                _localLoaded = false;
            }
        }
    }
}
/*********************************************MAR26EOF*******************************************/