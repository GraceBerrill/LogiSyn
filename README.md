# LogiSyn

A desktop order-management system built for **Anderson's Bakery**, written in C# / WPF (.NET 10). LogiSyn replaces manual, error-prone production workbooks with a single application that turns an incoming customer order (as a PDF) into a scaled production sheet — pans, trolleys, and raw-material quantities worked out automatically from each product's recipe — tracks that order through to completion, and manages the bakery's users and product catalog behind role-based access control.

## Background

The original brief behind this project was workbook automation for Anderson's Bakery:

- **Problem:** production sheets were being consolidated manually across separate workbooks (Shoprite + Frozen orders, Spar + small customers), which was slow and error-prone, and led to double-working.
- **Goal:** one system that incorporates each product's recipe to automatically work out raw material usage, pans and trolleys needed per order, replacing the manual workbook process.
- **Product categories:** Chilled, Ambient, Frozen.

LogiSyn is the resulting application: upload a customer's order as a PDF, and the system reads it, matches it against the product catalog, and produces a scaled production sheet — no manual transcription required.

---

## Table of Contents

- [Key Features](#key-features)
- [Tech Stack](#tech-stack)
- [System Architecture](#system-architecture)
- [Getting Started](#getting-started)
- [Default Login Credentials](#default-login-credentials)
- [Roles & Permissions](#roles--permissions)
- [Algorithms & Core Logic](#algorithms--core-logic)
- [Project Structure](#project-structure)
- [Known Limitations](#known-limitations)
- [Security Notes](#security-notes)

---

## Key Features

- **Role-based login** — Admin, Manager, and User accounts, each with a different sidebar and a different set of permitted actions.
- **PDF order ingestion** — drop or browse to a customer order PDF, and LogiSyn parses the order number, customer, date and line items automatically.
- **Automatic production scaling** — every matched line item is converted into pans needed, trolleys needed, and total raw materials required, based on the product's recipe.
- **Order tracking** — a live Orders view (auto-refreshing), a History view of completed orders, and per-order breakdown/production sheets.
- **Product & recipe management** — add, edit and delete products, each with its own ingredient list, method, and storage location.
- **User management** — add, edit, delete and search users, with an offline-safe sync mechanism to MongoDB.
- **Dashboard reporting** — order summary cards, and real Excel export / Outlook email of individual orders.
- **Offline-first data layer** — the app keeps working even if MongoDB Atlas is unreachable, transparently falling back to a local SQL Server database.

## Tech Stack

| Layer | Technology |
| --- | --- |
| UI | WPF (.NET 10, `net10.0-windows`), XAML |
| Backend services | C# class libraries, in-process (no network hop required) |
| Optional web API | ASP.NET Core 10 Web API (`AndersonsBakeryAPI`), OpenAPI/Swagger |
| Local database | SQL Server LocalDB, via `Microsoft.Data.SqlClient` 7.1.0 |
| Cloud database | MongoDB Atlas, via `MongoDB.Driver` 3.12.0 |
| ORM (API project) | Entity Framework Core 10.0.0 |
| PDF parsing | `UglyToad.PdfPig` |
| Excel export | `ClosedXML` |
| Email | Outlook COM automation (late-bound), with a `mailto:` fallback |
| Password hashing | PBKDF2-HMAC-SHA256 (custom implementation, no external library) |

## System Architecture

LogiSyn is a WPF desktop app (`LogiSyn`) that references its backend logic (`AndersonsBakeryAPI`) and shared models (`SharedLibrary`) **directly as .NET project references** — most screens call the C# service classes in-process. There is also a real, separately-runnable ASP.NET Core Web API (same `AndersonsBakeryAPI` project, run as a web host) that currently exposes **one** controller, for Orders — it's used opportunistically by a few views (with a 2-second timeout and an automatic fallback to the in-process services if it isn't running), and exists as scaffolding for a future hosted/multi-client deployment rather than something the desktop app depends on today.

Each kind of data is persisted differently:

| Entity | Where it lives | Sync strategy |
| --- | --- | --- |
| **Users** | SQL LocalDB **and** MongoDB Atlas | Login tries Mongo first, falls back to SQL. A manual **Sync** button on Manage Users pushes any SQL-only ("orphaned") user up to Mongo. |
| **Orders** | Local `orders.json` **+** SQL **+** MongoDB | Saved instantly to memory/disk, then written to SQL and Mongo in the background — no manual sync needed. |
| **Products** | Local `products.json` only | No SQL, no Mongo — the simplest of the three. |

```
┌─────────────────────────┐
│   LogiSyn (WPF, UI)      │
│  Views + code-behind     │
└─────────────┬────────────┘
              │ in-process calls (ProjectReference)
┌─────────────▼────────────┐        ┌──────────────────────────┐
│  AndersonsBakeryAPI       │  HTTP  │  AndersonsBakeryAPI       │
│  Service classes          │◄──────►│  (run standalone as a     │
│  (Login/User/Order/       │ 2s to  │   web host) — OrderController│
│   Product/Sync Services)  │  out   │   only, Swagger enabled   │
└───────┬─────────┬─────────┘        └──────────────────────────┘
        │         │
        ▼         ▼
 SQL LocalDB   MongoDB Atlas
 (LogiSynDb)   (per appsettings.json)
```

---

## Getting Started

### Prerequisites

- **.NET 10 SDK** (the project targets `net10.0-windows`)
- **Windows**, with WPF support (this is a Windows desktop application)
- **SQL Server Express LocalDB** (ships with Visual Studio; otherwise install the [SQL Server Express LocalDB](https://learn.microsoft.com/sql/database-engine/configure-windows/sql-server-express-localdb) package separately)
- Visual Studio 2022+ (or VS Code with the C# Dev Kit) is recommended for opening `LogiSyn.slnx`
- A MongoDB Atlas cluster is **optional** — the app runs fully offline against LocalDB if one isn't configured or reachable (see [Security Notes](#security-notes))

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

LocalDB creates the `LogiSynDb` database automatically on first connection, but it does **not** come with the `[User]` table or any seed accounts — those have to be created once per machine. Run the following with `sqlcmd` (or paste it into SSMS / Visual Studio's SQL Server Object Explorer):

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

> Passwords are stored in plain text only until first login — the very first successful login for an account automatically upgrades it to a salted PBKDF2 hash (see [Algorithms & Core Logic](#algorithms--core-logic)).

By default the app connects to `(localdb)\MSSQLLocalDB`, database `LogiSynDb`, with Windows integrated security — no further configuration needed. To point at a different SQL instance instead, set the `LOGISYN_CONNECTION` environment variable to a full connection string.

### 3. (Optional) MongoDB configuration

If you want the Users/Orders cloud sync to work, add your own MongoDB Atlas connection string via **.NET user secrets** (do not commit it to `appsettings.json` — see [Security Notes](#security-notes)):

```bash
cd AndersonsBakeryAPI
dotnet user-secrets set "ConnectionStrings:MongoConnection" "mongodb+srv://<user>:<password>@<cluster>.mongodb.net/?appName=Cluster0"
```

Without this, Mongo calls fail fast and every screen transparently falls back to the local SQL database — the app is fully usable offline.

### 4. Run

```bash
dotnet run --project LogiSyn/LogiSyn.csproj
```

Or press F5 in Visual Studio with `LogiSyn` set as the startup project.

---

## Default Login Credentials

Using the seed script above, three test accounts are available, one per role:

| Role | Username | Password |
| --- | --- | --- |
| Admin | `admin` | `admin123` |
| Manager | `manager` | `manager123` |
| User | `user` | `user123` |

These are local development seed accounts only — change or remove them before using LogiSyn with real data.

## Roles & Permissions

The sidebar (`ShellWindow`) is built dynamically based on the logged-in user's role:

| Screen | Admin | Manager | User |
| --- | --- | --- | --- |
| Dashboard | ✅ | ✅ | ✅ |
| Orders | ✅ (full table + filters) | ✅ (full table + filters) | ✅ (simplified, own view) |
| History | ✅ | ✅ | ❌ |
| Products | ✅ | ✅ | ❌ |
| Users | ✅ | ❌ | ❌ |
| Dashboard Excel/Email export | ✅ | ❌ | ❌ |

Order breakdown screens also differ by role: Admin/Manager open a production-sheet breakdown (Manager gets a fully editable sheet; other roles reaching it see a read-only summary), while a User fills in and submits their own editable order sheet, which is what moves an order's status to **Completed**.

---

## Algorithms & Core Logic

This section documents the non-obvious logic behind the UI — the parts a screenshot doesn't explain.

### Authentication & password security

- `LoginServiceRouter.Authenticate` tries MongoDB first (`MongoLoginService`); any exception (network, auth, timeout) is caught and silently falls back to `LoginService`, which queries SQL LocalDB instead. The user never sees which path succeeded.
- Passwords are hashed with **PBKDF2-HMAC-SHA256**, a random 16-byte salt per account, and **600,000 iterations** (`PasswordHasher.cs`) — deliberately expensive, to make a stolen hash table costly to brute-force.
- Backward compatibility: if a stored password isn't already in the app's hash format, it's compared as plain text, and on a successful login it is immediately re-saved as a proper hash — an automatic, lazy migration path rather than a one-off script.

### Dual-database fallback pattern

Both `LoginServiceRouter` and `UserServiceRouter` follow the same shape for every operation (read, add, update, delete): try the MongoDB-backed service, catch any exception, and either fall back to the SQL-backed service (reads) or attempt both and report what succeeded (writes). This means the app degrades gracefully instead of failing outright whenever Mongo is unreachable — at the cost of some operations silently taking the slower, retry-then-fallback path on every call.

### User sync (orphan reconciliation)

Because a new user can be created while Mongo is unreachable, that user is saved to SQL with an empty `MongoId` — an "orphan." `SyncService.SyncUsers()` scans SQL for orphan rows, pushes each one to Mongo, and writes the resulting Mongo `_id` back onto the SQL row. Until a user has been synced, Manage Users deliberately blocks editing or deleting them, to avoid editing a copy that's about to be overwritten.

### PDF order parsing

`OrderService.ReadAndScaleOrder` turns an uploaded PDF into a scaled order, entirely locally (no cloud/AI call):

1. Extracts raw text per page using `PdfPig`.
2. Regular expressions pull out the order number (`Order\s*(?:Number|#)?...`), customer name, and order date (supports both `12 Jan 2024` and `12/01/2024` style dates).
3. For each line of extracted text, it checks whether the line contains a known product name (substring match against the product catalog), then scans that line plus the next two lines for a quantity — looking for patterns like `pkts` or `Qty: 40`, falling back to the last number found if nothing more specific matches.

> Only PDF files are currently parsed — the file picker also lists `.xlsx`/`.csv` as accepted types, but non-PDF uploads won't extract anything meaningful yet.

### Production scaling (pans, trolleys, ingredients)

For every matched line item, once a quantity is known:

```
Pans     = ceil(quantity / 50)
Trolleys = ceil(quantity / 100)
Ingredients = recipe quantity × order quantity, plus a 10% buffer:
            ingredientTotal = baseQty × quantity × 1.1
```

If a product has no defined recipe, a hardcoded flour/yeast fallback ratio is used instead so the order can still be processed. Ingredient totals across every line item in the order are then aggregated into the order's overall raw-material requirement (`RecalRawMaterials`).

### Unique order ID generation

`EnsureUniqueOrderId` guarantees no two orders share an ID:

- If the parsed order ID is purely numeric (optionally prefixed with `#`), it increments the number — preserving its original zero-padded width — until it finds one not already in use.
- Otherwise, it appends `-1`, `-2`, `-3`, … to the ID until it's unique.

### Order persistence (write-local-first)

`OrderService.SaveOrder` writes synchronously to an in-memory list and a local `Data/orders.json` file first, so the UI never blocks on a network call — then fires off background writes to SQL and MongoDB. (Currently, if either background write fails, it's only logged to the console, not surfaced to the user.) `GetOrders` reads from memory first, then tries Mongo with a short timeout, then SQL, then the local JSON file as a last resort.

### Order status model

Deliberately simple: an order is either **Pending** or **Completed** — there is no "in progress" state. A User submitting their filled-in order sheet is what transitions it to Completed.

### Live auto-refresh

The Orders screen (`AdminOrdersView`) polls for updates automatically every 10 seconds via a `DispatcherTimer`, so Admin/Manager users always see current order status without manually refreshing.

---

## Project Structure

```
LogiSyn/
├── LogiSyn/                   # WPF desktop application (UI)
│   ├── Views/                 # Screens & modals (Login, Dashboard, Orders, Products, Users, …)
│   ├── Styles/                # Shared XAML styles, theme colours, and the vector icon set
│   └── Assets/                # Icons, images
├── AndersonsBakeryAPI/        # Backend logic — service classes + the optional ASP.NET Core Web API
│   ├── Services/               # Login/User/Order/Product/Sync services, dual-DB routers, PDF parsing,
│   │                           #   password hashing, Excel export, email
│   ├── Controllers/            # OrderController (the API's only controller today)
│   └── Repositories/           # SQL/Mongo order repositories
├── SharedLibrary/              # Shared models (UserRow, OrderScaled, ProductRow, …) used by both projects
└── LogiSyn.slnx                # Solution file
```

---

## Known Limitations

- **Dashboard summary cards show fixed demo data**, not real order counts — the live equivalent is the Orders and History screens.
- **Only PDF uploads actually parse** today, despite the file picker also listing Excel/CSV as accepted types.
- **Products are not synced** to SQL or MongoDB — they live only in a local `products.json` file, unlike Users and Orders.
- **The Web API only covers Orders** — Users and Products have no HTTP endpoints, so it can't yet serve a non-WPF client on its own.
- **Order status is binary** (Pending/Completed only) — there's no "in progress" or partially-fulfilled state.
- The repository contains a second, unused set of views (`AdminWindow`, `ManagerWindow`, `UserWindow`, and related pages) left over from an earlier navigation design — the live app only ever opens `ShellWindow`.
- The CI/CD workflow (`.github/workflows/ci-cd.yml`) currently installs .NET 8/9 SDKs, while the project targets .NET 10 — the pipeline needs updating to match.

## Security Notes

- `AndersonsBakeryAPI/appsettings.json` ships with a MongoDB Atlas connection string checked into source control. **Before making this repository public (or if it already is), rotate that credential and move it to [.NET user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) or an environment variable** — see the [MongoDB configuration](#3-optional-mongodb-configuration) step above. The project is already set up to read from user secrets (`UserSecretsId` is configured in `AndersonsBakeryAPI.csproj`); only the hardcoded fallback in `appsettings.json` needs to go.
- Local SQL LocalDB access uses Windows Integrated Security by default — no credentials are stored for it.
- Seed account passwords above are intentionally weak/demo-only; they auto-upgrade to salted PBKDF2 hashes on first login, but should still be changed before any real data is entered.
