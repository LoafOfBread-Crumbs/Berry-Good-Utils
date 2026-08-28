# Berry Good Utils

A modular WPF desktop utility suite built with .NET 8.

## Project Structure

- `BerryGoodUtils/` — Main WPF application
  - `Modules/` — Utility modules (each is a self-contained UserControl)
  - `Models/` — Shared data models
  - `Services/` — Shared persistence and export services
  - `Styles/` — Shared WPF styles and theme resources
- `Demo/QuoteGenerator/` — Original demo WPF app that the Quote Generator module is based on

## Build & Run

The project targets `net8.0-windows` and requires the .NET 8 SDK.

```powershell
# Build
& 'C:\Program Files\dotnet\dotnet.exe' build BerryGoodUtils/BerryGoodUtils.csproj

# Run
& 'C:\Program Files\dotnet\dotnet.exe' run --project BerryGoodUtils/BerryGoodUtils.csproj
```

Once `dotnet.exe` is on your PATH, you can also use `dotnet build` and `dotnet run`.

## Modules

Each module implements `IUtilityModule`:

- **Quote Generator** — Create customer quotes, add line items, and export to HTML/PDF. Quotes can be saved under a customer folder or as a business quote. Based on `Demo/QuoteGenerator`.
- **Part Request** — Build supplier part/service requests and generate an email-ready HTML or plain-text file.

## Adding a New Module

1. Create a folder under `BerryGoodUtils/Modules/<YourModule>/`.
2. Add a `UserControl` (XAML + code-behind) that implements `IUtilityModule`.
3. Register the module in `BerryGoodUtils/Modules/ModuleRegistry.cs`.

The dashboard will automatically create a tile for it and host its view when clicked.

## Data Storage

Application data is stored in `%APPDATA%\BerryGoodUtils\appdata.json`.

Customer folders are created under `Documents\BerryGoodUtils\Customers\<CustomerName>`, each with a `Quotes` subfolder.

Business documents are stored under `Documents\BerryGoodUtils\Business\`, including `Business\Quotes` and `Business\PartRequests`.
