<div align="center">

# Anderson's Bakery Management System
### Created By 
# LogiSyn

[![Grace Berrill](https://img.shields.io/badge/Grace_Berrill-ST10440118-6E56CF?style=flat&logo=github&logoColor=white)](https://github.com/GraceBerrill)
[![Adriaan Kock](https://img.shields.io/badge/Adriaan_Kock-ST10263443-6E56CF?style=flat&logo=github&logoColor=white)](https://github.com/AdriaanKock)
[![Matthew Rosselli](https://img.shields.io/badge/Matthew_Rosselli-ST10258193-6E56CF?style=flat&logo=github&logoColor=white)](https://github.com/CharlsWint)
[![Luc Naude](https://img.shields.io/badge/Luc_Naude-ST10443241-6E56CF?style=flat&logo=github&logoColor=white)](https://github.com/LucNaude)

A desktop management system built for Anderson's Bakery. Written in C# / WPF (.NET 10). LogiSyn replaces manual, error prone production workbooks with a single application that turns an incoming customer order (as a PDF) into a scaled production sheet. Pans, trolleys and raw material quantities worked out automatically from each product's recipe, tracks that order through to completion and manages the bakery's users and product catalog behind role-based access control.

![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?style=flat&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-WPF-239120?style=flat&logo=csharp&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-LocalDB-CC2927?style=flat&logo=microsoftsqlserver&logoColor=white)
![MongoDB](https://img.shields.io/badge/MongoDB-Atlas-47A248?style=flat&logo=mongodb&logoColor=white)
![Windows](https://img.shields.io/badge/Platform-Windows-0078D6?style=flat&logo=windows&logoColor=white)
 
<img src="Images/AndersonBakery.png" alt="Anderson's Silwood Bakery logo" width="200">
<img src="Images/Logisyn.png" alt="LogiSyn logo" width="130">

</div>

#
## Background
Anderson's Bakery is a local Cape Town business that supplies baked goods to major supermarkets such as Checkers and Spar. While most factories rely heavily on machinery and automation, Anderson's deliberately doesn't. Their goal is to create as many jobs as possible to help fight South Africa's high unemployment rate. Because of this, many employees have little or no formal education, which makes complex, high-end systems difficult to roll out.

That's where LogiSyn comes in. We built a system that keeps track of all incoming and ongoing orders, while staying simple enough for everyone in the factory to use.

- **Problem:** Production sheets were consolidated manually across separate workbooks, which was slow, error-prone, and led to double-working and theft.
- **Goal:** One system that uses each product's recipe to automatically work out raw material usage, pans and trolleys per order, replacing the manual workbook process.
- **Product categories:** Chilled, Ambient, Frozen.

#
## Table of Contents

- [YouTube Video Link](#youtube-video-link)
- [Running the System](#running-the-system)
- [Default Login Credentials](#default-login-credentials)
- [Roles & Permissions](#roles--permissions)
- [Key Features](#key-features)
- [Tech Stack](#tech-stack)
- [System Architecture](#system-architecture)
- [Design Decisions](#design-decisions)
- [Algorithms & Core Logic](#algorithms--core-logic)
- [Project Structure](#project-structure)
- [Branching & CI/CD](#branching--cicd)
- [AI Declaration](#ai-declaration)

#
## YouTube Video Link

**See LogiSyn in action:** A full walkthrough of the system, from login to completed order:

<a href="https://youtu.be/CByFgbEQakI">
  <img src="https://img.shields.io/badge/Click_here_to_watch_on_YouTube-FF0000?style=for-the-badge&logo=youtube&logoColor=white" alt="Click here to watch on YouTube">
</a>

#
## Running The System

### Prerequisites

- **.NET 10 SDK** (the project targets `net10.0-windows`).
- **Windows** With WPF support (this is a Windows desktop application).
- **SQL Server Express LocalDB** (ships with Visual Studio, or install [SQL Server Express LocalDB](https://learn.microsoft.com/sql/database-engine/configure-windows/sql-server-express-localdb) separately).
- **Visual Studio 2022+** (or VS Code with the C# Dev Kit) Is recommended for opening `LogiSyn.slnx`.
- **MongoDB Atlas** The app runs fully offline on LocalDB if no cluster is configured or reachable.

### 1. Clone and open

```bash
git clone https://github.com/GraceBerrill/LogiSyn.git
cd LogiSyn
```

Open `LogiSyn.slnx` in Visual Studio, or build from the command line:

```bash
dotnet restore
dotnet build LogiSyn/LogiSyn.csproj -c Debug
```

### 2. Database setup

LocalDB creates the `LogiSynDb` database automatically on first connection, but it does **not** include the `[User]` table or any seed accounts. Create those once per machine by running the following with `sqlcmd`, SSMS, or Visual Studio's SQL Server Object Explorer:
 
```sql
CREATE TABLE [User] (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    MongoId NVARCHAR(50) NULL,
    Username NVARCHAR(100) NOT NULL UNIQUE,
    Password NVARCHAR(256) NOT NULL,
    Role NVARCHAR(50) NOT NULL,
    DateAdded DATETIME NOT NULL DEFAULT GETDATE()
);

INSERT INTO [User] (Username, Password, Role) VALUES ('admin', 'admin123', 'Admin');
INSERT INTO [User] (Username, Password, Role) VALUES ('manager', 'manager123', 'Manager');
INSERT INTO [User] (Username, Password, Role) VALUES ('user', 'user123', 'User');
```

> Passwords are only stored in plain text until first login. The first successful login automatically upgrades each one to a salted PBKDF2 hash (see [Algorithms & Core Logic](#algorithms--core-logic)).
 
By default the app connects to `(localdb)\MSSQLLocalDB`, database `LogiSynDb`, using Windows integrated security, so no further configuration is needed. To use a different SQL instance, set the `LOGISYN_CONNECTION` environment variable to a full connection string.
 
### 3. MongoDB configuration 
 
To enable cloud sync for Users and Orders.
 
```bash
cd AndersonsBakeryAPI
dotnet user-secrets set "ConnectionStrings:MongoConnection" "mongodb+srv://reannaude1_db_user:MPaJYcEqumlJbf0j@cluster0.twltvce.mongodb.net/?appName=Cluster0"
```

Without this, Mongo calls fail fast and every screen falls back to the local SQL database, so the app is still fully usable offline.

### 4. Run

```bash
dotnet run --project LogiSyn/LogiSyn.csproj
```
 
Or press **F5** in Visual Studio with `LogiSyn` set as the startup project.
 
---

#
## Default Login Credentials

The seed script above creates one test account per role:

| Role | Username | Password |
| --- | --- | --- |
| Admin | `admin` | `admin123` |
| Manager | `manager` | `manager123` |
| User | `user` | `user123` |

These are local development seed accounts only — change or remove them before using LogiSyn with real data.

#
## Roles & Permissions

The sidebar (`ShellWindow`) is built dynamically from the logged-in user's role. Each role only sees the screens it needs, which keeps the app simple for factory-floor staff and stops anyone accidentally changing products, users or other people's orders.

| Screen | Admin | Manager | User |
| --- | --- | --- | --- |
| Dashboard | ✅ | ✅ | ✅ |
| Orders | ✅ (full table + filters) | ✅ (full table + filters) | ✅ (simplified, own view) |
| History | ✅ | ✅ | ❌ |
| Products | ✅ | ✅ | ❌ |
| Users | ✅ | ❌ | ❌ |
| Dashboard Excel/Email export | ✅ | ❌ | ❌ |

Order breakdown screens also differ by role: Admin/Manager open a production-sheet breakdown (Manager gets a fully editable sheet; other roles reaching it see a read-only summary), while a User fills in and submits their own editable order sheet, which is what moves an order's status to **Completed**.

#
## Key Features

- **Role-based login:** Admin, Manager and User accounts, each with its own sidebar and permitted actions.
- **PDF order ingestion:** Drop in or browse to a customer order PDF and LogiSyn reads the order number, customer, date and line items automatically.
- **Automatic production scaling:** Every line item is converted into pans, trolleys and total raw materials based on the product's recipe.
- **Order tracking:** A live, auto-refreshing Orders view, a History view of completed orders and per-order production sheets.
- **Product & recipe management:** Add, edit and delete products, each with its own ingredients, method and storage location.
- **User management:** Add, edit, delete and search users, with offline-safe sync to MongoDB.
- **Dashboard reporting:** Order summary cards, plus Excel export and Outlook email for individual orders.
- **Offline-first data layer:** The app keeps working when MongoDB Atlas is unreachable by falling back to a local SQL Server database.

#
## Tech Stack

| Layer | Technology | Why we chose it |
| --- | --- | --- |
| UI | WPF (.NET 10), XAML | The bakery runs on Windows PCs. A native desktop app works offline, needs no browser or hosting, and fit the team's C# experience. |
| Backend services | C# class libraries, called in-process | No separate server to install or keep running on the factory PC. The app is a single install. |
| Optional web API | ASP.NET Core 10 Web API, OpenAPI/Swagger | Groundwork for a future multi-device setup. Swagger makes endpoints easy to test. |
| Local database | SQL Server LocalDB (`Microsoft.Data.SqlClient` 7.1.0) | Free, ships with Visual Studio, needs no server setup, and keeps working without internet. |
| Cloud database | MongoDB Atlas (`MongoDB.Driver` 3.12.0) | Off-site copy of users and orders, so data isn't lost if the factory PC fails. Free tier covers the bakery's size. |
| ORM (API project) | Entity Framework Core 10.0.0 | Standard .NET data access for the API, with less hand-written SQL. |
| PDF parsing | `UglyToad.PdfPig` | Supermarket orders arrive as PDFs. PdfPig is free, open source and pure .NET, so no Adobe install or cloud service is needed. |
| Excel export | `ClosedXML` | The bakery already worked in Excel, so exports stay in a familiar format. ClosedXML doesn't need Excel installed. |
| Email | Outlook COM automation, `mailto:` fallback | Uses the email client staff already have, and still works on PCs without Outlook. |
| Password hashing | PBKDF2-HMAC-SHA256 | Industry-standard password hashing with no external library needed. |
 
#
## System Architecture

LogiSyn is a WPF desktop app (`LogiSyn`) that references its backend logic (`AndersonsBakeryAPI`) and shared models (`SharedLibrary`) **directly as .NET project references**, so most screens call the C# service classes in-process. We chose this so the app runs as a single install with no server to manage, which suits a factory without dedicated IT staff.

The same `AndersonsBakeryAPI` project can also run on its own as an ASP.NET Core Web API. It currently exposes **one** controller (Orders), which a few views use opportunistically with a 2-second timeout and an automatic fallback to the in-process services. It's scaffolding for a future hosted, multi-device setup rather than something the desktop app depends on today.
 
Each kind of data is stored differently, depending on how important and how often-changed it is:

| Entity | Where it lives | Sync strategy | Why |
| --- | --- | --- | --- |
| **Users** | SQL LocalDB **+** MongoDB Atlas | Login tries Mongo first, falls back to SQL. A **Sync** button on Manage Users pushes SQL-only ("orphaned") users up to Mongo. | Staff must always be able to log in, even with no internet. |
| **Orders** | `orders.json` **+** SQL **+** MongoDB | Saved instantly to memory/disk, then written to SQL and Mongo in the background. | Orders are the most important data, so they're kept in three places and saving never freezes the screen. |
| **Products** | `products.json` only | No SQL or Mongo. | The catalogue rarely changes, so the simplest option was enough for now. |

```
┌────────────────────────────┐
│     LogiSyn (WPF, UI)      │
│    Views + code-behind     │
└─────────────┬──────────────┘
              │ in-process calls (ProjectReference)
┌─────────────▼──────────────┐         ┌────────────────────────────┐
│     AndersonsBakeryAPI     │  HTTP   │     AndersonsBakeryAPI     │
│      Service classes       │◄───────►│   (standalone web host)    │
│    Login / User / Order /  │  2s     │   OrderController only,    │
│   Product / Sync Services  │ timeout │      Swagger enabled       │
└───────┬────────────┬───────┘         └────────────────────────────┘
        │            │
        ▼            ▼
   SQL LocalDB   MongoDB Atlas
   (LogiSynDb)   (user secrets)
```

### Hosted API (Render)
 
The Web API is containerised with Docker and deployed on Render at `https://andersons-bakery-api.onrender.com`. Docker keeps the .NET 10 runtime identical on our machines and on the server.
 
Render's free tier puts the container to sleep after 15 minutes of inactivity, and waking it takes 30–50 seconds. `ApiClient` handles this by trying each option in turn:
 
1. The Render endpoint
2. A local development host (`https://localhost:7274` / `http://localhost:5109`), used during development only
3. The in-process services, backed by LocalDB
The UI shows a status badge while it waits instead of freezing.

#
## Design Decisions

### Availability over consistency
On a factory floor, being able to keep working matters more than every copy of the data matching at every moment. LogiSyn never waits on the cloud: changes are saved locally first and reach MongoDB when it's reachable. The trade-off is that SQL and Mongo can briefly disagree. For example, a user created offline stays "orphaned" until someone presses Sync. In CAP theorem terms, LogiSyn favours availability and partition tolerance over strict consistency.
 
### Why MongoDB for the cloud copy
Orders are nested by nature: an order has line items, and each line item has its own scaled ingredients, pans and trolleys. A document database stores each order as one document that matches `OrderScaled` directly, instead of splitting it across several tables and joining it back together.
 
### Why SQL LocalDB for the local copy
LocalDB gives us proper transactions and constraints (like unique usernames) without installing or running a database server. It's free, ships with Visual Studio and works with no internet at all.
 
### Why desktop instead of a web app
A web app would need a server to be reachable before anyone could log in, and customer PDFs would have to be uploaded before they could be read. As a desktop app, LogiSyn parses PDFs, builds Excel files and opens Outlook directly on the bakery's PC, with or without a connection.

#
## Algorithms & Core Logic

This section explains the logic behind the UI, the parts a screenshot doesn't show.

### Authentication & password security

- `LoginServiceRouter.Authenticate` tries MongoDB first (`MongoLoginService`). Any exception (network, auth, timeout) is caught and it silently falls back to `LoginService`, which queries SQL LocalDB. The user never sees which path succeeded.
- Passwords are hashed with **PBKDF2-HMAC-SHA256**, a random 16-byte salt per account and **600,000 iterations** (`PasswordHasher.cs`). The high iteration count is deliberate: it makes a stolen password table very slow and costly to crack, and matches current OWASP guidance.
- **Lazy migration:** if a stored password isn't hashed yet, it's compared as plain text, then immediately re-saved as a proper hash on successful login. This let us upgrade existing accounts without a separate migration script.
- Salts come from `RandomNumberGenerator`, .NET's cryptographically secure random generator.
- Hashes are compared with `CryptographicOperations.FixedTimeEquals`, which takes the same time no matter where the mismatch is, so an attacker can't learn anything from how long a login takes to fail.
- The password field on `UserRow` is marked `[JsonIgnore]`, so hashes are never included in API responses.
- All API calls use HTTPS with certificate validation switched on, so unverified certificates are rejected.

### Dual-database fallback pattern

`LoginServiceRouter` and `UserServiceRouter` follow the same shape for every operation (read, add, update, delete): try the Mongo-backed service, catch any exception, then either fall back to SQL (reads) or attempt both and report what succeeded (writes).
 
**Why:** internet at the factory can drop out, including during load-shedding. This pattern means the app slows down slightly instead of failing outright when Mongo is unreachable.

### User sync (orphan reconciliation)

A user created while Mongo is unreachable is saved to SQL with an empty `MongoId`, making it an "orphan." `SyncService.SyncUsers()` finds orphan rows, pushes each one to Mongo, and writes the new Mongo `_id` back onto the SQL row.
 
Until a user has been synced, Manage Users blocks editing or deleting them. This avoids editing a copy that's about to be overwritten.
 
### PDF order parsing

`OrderService.ReadAndScaleOrder` turns an uploaded PDF into a scaled order:
 
1. Extracts raw text per page using `PdfPig`.
2. Uses regular expressions to pull out the order number, customer name and order date (supports both `12 Jan 2024` and `12/01/2024`).
3. For each line, checks for a known product name (substring match against the catalogue), then scans that line and the next two for a quantity, looking for patterns like `pkts` or `Qty: 40` and falling back to the last number found.
**Why parse locally instead of using an AI or cloud service:** customer order data stays on the bakery's own machine, it works offline, and there's no ongoing cost.
 
> Only PDF files are currently parsed. The file picker also lists `.xlsx` / `.csv`, but those won't extract anything yet.
 
### Production scaling (pans, trolleys, ingredients)

For every matched line item, once a quantity is known:
 
```
Pans            = ceil(quantity / 50)
Trolleys        = ceil(quantity / 100)
ingredientTotal = baseQty × quantity × 1.1   // recipe quantity + 10% buffer
```
 
Pans and trolleys are always rounded **up**, since you can't use half a pan. The 10% ingredient buffer allows for waste and spillage so production doesn't run short mid-order.
 
If a product has no recipe, a hardcoded flour/yeast ratio is used so the order can still be processed. Ingredient totals across all line items are then added up into the order's overall raw-material requirement (`RecalRawMaterials`).
 
### Unique order ID generation

`EnsureUniqueOrderId` guarantees no two orders share an ID, so the same PDF uploaded twice can't overwrite an existing order:
 
- **Numeric IDs** (optionally prefixed with `#`) are incremented, keeping their original zero-padded width, until an unused one is found.
- **Other IDs** get `-1`, `-2`, `-3`, … appended until unique.

### Order persistence (write-local-first)
 
`OrderService.SaveOrder` writes to an in-memory list and a local `Data/orders.json` file first, so the screen never freezes waiting on the network. It then writes to SQL and MongoDB in the background. Background failures are currently only logged to the console, not shown to the user.
 
`GetOrders` reads in this order: **memory → Mongo (short timeout) → SQL → local JSON**, so there's always a copy to fall back on.
 
### Order status model
 
An order is either **Pending** or **Completed**, with no "in progress" state. We kept it this simple on purpose so floor staff only ever have one action to take: fill in and submit the sheet.
 
### Live auto-refresh
 
The Orders screen (`AdminOrdersView`) polls for updates every 10 seconds using a `DispatcherTimer`. Admins and Managers always see current order status without having to remember to refresh.

#
## Project Structure

```
LogiSyn/
├── LogiSyn/                  # WPF desktop application (UI)
│   ├── Views/                # Screens & modals (Login, Dashboard, Orders, Products, Users, …)
│   ├── Styles/               # Shared XAML styles, theme colours, vector icon set
│   └── Assets/               # Icons, images
├── AndersonsBakeryAPI/       # Backend logic + ASP.NET Core Web API
│   ├── Services/             # Login/User/Order/Product/Sync services, dual-DB routers,
│   │                         #   PDF parsing, password hashing, Excel export, email, ApiClient
│   ├── Controllers/          # OrderController (the API's only controller today)
│   └── Repositories/         # SQL / Mongo order repositories
├── SharedLibrary/            # Shared models (UserRow, OrderScaled, ProductRow, …)
├── Images/                   # README images
├── .github/workflows/        # CI/CD pipeline (ci-cd.yml)
└── LogiSyn.slnx              # Solution file
```

`SharedLibrary` exists so the UI and backend use the exact same model classes, which avoids the two drifting out of sync.

#
## Branching & CI/CD
 
| Branch | Purpose |
| --- | --- |
| `main` | Stable, release-ready code |
| `Dev` | Integration branch for finished features |
| `Fix` | Bug fixes, refactoring and security fixes |
 
A GitHub Actions workflow (`.github/workflows/ci-cd.yml`) runs on every push and pull request. It restores NuGet packages, builds `LogiSyn.slnx` in Release mode, runs all 19 unit tests, and uploads the zipped build as a downloadable artifact. A pull request is only merged once the build and tests pass.

#
## AI Declaration
Our full declaration of how AI tools were used in this project is available below.

<a href="Documents/AI-Declaration.pdf">
  <img src="https://img.shields.io/badge/View_AI_Declaration-6E56CF?style=for-the-badge&logo=adobeacrobatreader&logoColor=white" alt="View AI Declaration (PDF)">
</a>
