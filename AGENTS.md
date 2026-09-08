# Berry Good Utils

A modular WPF desktop utility suite built with .NET 8.

## Project Structure

- `BerryGoodUtils/` — Main WPF application
  - `Modules/` — Utility modules (each is a self-contained UserControl)
  - `Services/` — Windows persistence, export, and Gmail adapters
  - `Styles/` — Shared WPF styles and theme resources
- `BerryGoodUtils.Core/` — Cross-platform models, document composition, and email contracts
- `BerryGoodUtils.Core.Tests/` — Cross-platform unit tests
- `Demo/QuoteGenerator/` — Original demo WPF app that the Quote Generator module is based on

## Build & Run

The project targets `net8.0-windows` and requires the .NET 8 SDK.

```powershell
# Build
& 'C:\Program Files\dotnet\dotnet.exe' build BerryGoodUtils/BerryGoodUtils.csproj

# Test portable logic
& 'C:\Program Files\dotnet\dotnet.exe' test BerryGoodUtils.Core.Tests/BerryGoodUtils.Core.Tests.csproj

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

## Gmail Setup

Email uses Google OAuth and the Gmail API. It never asks for or stores a Gmail password.

1. In Google Cloud Console, create or select a project.
2. Enable the **Gmail API** for that project.
3. Configure the OAuth consent screen. While its publishing status is **Testing**, add the personal Gmail address under **Test users**.
4. Create an OAuth client with application type **Desktop app**.
5. Download the client JSON, rename it to `gmail-oauth-client.json`, and place it in `%APPDATA%\BerryGoodUtils\gmail-oauth-client.json`.
6. Generate a quote or part request, choose to email it, and select **Sign in / Change account**. Complete consent in the browser.

The refresh token is encrypted for the current Windows user under `%APPDATA%\BerryGoodUtils\GmailTokens` and is restored automatically when the app restarts. The OAuth JSON, token data, and personal test address must never be committed. Google OAuth apps configured as External with publishing status Testing can issue refresh tokens that expire after seven days for non-basic scopes such as Gmail; production users should not need to sign in on every launch, but the consent app must be moved out of Testing when it is ready for ongoing use.

To change the sending Gmail account, open an email preview, select **Sign out**, then **Sign in / Change account** and authenticate with the replacement account. If the replacement account is used while the OAuth app remains in Testing, add it as a Google Cloud test user first. Replacing the Google Cloud project itself requires signing out, replacing `gmail-oauth-client.json`, and signing in again.

For a production release, complete the OAuth consent and verification requirements applicable to the selected Gmail scopes. Review Google Cloud publishing rules before distributing the application.

## Mobile Migration

`BerryGoodUtils.Core` is UI- and platform-neutral and can be referenced by a future .NET MAUI iOS/Android application. Mobile apps must use platform-specific iOS and Android OAuth client IDs and redirect URI handling; do not reuse the Desktop OAuth client as the final mobile configuration. Implement `IEmailSender` and secure token storage with iOS Keychain, Android Keystore, or MAUI `SecureStorage`, while reusing the Core models, document composers, validation, and email message factories.

## Releasing an Update

The app can check for and install updates from GitHub Releases.

1. Update the version in `BerryGoodUtils/BerryGoodUtils.csproj`:
   ```xml
   <Version>1.0.1</Version>
   <AssemblyVersion>1.0.1</AssemblyVersion>
   <FileVersion>1.0.1</FileVersion>
   ```
2. Publish a self-contained EXE:
   ```powershell
   dotnet publish BerryGoodUtils/BerryGoodUtils.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
   ```
3. Create a GitHub Release with a tag matching the version, e.g. `v1.0.1`.
4. Attach the published EXE (`BerryGoodUtils.exe`) to the release. The updater looks for an asset with that exact name.
5. Deployed builds can click **Check for Updates** on the dashboard header; if a newer release tag is found, the app downloads the EXE and restarts with the new version.

The replacement happens from a temporary PowerShell helper script after the current process exits. If the app is installed under a protected folder such as `Program Files`, the helper will need elevation to overwrite the EXE. For first-time deployments, installing to a user-writable location (for example, a folder under `%LOCALAPPDATA%`) avoids this issue.
