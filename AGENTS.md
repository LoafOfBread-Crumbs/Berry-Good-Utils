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

The project targets `net8.0-windows10.0.19041.0` and requires the .NET 8 SDK.

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

- **Customer Management** — Add, edit, and remove customer records used across the utility suite. Customers can be opened directly from their folders on disk.
- **Part Request** — Build supplier part/service requests and generate an email-ready HTML or plain-text file.
- **Part Catalogue** — Add, edit, and remove parts from the shared catalogue. Parts carry extra identifiers (manufacturer, equipment type, part/model/serial/reference numbers, voltage, amps, frequency, phase, horsepower, kW, IP rating, refrigerant, pressures/outputs, weight, approval/build/barcode data, country of manufacture, notes) and an optional reference image that can be attached to part requests. Individual fields can be toggled on/off for inclusion in outgoing emails. You can also add a part from a reference image: the app uses Windows OCR to read the nameplate, extracts the details, and shows them in a review window for editing before saving.
- **Scheduling** — Create recurring or date-specific schedules for customers (weekly, bi-weekly, monthly, yearly, custom intervals, or specific dates), view them in a built-in calendar, and sync them to Google Calendar so alerts appear on mobile devices signed into the same Google account.

## Adding a New Module

1. Create a folder under `BerryGoodUtils/Modules/<YourModule>/`.
2. Add a `UserControl` (XAML + code-behind) that implements `IUtilityModule`.
3. Register the module in `BerryGoodUtils/Modules/ModuleRegistry.cs`.

The dashboard will automatically create a tile for it and host its view when clicked.

## Data Storage

Application data is stored in `%APPDATA%\BerryGoodUtils\appdata.json`.

Customer folders are created under `Documents\BerryGoodUtils\Customers\<CustomerName>`. Imported and locally selected visit photos are archived under `Customer Files\<yyyy-MM-dd>` inside that customer folder; timestamped post-visit comment snapshots are stored alongside them.

Business documents are stored under `Documents\BerryGoodUtils\Business\`, including `Business\PartRequests`.

## Gmail & Google Calendar Setup

Email uses Google OAuth and the Gmail API. Scheduling sync uses Google Calendar. The app never asks for or stores a Google password.

1. In Google Cloud Console, create or select a project.
2. Enable the **Gmail API**, **Google Calendar API**, and **Google Drive API** for that project.
3. Configure the OAuth consent screen. Add the `.../auth/gmail.send`, `.../auth/gmail.metadata`, `.../auth/calendar.events`, `.../auth/drive.file`, and `.../auth/drive.readonly` scopes. While its publishing status is **Testing**, add the personal Gmail address under **Test users**.
4. Create an OAuth client with application type **Desktop app**.
5. Download the client JSON, rename it to `gmail-oauth-client.json`, and place it in `%APPDATA%\BerryGoodUtils\gmail-oauth-client.json`.
6. From the main dashboard header, select **Sign in to Google**. Complete consent in the browser. The same sign-in is used for Gmail and Google Calendar sync.
7. Generate a part request and choose to email it, or open the Scheduling module and sync a schedule.

The refresh token is encrypted for the current Windows user under `%APPDATA%\BerryGoodUtils\GoogleTokens` and is restored automatically when the app restarts. The OAuth JSON, token data, and personal test address must never be committed. Adding Calendar or Drive scopes means existing users must sign out and sign in again after the update to grant the new permissions. Google OAuth apps configured as External with publishing status Testing can issue refresh tokens that expire after seven days for non-basic scopes such as Gmail/Calendar; production users should not need to sign in on every launch, but the consent app must be moved out of Testing when it is ready for ongoing use.

To change the sending Google account, use the **Sign out** button on the main dashboard header, then **Sign in to Google** and authenticate with the replacement account. If the replacement account is used while the OAuth app remains in Testing, add it as a Google Cloud test user first. Replacing the Google Cloud project itself requires signing out, replacing `gmail-oauth-client.json`, and signing in again.

For a production release, complete the OAuth consent and verification requirements applicable to the selected Gmail scopes. Review Google Cloud publishing rules before distributing the application.

## Mobile Migration

`BerryGoodUtils.Core` is UI- and platform-neutral and can be referenced by a future .NET MAUI iOS/Android application. Mobile apps must use platform-specific iOS and Android OAuth client IDs and redirect URI handling; do not reuse the Desktop OAuth client as the final mobile configuration. Implement `IEmailSender` and secure token storage with iOS Keychain, Android Keystore, or MAUI `SecureStorage`, while reusing the Core models, document composers, validation, and email message factories.

## Releasing an Update

Berry Good Utils is distributed as an x64 MSIX through a permanently private Microsoft Store audience. The Store signs, installs, and updates the application; do not restore the former GitHub EXE self-updater.

1. Increment `Version`, `AssemblyVersion`, and `FileVersion` in `BerryGoodUtils/BerryGoodUtils.csproj`.
2. Set the matching four-part `Identity Version` in `BerryGoodUtils.Package/Package.appxmanifest` (for example, app version `1.0.10` uses package version `1.0.10.0`). Store package versions must always increase.
3. Run the Core tests and Release build.
4. Build the Store upload package from `BerryGoodUtils.Package/BerryGoodUtils.Package.wapproj` for `Release|x64` with Store upload mode enabled.
5. Run the Windows App Certification Kit against the package.
6. Upload the generated `.msixupload` or `.appxupload` file as a new submission for the existing **Berry Good Utils** Partner Center product.
7. Confirm **Free** and **Private audience** still target only the approved Known User Group before submitting for certification.

The Store package identity is `GitaLoafofBread.BerryGoodUtils`. Never change an existing submission to **Public audience**; Microsoft does not allow a public product to return to private visibility.
