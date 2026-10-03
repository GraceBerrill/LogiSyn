# Connection strings and environment variables

This project reads database connection information from either the LOGISYN_CONNECTION environment variable or from standard ASP.NET Core connection strings (ConnectionStrings:DefaultConnection). For local development you can set either option.

Important: appsettings.json in the repository is intentionally left without a stored connection string. Use environment variables to provide runtime credentials.

PowerShell — set for the current PowerShell session

$env:LOGISYN_CONNECTION = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=LogiSynDb;Integrated Security=True;"

PowerShell — persist for your user (requires restarting shells / IDE)

setx LOGISYN_CONNECTION "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=LogiSynDb;Integrated Security=True;"

ASP.NET Core style (ConnectionStrings:DefaultConnection)

You can set the ConnectionStrings:DefaultConnection value via environment variable using a double-underscore separator:

$env:ConnectionStrings__DefaultConnection = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=LogiSynDb;Integrated Security=True;"

Or persist:

setx ConnectionStrings__DefaultConnection "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=LogiSynDb;Integrated Security=True;"

Notes and verification
- After using setx you must restart Visual Studio / your terminal for the value to be visible to new processes.
- Verify at a PowerShell prompt:

	echo $env:LOGISYN_CONNECTION

- The application already falls back to local JSON files when DB access fails (Data/users.json, Data/products.json). If you prefer not to use a DB, ensure the running app has write access to a `Data` folder under its application base directory so the fallback files can be created.

Security
- Do not commit credentials or production connection strings to source control. Prefer environment variables or secure secret stores for production.
