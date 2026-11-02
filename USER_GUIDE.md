# Anderson's Bakery Management System (LogiSyn) — User Guide

<div align="center">
  <img src="Images/AndersonBakery.png" alt="Anderson's Silwood Bakery" width="180">
  <img src="Images/Logisyn.png" alt="LogiSyn" width="120">
  <br>
  <strong>Empowering Factory Floor Operations with Simple, Automated, and Resilient Order Management</strong>
</div>

---

## Table of Contents
1. [Introduction & Overview](#1-introduction--overview)
2. [User Roles & Permissions](#2-user-roles--permissions)
3. [System Access & Getting Started](#3-system-access--getting-started)
   - [Launching the Desktop Application](#launching-the-desktop-application)
   - [Default Login Credentials](#default-login-credentials)
   - [Understanding the Connection Status Badge](#understanding-the-connection-status-badge)
4. [Application Navigation & Layout](#4-application-navigation--layout)
5. [Step-by-Step Module Guides](#5-step-by-step-module-guides)
   - [Module 1: Dashboard Overview](#module-1-dashboard-overview)
   - [Module 2: Order Ingestion & Automatic Scaling](#module-2-order-ingestion--automatic-scaling)
   - [Module 3: Production Sheets & Manager Order Review](#module-3-production-sheets--manager-order-review)
   - [Module 4: Factory Floor Production Workflow (Baker / User Role)](#module-4-factory-floor-production-workflow-baker--user-role)
   - [Module 5: Order History & Quality Audit](#module-5-order-history--quality-audit)
   - [Module 6: Product & Recipe Catalog Management](#module-6-product--recipe-catalog-management)
   - [Module 7: User Management & Cloud Sync](#module-7-user-management--cloud-sync)
   - [Module 8: Reporting, Excel Export & Email Dispatch](#module-8-reporting-excel-export--email-dispatch)
6. [Mobile Web Companion Guide](#6-mobile-web-companion-guide)
7. [Offline Resilience & Load-Shedding Survival](#7-offline-resilience--load-shedding-survival)
8. [Troubleshooting & Frequently Asked Questions (FAQ)](#8-troubleshooting--frequently-asked-questions-faq)
9. [Quick Reference Cheat Sheet](#9-quick-reference-cheat-sheet)

---

## 1. Introduction & Overview

**LogiSyn** is a production management system engineered specifically for **Anderson's Bakery** in Cape Town. Anderson's supplies fresh, chilled, ambient, and frozen baked goods to leading retail supermarkets including **Checkers** and **Spar**.

### The Core Problem LogiSyn Solves
Historically, incoming customer purchase orders were transferred by hand into multiple disconnected physical logbooks and spreadsheets. This led to:
- Calculation bottlenecks when converting retail loaf and pastry counts into batch ingredients.
- Human error in pan and trolley allocations, resulting in production delays or wasted oven space.
- Discrepancies between planned raw materials and actual bakery floor usage.
- High vulnerability to internet drops and load-shedding.

### How LogiSyn Works
LogiSyn transforms this workflow into an automated, four-step process:
1. **Ingest Order:** Drag-and-drop an incoming customer order (PDF or Excel).
2. **Auto-Scale:** LogiSyn automatically reads the customer, line items, and quantities, calculating exact pans, trolleys, and recipe ingredient totals (including a 10% safety buffer).
3. **Bake & Track:** Bakers on the floor view the digital production sheet, record actuals, or upload a filled production Excel file.
4. **Complete & Archive:** Submitting the order marks it complete, persists the records across local storage, SQL Server LocalDB, and cloud MongoDB Atlas, and moves it to searchable historical archives.

```
┌────────────────────────┐      ┌─────────────────────────┐      ┌────────────────────────┐
│  Incoming Order File   │ ───► │   Automatic Scaling     │ ───► │ Factory Floor Tracking │
│ (Checkers/Spar PDF/XLS)│      │  Pans, Trolleys, +10%   │      │ (Digital Sheet / Excel)│
└────────────────────────┘      └─────────────────────────┘      └───────────┬────────────┘
                                                                             │
                                                                 ┌───────────▼────────────┐
                                                                 │  Completed & Archived  │
                                                                 │ (LocalDB + Mongo Sync) │
                                                                 └────────────────────────┘
```

---

## 2. User Roles & Permissions

LogiSyn implements strict Role-Based Access Control (RBAC). The interface adapts automatically to the user's role upon login, ensuring that floor staff are not overwhelmed with administrative settings, while administrators retain full governance over products and user access.

| Screen / Feature | Admin | Manager | User (Baker / Floor Staff) |
| :--- | :---: | :---: | :---: |
| **Dashboard** | Full access (KPIs + Export/Email) | View KPIs & Recent Orders | View KPIs & Personal Shift Tasks |
| **Orders View** | Full table, search, filters & create | Full table, search, filters & create | Streamlined active production list |
| **Order Ingestion (PDF / Excel)** | ✅ Yes | ✅ Yes | ❌ No |
| **Production Sheets Breakdown** | Read & Summary Paper | Detailed & Editable Sheet | ❌ Replaced by User Order Sheet |
| **User Production Sheet** | Read-only | Read & Edit | ✅ Interactive (Enter actuals & complete) |
| **Excel Production Upload** | ❌ Managed via Ingestion | ❌ Managed via Ingestion | ✅ Upload completed floor sheet |
| **Order History** | ✅ Full search & date filters | ✅ Full search & date filters | ❌ Hidden |
| **Product & Recipe Catalog** | ✅ Full CRUD (Add/Edit/Delete) | ✅ Full CRUD (Add/Edit/Delete) | ❌ Hidden |
| **User Administration** | ✅ Full CRUD & Cloud Sync | ❌ Hidden | ❌ Hidden |
| **Excel & Email Reporting** | ✅ Full access from Dashboard | ❌ Hidden on Dashboard | ❌ Hidden |

---

## 3. System Access & Getting Started

### Launching the Desktop Application
1. Double-click the **LogiSyn** shortcut on your Windows desktop, or launch `LogiSyn.exe` from your installation folder.
2. The **Login Screen** will appear:
   - Enter your assigned **Username** and **Password**.
   - Click the **Eye Icon** (`eye-off.png`) to reveal or mask your entered password characters.
   - Click **Login** (or press `Enter`).

### Default Login Credentials
For initial deployment and development evaluation, the system includes the following seed accounts:

| Role | Username | Initial Password | Recommended Purpose |
| :--- | :--- | :--- | :--- |
| **Administrator** | `admin` | `admin123` | System setup, user administration, reporting, recipe configuration |
| **Manager** | `manager` | `manager123` | Order scheduling, recipe review, daily production oversight |
| **User (Baker)** | `user` | `user123` | Factory floor baking, pan allocations, order completion |

> [!IMPORTANT]
> **Automatic Password Security Upgrade:** When an account logs in for the first time, LogiSyn automatically upgrades plain-text passwords to secure, salted **PBKDF2-HMAC-SHA256** hashes with 600,000 iterations. Change default passwords after initial deployment.

### Understanding the Connection Status Badge
In the top-right corner of the application header, an intelligent connection pill indicates live connectivity:

| Indicator Badge | Meaning | System Behavior |
| :--- | :--- | :--- |
| 🟢 **API Connected** | Online with Render Cloud API & MongoDB Atlas. | Orders and user updates sync immediately to cloud and local storage. |
| 🔵 **Calling API…** | An in-flight request is communicating with the cloud backend. | The application remains fully interactive; operations run in background tasks. |
| 🟠 **Local Storage (Fallback)** | Offline mode (Load-shedding, internet interruption, or cloud container sleeping). | Zero downtime. The app operates seamlessly against **SQL Server LocalDB** and local JSON files. |

> [!TIP]
> **Manual Connection Test:** Click directly on the connection badge at any time. LogiSyn will immediately ping the cloud health probe and display a confirmation toast message.

---

## 4. Application Navigation & Layout

Once logged in, LogiSyn displays a clean, high-contrast dark navy sidebar (`#132238`) on the left and an open, responsive workspace on the right.

```
┌───────────────┬────────────────────────────────────────────────────────────────────────┐
│  LogiSyn      │  [Page Title]                 [Role Badge] [Connection Badge] [Logout]│
├───────────────┼────────────────────────────────────────────────────────────────────────┤
│  Dashboard    │                                                                        │
│  Orders       │                                                                        │
│  History *    │                          MAIN WORKSPACE AREA                           │
│  Products *   │                                                                        │
│  Users **     │                                                                        │
│               │                                                                        │
└───────────────┴────────────────────────────────────────────────────────────────────────┘
* Visible to Admin and Manager only.
** Visible to Admin only.
```

- **Top Bar:** Shows the current screen name, logged-in user role, live connection badge, and a **Logout** button.
- **Toast Notifications:** Non-intrusive status popups appear in the upper right corner (e.g., *"Order #1042 saved successfully"*, *"Everything is already in sync"*) and automatically fade after 3.5 seconds.
- **Role-Gated Sidebar:** Buttons only appear if your role is authorized to open that section.

---

## 5. Step-by-Step Module Guides

---

### Module 1: Dashboard Overview

The **Dashboard** serves as the central command center for Anderson's Bakery.

#### Key Features:
1. **Live Production KPI Cards:**
   - **New / Pending Orders:** Real-time count of orders waiting to be baked or in progress.
   - **Completed Orders:** Total count of orders successfully fulfilled during the active period.
2. **Recent Orders Feed:** A chronological list of recent orders with customer name, order number, date, and status pill.
3. **Quick Export Actions (Admin Only):**
   - **Export to Excel:** Opens the batch Excel export dialog.
   - **Email Orders:** Opens the email composition dialog to dispatch production sheets directly to retail liaisons or supervisors.

---

### Module 2: Order Ingestion & Automatic Scaling

This module eliminates manual calculation of recipes, pan counts, and trolley loads.

#### Step 1: Open Order Ingestion
1. Click **Orders** on the sidebar.
2. Click the green **Create Order** button in the upper-right corner.

#### Step 2: Upload Customer Order File
You can ingest order files received from supermarkets:
- **Supported File Types:** `.pdf` (standard for Checkers & Spar purchase orders), `.xlsx`, `.xls`, and `.csv`.
- **Method A — Drag & Drop:** Drag the PDF or Excel file from Windows File Explorer and drop it onto the designated dotted drop zone.
- **Method B — File Browser:** Click **Browse Files**, locate the customer purchase order, and click **Open**.

#### Step 3: Parse and Auto-Scale
1. The selected file name will appear in the upload box.
2. Click the **Process Order (Go)** button.
3. LogiSyn extracts:
   - Supermarket / Customer Name (e.g., *"Checkers Hyper Brackenfell"* or *"Spar Western Cape"*).
   - Purchase Order Number (e.g., `#CK-90214`).
   - Order Date.
   - Line items and ordered quantities.
4. **The Scaling Engine runs automatically:**
   - **Pans:** Calculated as $\lceil \text{Quantity} / 50 \rceil$ (e.g., 120 loaves require 3 pans).
   - **Trolleys:** Calculated as $\lceil \text{Quantity} / 100 \rceil$ (e.g., 120 loaves require 2 trolleys).
   - **Ingredients:** Scaled per product recipe with a **10% safety buffer**:
     $$\text{Ingredient Total} = \text{Base Recipe Qty} \times \text{Order Qty} \times 1.10$$
   - **Unique ID Collision Detection:** If an order with that ID already exists, LogiSyn automatically assigns a unique incremental suffix (e.g., `#CK-90214-1`) to prevent overwriting past data.
5. You are immediately navigated to the **Order Production Sheet** view.

---

### Module 3: Production Sheets & Manager Order Review

Managers use the **Order Sheets View** (`OrderSheetsView`) to inspect, adjust, and approve scaled production before it moves to the ovens.

#### The Dual-Sheet Layout:
- **Left Panel (Production Sheet):** Shows each ordered product, quantity, target production line, required baking pans, trolley counts, and special baker notes.
- **Right Panel (Raw Material Scaling):** Displays the consolidated bakery-wide raw materials required for this batch (flour, yeast, sugar, salt, butter, water, etc.) with the 10% safety buffer included.

#### Actions on this Screen:
- **Add Product on the Fly:** Click **Add Product** to open the `AddMoreProducts` dialog. Select any product from the catalog, specify the additional quantity, and the pan, trolley, and ingredient totals will re-scale automatically.
- **Done / Submit:** Click **DONE** to finalize the order. This opens the output dialog presenting three options:
  1. **SAVE EXCEL:** Prompts for a file destination to export a formatted production `.xlsx` workbook.
  2. **PRINT (EXCEL):** Automatically exports and launches Excel so you can send the production sheet directly to the bakery floor printer.
  3. **EMAIL (EXCEL):** Launches Outlook with the generated Excel production sheet attached, ready to send to floor managers or clients.

---

### Module 4: Factory Floor Production Workflow (Baker / User Role)

Designed for speed, clarity, and simplicity on the factory floor.

#### Digital Workflow (On Screen):
1. The baker logs in with their `user` account.
2. The **User Orders** screen displays active pending orders.
3. Click on the order to open the **User Order Sheet**.
4. **During Production:**
   - View the exact required ingredient amounts.
   - In the **Used** column, enter the actual quantities consumed.
   - Record the actual number of pans and trolleys used.
   - Add any operational notes (e.g., *"Oven 2 ran 5°C hot; baking time reduced by 3 minutes"*).
5. **Finishing the Batch:**
   - Click the green **Mark as Complete** button at the bottom.
   - A confirmation dialog appears. Click **Yes**.
   - The order status updates immediately to **Completed**, the sheet locks into read-only mode, and the order is moved to History.

#### Alternative Offline Workflow (Printed / Paperless Excel Loop):
If your bakery floor operates away from a computer screen:
1. Export the order to Excel via the **DONE** $\rightarrow$ **SAVE EXCEL** or **PRINT (EXCEL)** dialog.
2. Staff record actuals in the highlighted Excel columns during their shift.
3. Back at the terminal, the user clicks **Upload Production Excel Sheet** on the User Orders screen.
4. LogiSyn automatically reads the recorded quantities and notes from the Excel file.
5. The system asks: *"Do you want to mark this order as Completed now?"*
   - Click **Yes** to instantly finish the order.
   - Click **No** to review the populated digital sheet first before final submission.

---

### Module 5: Order History & Quality Audit

Accessible by **Administrators** and **Managers**.

#### Features:
1. **Search Bar:** Quickly locate past orders by customer name or purchase order number.
2. **Date Range Filter:**
   - **All:** Full archive history.
   - **Today:** Orders completed during the active shift.
   - **This Week:** Weekly production review.
   - **This Month:** Monthly billing reconciliation.
3. **Auditing Completed Orders:**
   - Click on any completed order row or the **View** button.
   - Opens the read-only **Order Breakdown** displaying the exact quantities ordered, raw materials consumed, actual pan/trolley allocations, and timestamped baker notes.

---

### Module 6: Product & Recipe Catalog Management

Accessible by **Administrators** and **Managers**. Controls the recipes and storage specifications used by the automatic scaling engine.

#### Browsing Products:
- View all active bakery items with unit price, sell-by days, best-before days, and storage classification.
- **Storage Category Filter:** Filter by **Ambient** (breads, rolls, buns), **Chilled** (pastries, cream goods), or **Frozen** (doughs, pre-baked items).

#### Adding a New Product:
1. Click **Add Product** in the top action bar.
2. Fill in the product profile:
   - **Product Name:** e.g., *"Artisan Sourdough Loaf 800g"*.
   - **Price Per Unit (R):** e.g., `24.50`.
   - **Storage Location:** Select *Ambient*, *Chilled*, or *Frozen*.
   - **Sell By (Days):** Shelf life before retail pull date (e.g., `3`).
   - **Best Before (Days):** Quality expiry threshold (e.g., `5`).
   - **Preparation Method:** Step-by-step instructions for mixing, fermentation, dividing, proofing, and baking.
3. **Configure Recipe Ingredients:**
   - Enter **Ingredient Name** (e.g., *White Bread Flour*), **Quantity** (e.g., *0.5*), and **Unit** (e.g., *kg*).
   - Click **+ Add Ingredient** to add more line items (Yeast, Salt, Water, Starter, etc.).
   - Click the **Trash** icon to remove an ingredient row.
4. Click **Add Product** to save.

#### Editing & Viewing Recipe Details:
- Click the **View** icon on any product row to open the **Product Detail Modal**.
- Adjust ingredient weights, modify preparation instructions, or update shelf-life values.
- Click **Save Changes** to commit updates.

---

### Module 7: User Management & Cloud Sync

Accessible exclusively by **Administrators**.

#### Adding Staff Accounts:
1. Click **Users** on the sidebar.
2. Click **Add User**.
3. Enter:
   - **Username:** Unique login name (e.g., `baker_john`).
   - **Password:** Initial temporary password (e.g., `Bake2026!`).
   - **Role:** Select *Admin*, *Manager*, or *User*.
4. Click **Save**.

#### The Orphan Reconciliation & Cloud Sync Button:
When users are created while the bakery computer is offline or during a network outage, they are saved locally to SQL Server LocalDB with an unassigned `MongoId`. These accounts are called **Orphans**.

1. When internet connectivity is restored, click the **Sync** button in the top action bar.
2. LogiSyn scans for all local orphan accounts and pushes them to MongoDB Atlas.
3. A status modal reports the result:
   - *“Sync complete: 2 users pushed to MongoDB, 0 skipped, 0 failed.”*
4. All terminals and mobile devices can now authenticate those accounts immediately.

---

### Module 8: Reporting, Excel Export & Email Dispatch

Accessible by **Administrators** from the Dashboard or Order Breakdown view.

#### Batch Exporting to Excel:
1. On the Dashboard, click **Export to Excel**.
2. The **Export Orders Modal** appears:
   - Select the checkbox next to each order you wish to include, or check **Select All**.
   - The selection summary displays: e.g., *"3 of 12 orders selected"*.
3. Click **Export Selected**.
4. Choose your save destination in Windows Explorer.
5. LogiSyn generates an OpenXML `.xlsx` workbook containing:
   - Executive order summary headers.
   - Itemized product breakdowns with pan/trolley allocations.
   - Raw material scaling sheets formatted for inventory auditing.

#### Emailing Orders via Outlook:
1. On the Dashboard or Order Breakdown, click **Email Orders**.
2. In the modal:
   - Select the orders to dispatch.
   - Enter the **Recipient Email Address** (or multiple separated by commas).
   - Customize the **Subject** and **Cover Note**.
3. Click **Send Email**:
   - If Microsoft Outlook is installed, LogiSyn uses COM automation to draft an email with the formatted Excel production sheets automatically attached.
   - If Outlook is not present, LogiSyn activates a standard `mailto:` link with pre-populated subject and message body.

---

## 6. Mobile Web Companion Guide

LogiSyn includes a lightweight, mobile-optimized web companion accessible directly from smartphone browsers on the bakery floor without requiring APK installations (`AndersonsBakeryAPI/wwwroot/index.html`).

- **Cloud URL:** `https://andersons-bakery-api.onrender.com`
- **Supported Devices:** Any smartphone or tablet running Chrome, Safari, Firefox, or Edge.

### Complete Mobile Screen Breakdown (Figma Design Parity):
1. **Admin - Login:** High-contrast navy branding card, secure password entry, quick-fill test role chips, and JWT session generation.
2. **Admin - Dashboard:** Real-time KPI statistics ("New Orders", "Completed Orders"), live database connection status pill, and Recent Orders feed.
3. **Admin - Orders:** Comprehensive order list with search, status filters ("All", "Pending", "In Production", "Completed"), dedicated "View" buttons, and a bottom "Create New Order" action button.
4. **Admin - History:** Archive of completed orders with customer/order search and date filters ("All", "Today", "This Week", "This Month").
5. **Admin - Manage Products:** Full bakery product catalog with search, storage filters ("Ambient", "Chilled", "Frozen"), unit sell prices, "View" recipe buttons, and a bottom "Add Product" action button.
6. **Admin - Product Detail Modal:** Pop-up recipe card displaying ingredients per batch with units, step-by-step preparation method, and storage location room.
7. **Admin - Order Breakdown:** Scaled production sheet displaying itemized ingredient requirements, safety buffer additions, actuals used, and pan/trolley allocations, concluded by a full-width "DONE" button.
8. **Admin - Create Order (Upload):** Clean drag-and-drop / file browser card with dashed border, upload icon, and a prominent "GO" button to scale orders.
9. **Admin - Create Order [Order Sheets]:** Dynamic review sheet for newly scaled orders with "+ ADD PRODUCT" to append products on the fly and "DONE" to commit to the database.
10. **Admin - Add Product Modal:** Form to add new products, shelf-life specifications (sell-by and best-before days), preparation methods, and dynamic ingredient rows directly to the database.
11. **Admin - Print or Email (Order Complete Modal):** Completion modal featuring a green confirmation icon, "PRINT" (floor printer output), and "EMAIL" (dispatching production sheets).
12. **4-Tab Bottom Navigation Bar:** Persistent navigation between **Dashboard**, **Orders**, **History**, and **Products**.

### Real Database Connectivity:
- **Authentication:** `POST /api/auth/login` connects to MongoDB Atlas / SQL Server LocalDB via `LoginServiceRouter`, returning a secured JWT bearer token.
- **Orders Pipeline:** `GET /api/orders` retrieves live production orders from the database; `POST /api/orders` commits newly scaled orders directly to storage; `PUT /api/orders/{id}/status` updates order milestones in real time.
- **Recipe Catalog:** `GET /api/products` and `POST /api/products` read and write live bakery items.
- **Health & Fallback:** Header connection status pill displays 🟢 **● Live Database** when connected and 🟠 **● Offline Cache** during network dropouts, seamlessly using local cache so the mobile interface remains 100% operational on the floor.

---

## 7. Offline Resilience & Load-Shedding Survival

Anderson's Bakery operations cannot halt when internet drops or load-shedding occurs. LogiSyn is architected with a **Local-First, Failover Hybrid Strategy**:

```
                       ┌────────────────────────────┐
                       │    WPF Desktop Client      │
                       └──────────────┬─────────────┘
                                      │
            ┌─────────────────────────┴─────────────────────────┐
            │                                                   │
  [Internet Available]                                [Internet Offline]
            │                                                   │
            ▼                                                   ▼
┌────────────────────────────┐                        ┌───────────────────┐
│     Cloud Web API          │                        │ In-Process Logic  │
│        (Render)            │                        │  Local JSON Store │
│            │               │                        │  SQL LocalDB      │
│            ▼               │                        └───────────────────┘
│      MongoDB Atlas         │                        (Continues working
└────────────────────────────┘                         with ZERO disruption)
```

1. **Order Creation Works Offline:** Orders are saved directly to local storage and SQL Server LocalDB first before cloud replication is attempted.
2. **Local PDF & Excel Parsing:** PDF parsing with `PdfPig` and Excel processing with `ClosedXML` execute locally on your PC. No cloud connection or Adobe license is needed.
3. **Automatic Offline Fallback:** If MongoDB Atlas or Render cannot be contacted, the system seamlessly routes authentication and order queries to the local SQL database without error popups.
4. **Re-synchronization:** As soon as connectivity returns, clicking **Sync** pushes any accounts or orders created during downtime to the cloud database.

---

## 8. Troubleshooting & Frequently Asked Questions (FAQ)

### Q1: The PDF order failed to parse or says "Unable to parse order file."
- **Cause:** The supermarket changed their PDF purchase order format or sent a scanned bitmap image instead of a text-based digital PDF.
- **Solution:** Ensure the PDF contains selectable text (test by trying to highlight text in Adobe Acrobat or Edge). If the file is an image scan, request a digital PDF or Excel `.xlsx` / `.csv` copy from the supermarket, or use **Add Product** on the production sheet to manually input the line items.

### Q2: What should I do if the connection badge shows orange "Local Storage (Fallback)"?
- **Cause:** Internet connection is down, load-shedding is active, or the cloud API is rebooting.
- **Solution:** You do not need to take any action. LogiSyn is fully functional in offline mode. You can continue creating orders, scaling recipes, and completing production sheets. Click the badge once internet is restored to test connectivity.

### Q3: A new user account created offline cannot log in on the mobile companion.
- **Cause:** The user account was created while offline and is stored as an "orphan" in local SQL.
- **Solution:** Have an Admin open **Manage Users** on the desktop terminal and click **Sync**. Once the sync finishes, the account will be active on MongoDB and accessible on mobile.

### Q4: An order was uploaded twice by mistake. Did it overwrite the original?
- **No.** LogiSyn has built-in collision prevention (`EnsureUniqueOrderId`). If order `#4501` is imported again, LogiSyn names the second order `#4501-1`. You can safely view and delete the duplicate if not needed.

### Q5: How do I recover or reset a forgotten password?
- **Solution:** An Administrator can log in, navigate to **Users**, select the staff member, click **Edit**, and set a new temporary password. The next time the user logs in, their password will automatically be re-hashed.

---

## 9. Quick Reference Cheat Sheet

| Task | Steps | Shortcut / Location |
| :--- | :--- | :--- |
| **Log In** | Enter Username & Password $\rightarrow$ click Login | Login Screen |
| **Ingest New Order** | Orders $\rightarrow$ **Create Order** $\rightarrow$ Drag/Drop PDF $\rightarrow$ **Go** | Top right on Orders view |
| **Add Extra Product** | Inside Order Sheet $\rightarrow$ click **Add Product** | Order Sheets View |
| **Complete & Output Order** | Inside Order Sheet $\rightarrow$ click **DONE** $\rightarrow$ select **SAVE EXCEL**, **PRINT**, or **EMAIL** | Order Sheets View |
| **Finish Order (Baker)** | User Orders $\rightarrow$ open Order $\rightarrow$ enter actuals $\rightarrow$ **Mark Complete** | User Order Sheet View |
| **Upload Floor Excel** | User Orders $\rightarrow$ click **Upload Production Excel Sheet** | User Orders View |
| **Search Past Orders** | History $\rightarrow$ type in search bar or select Date filter | History View |
| **Add New Recipe** | Products $\rightarrow$ **Add Product** $\rightarrow$ enter method & ingredients $\rightarrow$ Save | Manage Products View |
| **Sync Offline Users** | Users $\rightarrow$ click **Sync** button | Manage Users View |
| **Test Cloud Status** | Click the **Connection Pill** in the top-right header | Any screen |

---

<div align="center">
  <sub>LogiSyn Management System &bull; Anderson's Bakery &bull; Documentation Version 1.0 (2026)</sub>
</div>
