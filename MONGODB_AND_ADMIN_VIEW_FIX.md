# MongoDB and Admin Order View Fix - Troubleshooting Guide

## Summary of Changes

### ✅ What Was Fixed

1. **Removed Static Data from AdminOrdersView**
   - AdminOrdersView no longer loads `SampleData.Orders()`
   - Now fetches orders dynamically from the API every 10 seconds
   - Displays newly created orders automatically

2. **Created ApiClient Service**
   - New HTTP client for communicating with AndersonsBakeryAPI
   - Handles order fetching, saving, and API availability checks
   - Located at: `LogiSyn/Services/ApiClient.cs`

3. **Added OrderRow Factory Method**
   - `OrderRow.FromOrderScaled()` converts API orders to UI format
   - Handles status mapping (Completed → Complete)

4. **Enhanced Debugging**
   - Added comprehensive logging to OrderService and MongoOrderRepository
   - Debug endpoint: `GET /api/orders/debug/ping`
   - Console output shows [API], [MONGODB], [SQL] tags for easy debugging

## How to Test

### 1. Verify API is Running
```
Open browser: https://localhost:7043/api/orders/debug/ping
Expected: { "timestamp": "...", "message": "API is running", "orderCount": 0 }
```

### 2. Check Visual Studio Output Window
- Look for logs tagged with `[API]`, `[MONGODB]`, `[MONGO_REPO]`
- This shows the flow of order saves

### 3. Test Order Creation and Display
1. In the desktop app, upload a PDF to create an order
2. Check Visual Studio Output for:
   ```
   [API] POST /api/orders called with SaveOrder
   [MONGODB] Saving order #001 to MongoDB...
   [MONGO_REPO] Attempting to save order #001
   [MONGO_REPO] ✓ Order #001 saved successfully
   ```
3. Check AdminOrdersView - new order should appear within 10 seconds

## Debugging MongoDB Persistence Issues

If orders aren't appearing in MongoDB:

### Check 1: API Connection String
**File:** `AndersonsBakeryAPI/appsettings.json`
```json
"ConnectionStrings": {
  "MongoDb": "mongodb+srv://reannaude1_db_user:MPaJYcEqumlJbf0j@cluster0.twltvce.mongodb.net/?appName=Cluster0"
}
```
- Verify credentials are correct
- Verify cluster URL matches your MongoDB Atlas cluster
- Check that IP address is whitelisted in MongoDB Atlas

### Check 2: Network Connectivity
```powershell
# Test MongoDB connection from PowerShell
Test-NetConnection cluster0.twltvce.mongodb.net -Port 27017
```

### Check 3: Visual Studio Output Window
When saving an order, look for these messages in Debug output:

**Success path:**
```
[API] POST /api/orders called with SaveOrder
[API] Saving order: ID=#001, Customer=Checkers, Status=Pending
[MONGODB] Saving order #001 to MongoDB...
[MONGODB] Order details: Customer=Checkers, Status=Pending, OrderDate=2026-05-09T10:30:00
[MONGO_REPO] Attempting to save order #001
[MONGO_REPO] ReplaceOneAsync result: ModifiedCount=0, UpsertedId=...
[MONGO_REPO] ✓ Order #001 saved successfully
[MONGODB] ✓ Successfully saved order #001 to MongoDB
[API] Order #001 saved successfully
```

**Error path:**
```
[MONGO_REPO] ✗ ERROR saving order #001: [error message]
[MONGO_REPO] Exception Type: [exception type]
[MONGO_REPO] Stack Trace: [full stack trace]
```

### Check 4: MongoDB Atlas Console
Check if documents are in the database:
1. Go to MongoDB Atlas dashboard
2. Navigate to: Databases → LogiSynDb → Collections → Orders
3. You should see order documents here
4. If empty, the save operation failed (check Visual Studio logs)

### Check 5: OrderScaled Serialization
MongoDB may have trouble serializing complex objects like:
- `ProductionItems` (List<ProductionItem>)
- `RawMaterials` (Dictionary<string, RawMaterialValue>)

Currently these are set to be ignored in the SQL DbContext. MongoDB will store them as-is.

## AdminOrdersView Auto-Refresh

The view automatically refreshes every 10 seconds to show new orders:
- Timer: `_refreshTimer.Interval = TimeSpan.FromSeconds(10);`
- Fetches from API: `await _apiClient.GetOrdersAsync()`
- Converts data: `OrderRow.FromOrderScaled(o)`
- Falls back to sample data if API is unavailable

## API Endpoints

### Get All Orders
```
GET /api/orders
Returns: List<OrderScaled>
```

### Get Specific Order
```
GET /api/orders/{id}
Returns: OrderScaled or 404
```

### Save/Create Order
```
POST /api/orders
Body: { OrderId, Customer, Status, OrderDate, ProductionItems, RawMaterials }
Returns: OrderScaled (201 Created)
```

### Parse PDF Order
```
POST /api/orders/parse
Body: multipart/form-data with PDF file
Returns: OrderScaled with parsed details
```

### Debug/Ping
```
GET /api/orders/debug/ping
Returns: { timestamp, message, orderCount }
```

## Key Files Modified

- `LogiSyn/Services/ApiClient.cs` - NEW (HTTP communication)
- `LogiSyn/Views/AdminOrdersView.xaml.cs` - MODIFIED (fetch from API)
- `SharedLibrary/Model/UiModels.cs` - MODIFIED (added factory method)
- `AndersonsBakeryAPI/Services/OrderService.cs` - MODIFIED (added logging)
- `AndersonsBakeryAPI/Repositories/MongoOrderRepository.cs` - MODIFIED (added logging)
- `AndersonsBakeryAPI/Controllers/OrderController.cs` - MODIFIED (added logging & debug endpoint)

## Next Steps if Still Not Working

1. **Enable MongoDB diagnostic logging:**
   - Set driver logging level in code or connection string

2. **Use MongoDB Compass locally:**
   ```
   1. Download MongoDB Compass
   2. Connect with: mongodb+srv://reannaude1_db_user:...@cluster0.twltvce.mongodb.net
   3. Check if databases and collections exist
   4. Manually insert a test document
   ```

3. **Check firewall:**
   - Ensure outbound HTTPS (port 443) is not blocked for MongoDB Atlas

4. **Restart services:**
   - Stop API server
   - Stop desktop app
   - Restart both and test again

5. **Check MongoDB user permissions:**
   - User needs readWrite on LogiSynDb database
   - Verify in MongoDB Atlas under Database Access
