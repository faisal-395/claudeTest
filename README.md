# Grain Market Management System

Desktop business application for **Umer Farooq & Brothers**, a grain-market commission agent
(arhti) business in Pakistan. Replaces the legacy Windows app with a modern, validated, bilingual
(English/Urdu) system, built to the technical specification in full.

## Architecture

```
GrainMarket.Client (MAUI Blazor Hybrid, Windows)  --HTTP-->  GrainMarket.Api (ASP.NET Core)
                                                                     |  Npgsql / EF Core
                                                                     v
                                                                PostgreSQL
```

- **Today**: everything runs on one Windows PC. The API is a normal ASP.NET Core process
  (run it as a Windows service via `sc create` / NSSM for production, or `dotnet run` for
  development) listening on `http://localhost:5080`. The client always talks to it over HTTP —
  it never touches Postgres directly.
- **Tomorrow**: move the API to a LAN server and change one setting. On the client, open
  **Connection Settings** (link on the Login screen) and point the API base URL at the server's
  address — `Services/AppSettings.cs` persists it via device Preferences. No rebuild, no other
  code change. Run the same `GrainMarket.Api` build on the server, pointed at the same (or a
  migrated) Postgres instance.
- Protected chart-of-accounts permission logic lives entirely in the API
  (`ChartOfAccountService`, `LedgerQueryService`, `ModulePermissionAttribute`) — never
  duplicated client-side — so this move never re-introduces a security gap.

### Solution structure

```
GrainMarket.sln
├── src/
│   ├── GrainMarket.Domain/          entities, enums — no dependencies
│   ├── GrainMarket.Application/     DTOs, FluentValidation validators, use-case services,
│   │                                 the deduction engine, unit-conversion & ledger-posting logic
│   ├── GrainMarket.Infrastructure/  EF Core + Npgsql, migrations, seed data, Serilog, JWT, BCrypt
│   ├── GrainMarket.Api/             ASP.NET Core Web API, JWT auth, permission middleware,
│   │                                 controllers (one per module)
│   └── GrainMarket.Client/          .NET MAUI Blazor Hybrid app (Windows)
└── tests/
    ├── GrainMarket.Domain.Tests/        xUnit
    ├── GrainMarket.Application.Tests/   xUnit — deduction engine, unit conversion, ledger
    │                                     posting, FluentValidation rules (29 tests)
    └── GrainMarket.Api.Tests/           WebApplicationFactory integration tests — auth,
                                          module permissions, protected-account enforcement (9 tests)
```

**40/40 tests pass.** Run them with:

```bash
dotnet test GrainMarket.sln
```

(On Linux/macOS without the MAUI workload, run the three test projects individually instead —
`GrainMarket.Client` is part of the solution and will fail to restore/build there.)

## Running it

### Prerequisites

- .NET 10 SDK
- PostgreSQL 14+ (a connection string, in `src/GrainMarket.Api/appsettings.json` under
  `ConnectionStrings:Default` — defaults to `Host=localhost;Port=5432;Database=grainmarket;
  Username=postgres;Password=postgres`)
- **Windows 10/11 with the .NET MAUI workload** to build/run `GrainMarket.Client`
  (`dotnet workload install maui`) — see "A note on the MAUI client" below.

### 1. API

```bash
createdb grainmarket   # or your usual Postgres provisioning
cd src/GrainMarket.Api
dotnet run
```

On first run the API applies EF Core migrations and seeds:
- Four roles — **Owner/Admin**, **Manager**, **Accountant**, **Clerk** (Munshi) — each with a
  full `RolePermission` matrix (see "Open items" below for why these four).
- A default admin user: **username `admin`, password `Admin@12345`** — change this immediately
  after first login (Setup ▸ User Account).
- A starter chart of accounts (Cash, Bank, income/expense accounts for each deduction, a
  suspense "Unallocated Deductions" account), the eight legacy deduction rules under
  Setup ▸ Format, default unit conversions, one active season, and three sample products.
- Swagger UI is available at `/swagger` in Development.

Before deploying anywhere real, change `Jwt:Secret` in `appsettings.json` (or override via
environment variables / user-secrets) — the checked-in value is a development placeholder.

### 2. Client (Windows only)

```bash
cd src/GrainMarket.Client
dotnet build -f net10.0-windows10.0.19041.0    # or open in Visual Studio and F5
```

On first launch it points at `https://localhost:7200/` (the API's `https` launch profile). Use
**Connection Settings** on the Login screen to repoint it once the API moves to a LAN server.

Fonts are bundled and committed — **Noto Nastaliq Urdu** (Urdu labels and print templates) and
**Open Sans** (Latin UI text), both SIL Open Font License, at `Resources/Fonts/*.ttf` and
`wwwroot/fonts/NotoNastaliqUrdu-Regular.ttf` (the web-rendered print templates' `@font-face` in
`wwwroot/css/app.css`). Swap in a different Nastaliq-capable font by replacing those files and
updating the two references (`app.css`'s `@font-face` and `MauiProgram.cs`'s `fonts.AddFont`).

## A note on the MAUI client, and what's verified vs. not

This was built in a Linux sandbox with no access to the MAUI workload (offline — the workload
feed isn't reachable) or a Windows host, so `GrainMarket.Client` could not be `dotnet build`'d
end-to-end here. That said, it wasn't shipped untested, and it was re-verified after the
.NET 8 → .NET 10 migration described below:

- **Domain, Application, Infrastructure, Api, and all three test projects**: built and tested
  directly in this environment on .NET 10. `dotnet test` (per test project — the solution
  includes the Windows-only Client, so `dotnet test GrainMarket.sln` won't run on Linux/macOS) →
  40/40 passing, zero warnings.
- **Client**: every `.razor` file (all pages, layout, `_Imports.razor`, `Main.razor`) was
  compiled with the real Razor compiler in an isolated scratch project against the actual
  `GrainMarket.Application` DTOs and `Microsoft.AspNetCore.Components.Web` 10.0.12, to catch
  binding/markup errors — this is exactly the same compilation step the MAUI build would run,
  just without the MAUI-specific host. It caught and fixed one real bug during the initial build
  (`Products.razor` was two-way-binding directly to a `record`'s `init`-only properties, which
  doesn't compile — reworked to local mutable fields, matching every other Setup page's pattern).
  The MAUI-specific files (`MauiProgram.cs`, `App.xaml(.cs)`, `MainPage.xaml(.cs)`,
  `Platforms/Windows/*`) were checked by hand against the standard MAUI Blazor Hybrid template
  and got as far as this sandbox's toolchain allows: NuGet restore and Resizetizer icon/splash
  generation succeeded (which also caught and fixed an unescaped `&` in `Package.appxmanifest`
  during the initial build); it now fails one step earlier than before, at workload resolution
  (`NETSDK1147: ... maui-tizen`), because the .NET 10 SDK here has no MAUI workload installed at
  all — on a Windows machine with `dotnet workload install maui` run, this resolves and the build
  proceeds to WinUI3's `XamlCompiler.exe`, a native Windows binary that cannot run on Linux
  regardless. Both are environment limitations, not code defects.

**If you hit a build error in `GrainMarket.Client` on Windows**, it's most likely in the
untested 5% (the MAUI host files) rather than the Razor pages — start there.

### The .NET 8 → .NET 10 migration

The solution now targets `net10.0` (`net10.0-windows10.0.19041.0` for the Client) throughout,
with every EF Core/ASP.NET Core-tied package bumped to its 10.0.x release (Npgsql.
EntityFrameworkCore.PostgreSQL 10.0.3, Microsoft.Maui.Controls 10.0.101, etc.) — see the
`.csproj` files for exact versions. Two real issues surfaced and were fixed while migrating,
both worth knowing about if you touch this code:

1. **Swashbuckle.AspNetCore 10.x** moved to OpenAPI.NET 2.x, which renamed the whole
   `Microsoft.OpenApi.Models` namespace to `Microsoft.OpenApi` and changed
   `AddSecurityRequirement`'s signature to a `Func<OpenApiDocument, OpenApiSecurityRequirement>`
   factory (so it can reference a scheme already registered in the document via
   `OpenApiSecuritySchemeReference`, rather than a duplicated inline definition). Updated in
   `Program.cs`.
2. **EF Core's `AddDbContext` now supports multiple `IDbContextOptionsConfiguration<T>`
   registrations** rather than one config action per context type. `CustomWebApplicationFactory`
   in `GrainMarket.Api.Tests` was only removing the `DbContextOptions<AppDbContext>` descriptor
   before re-adding `AddDbContext` with the InMemory provider — under EF Core 10 that left
   Npgsql's `IDbContextOptionsConfiguration<AppDbContext>` registration in place too, so both
   providers got applied to the same options and every integration test failed at startup with
   "Only a single database provider can be registered". Fixed by also removing that descriptor
   (and `DbContextOptions`/`AppDbContext` itself, for good measure) before re-adding.

If you ever need to go back to .NET 8 for some reason, reverse both TFM changes and the package
version bumps above; nothing else in the codebase is .NET-10-specific.

## Bilingual / Urdu support

- Kachi, Pakki, Dual Invoice and their print templates (`Pages/Print/KachiPrint.razor`,
  `Pages/Print/PakkiPrint.razor`) carry bilingual English/Urdu labels throughout — not just the
  print templates: field labels, dropdown options and table headers on the entry screens
  themselves show the Urdu alongside the English (e.g. "Farmer / کسان", and farmer/buyer/product
  dropdown options show `NameUrdu` next to `Name`), matching the legacy screens' bilingual layout.
- General Sale/Purchase stay English-first with a Thermal/A4 × English/Urdu print toggle, as in
  the legacy "New Sale General" screen.
- `wwwroot/css/app.css` defines `.rtl-urdu` (RTL, Nastaliq font, right-aligned) and `.ltr-inline`
  (keeps invoice numbers/amounts left-to-right inside RTL text) utility classes, and bundles the
  Urdu font via `@font-face` so it never depends on what's installed on the till PC.
- `Party`, `Product` and `ChartOfAccount` all carry `Name` + `NameUrdu` side by side, so
  English-UI screens and Urdu print templates read from the same record.

## Kachi/Pakki rate: per Man (maund), not per kg

`Kachi.RatePerUnit`/`Pakki.RatePerUnit` are quoted **per Man (maund)**, matching arhti market
convention — even though weight is always stored internally in kg (the spec's canonical base
unit). `UnitConversionCalculator.GrossAmountFromRatePerMan` converts the stored kg weight back to
Man using the same configurable Man→kg factor as weight entry (Setup ▸ Unit Conversions, with
optional per-product overrides), so a rate of "5000" against 400 kg (= 10 Man at the default
40 kg/Man) computes a gross of Rs 50,000 — not Rs 2,000,000, which is what treating the same
number as a per-kg rate would produce. Both Kachi and Pakki creation/update paths go through this
one helper, covered by `UnitConversionCalculatorTests`.

## Editing Kachi and Pakki

- **Kachi**: fully editable while `Status = Open` (`PUT /api/kachis/{id}`, `Edit` link on the
  Kachi screen) — every field can change, deductions and totals recompute.
- **Pakki**: editable while `Status = Open` (`PUT /api/pakkis/{id}`, `Edit` link on the Pakki
  screen), but only its *commercial terms* — buyer, rate, vehicle number, notes. Weight, product,
  farmer and season stay fixed once posted (they carry the originating Kachi's identity forward,
  or fix a standalone sale's own identity). A Pakki is a posted financial document — its ledger
  entries (buyer owes gross, farmer is owed net, each deduction credited to its income account)
  already exist — so `PakkiService.UpdateAsync` reverses exactly what was posted (the same
  mirror-image approach `CancelAsync` uses) and posts fresh entries for the new terms, rather than
  mutating any `LedgerEntry` row in place.

## Session persistence: refresh tokens

Access tokens are short-lived (`Jwt:ExpiryHours`, default 8h). Login and
`POST /api/auth/refresh` both return a `RefreshToken` alongside the JWT — a random 64-byte value
whose SHA-256 hash is stored in the `RefreshTokens` table (`Jwt:RefreshTokenExpiryDays`, default
30 days); the plaintext is only ever handed to the client. `TokenAuthHandler`
(`Client/Services/TokenAuthHandler.cs`) attaches the JWT to every request and:

- **Proactively refreshes** shortly before the access token expires, so a request made after the
  app has sat idle for hours doesn't even try the stale token.
- **Reactively refreshes and retries once** if a request still comes back 401 (clock skew, or the
  refresh didn't happen for some reason) — the original request is cloned and resent with the new
  token.
- **Rotates** the refresh token on every use (`AuthService.RefreshAsync` revokes the old one and
  issues a new pair) — reusing a spent refresh token is rejected.
- If the refresh token itself is gone, expired, or revoked, `AuthState.ClearDueToExpiry()` clears
  the session and raises `SessionExpired`, which `RequireAuth` (wrapping every protected page)
  turns into a redirect to `/login?expired=1` with a plain-language message — instead of the app
  ever falling through to Blazor's fatal "An unhandled error has occurred" overlay.

## Searchable dropdowns

Native `<select>` popups are rendered by WebView2 as an OS-level overlay outside Blazor's own
layout — after the host window is resized, moved, or hits a display's DPI scaling, that popup can
render detached from the control it belongs to. `Components/SearchSelect.razor` replaces it for
every farmer/buyer/product/party/account picker: a type-to-filter text input plus a menu that
renders in normal page flow, so it can never separate from its control. Static, short enum lists
(season, role, print format, account type, …) are left as native `<select>` — no benefit there,
and it keeps those forms simpler.

## Cancelling Sale Invoices and Purchases

Kachi, Pakki and every Voucher type could already be reversed (`Cancel`); Sale Invoice and
Purchase could not — there was no way for anyone, including Owner/Admin, to undo a mistaken entry
short of a database edit. Both now carry an `IsCancelled` flag and a
`POST /api/{sale-invoices|purchases}/{id}/cancel` endpoint (gated by the module's `Delete`
permission, same as every other cancel action) that posts mirror-image ledger entries — customer/
supplier, income/expense account, and any cash applied — rather than deleting the row, preserving
the audit trail the same way Kachi/Pakki/Voucher already do.

## Kachi and Pakki are separate stages with separate deductions

Kachi (provisional receipt — farmer's produce weighed, sale rate often not settled yet) and Pakki
(the finalized sale, once a buyer and rate are locked in) are independent stages, each with their
own independently configurable deductions under Setup ▸ Format (`DeductionRule.AppliesTo`: Kachi,
Pakki, or Both). Commission, Market Fee, Association Fund and Withholding Tax are seeded as
**separate rows for each stage** (e.g. "Commission" for Pakki, "Commission (Kachi)" for Kachi,
crediting separate chart-of-accounts rows) — a Kachi and the Pakki it's later converted to can
charge different amounts, or the Kachi-stage rate can be left at its seeded 0% until you actually
want to charge something at that stage. Labour (Palledari), Bagging/Stitching and Freight remain
`AppliesTo = Both`, since those are physical handling costs that make sense as soon as weight is
known, independent of whether a sale price exists yet.

`Kachi` now carries an optional `BuyerId` — set when the buyer is already known at Kachi stage
(e.g. raised via Multi-Farmer Purchase below), left null for a purely provisional weighing where
the buyer isn't decided. It plays no part in the deduction calculation, which only ever looks at
product/farmer/weight/gross.

## Kachi: multi-farmer entry + a separate records page

One buyer commonly receives from several different farmers in one sitting, so the **Kachi** screen
(`Pages/Kachi.razor`, `/kachi` — this *is* the "Multi-Farmer Purchase" screen; the old single-row
Kachi entry form no longer exists) is entry-only: pick the buyer, season and date once, then add
one row per farmer/product/weight — rate is optional, same as any Kachi. Each row becomes its own
Kachi with this buyer already attached (`Application/MultiPurchase/MultiPurchaseService`, calling
`IKachiService.CreateAsync` per row, still under the `Application.MultiPurchase` namespace/route
internally) — this never touches Pakki; finalizing each farmer's sale (choosing/confirming the
buyer, locking in the rate) stays a deliberate, separate step per farmer from the printed receipt.

Every row shows its calculated breakdown (paledari/labour, Kachi-stage commission, association
fund, withholding tax, ...) live as you fill it in, updating on every keystroke — not just after
saving. This isn't a second, hand-written copy of the math: the page calls
`Application.Common.Services.DeductionEngine`/`UnitConversionCalculator` directly with
`DeductionAppliesTo.Kachi` (the exact same pure, stateless calculators and the same code path
`KachiService.CreateAsync` uses to post the Kachi), since the Client project already references
`GrainMarket.Application`. There is no live-preview API endpoint and nothing to keep in sync — what
you see while typing is what actually gets posted.

**Kachi Records** (`Pages/KachiRecords.razor`, `/kachi-records`) is the separate management view —
every Kachi ever raised, with per-row edit/cancel/print/"Convert to Pakki" and the deduction-line
detail toggle. It's where the printed receipt's "View / edit all Kachi records" link goes, and it's
its own nav item since entry (`/kachi`) and browsing/managing existing records are different tasks.

## Open items — resolved

The spec flagged four open items to confirm before/while building. Given the instruction to
proceed autonomously, here's what was decided and why — all four are trivially changeable later:

1. **Dual Invoice.** Implemented as a single-screen shortcut
   (`Pages/DualInvoice.razor`, `Application/DualInvoice/`) that raises a Kachi and immediately
   converts it to a Pakki in one submit, for when the farmer, buyer and rate are all known up
   front. The standalone Kachi screen still exists for the provisional/unpriced case.
2. **"Trading" menu item.** Kept in scope as a stock-position report
   (`Pages/Trading.razor`, `/api/trading/stock-position`) — Purchase-in vs. Sale-out, grouped by
   product, for the business's own-account trading. This is explicitly separate from the
   commission-based Kachi/Pakki flow, which never puts produce onto the business's own books.
3. **Maund-to-kg conversion.** Seeded as **1 Man = 40 kg** (common Punjab grain-market
   convention), **1 Bori = 100 kg**, alongside Kilo=1kg/Gram=0.001kg — all editable under
   Setup ▸ Unit Conversions, with optional per-product overrides (see `UnitConversion` entity).
4. **Seed roles.** Used the spec's own four: **Owner/Admin** (full access), **Manager**
   (everything except Setup ▸ User Account), **Accountant** (view everywhere, create/edit on
   vouchers/expense/ledger/reports/recovery), **Clerk/Munshi** (Kachi/Pakki/Sale/Purchase entry,
   view-only on Dashboard/Ledger). Adjust `SeedData.SeedRolesAndPermissionsAsync` or edit
   permissions from Setup ▸ User Account once real staffing is confirmed.

## Validation & the deduction engine

`Common/Services/DeductionEngine.cs` and `UnitConversionCalculator.cs` guard exactly the
failure mode called out in the spec: a Kachi/Pakki total never renders from a `NaN` or
divide-by-zero input. Net weight must be resolved from at least one entered quantity via a
configured conversion factor before any deduction is computed; every deduction rule guards its
own output; FluentValidation rejects the request before it ever reaches these calculators if
weight/rate fields are missing. Client-side, forms disable their Save button and show an inline
warning (`.invalid-total`) until those same conditions hold — belt and suspenders, but the
server-side guard is authoritative.

## Protected accounts — how the enforcement actually works

`ChartOfAccount.IsProtected` + `ChartOfAccountRole` (many-to-many with `Role`) is enforced in
exactly one place per read path: `ChartOfAccountService.GetVisibleAsync`/`GetByIdAsync` and
`LedgerQueryService.GetAccountLedgerAsync`. A non-allowed role gets an empty/filtered list from
the list endpoint and a 404 (not 403) from the by-id endpoint — a raw API call with a guessed id
can't discover that the account exists. This is covered by
`GrainMarket.Api.Tests/ProtectedAccountTests.cs`. Separately, `ModulePermissionAttribute` enforces
per-menu-module CRUD permissions (Setup ▸ User Account's `RolePermission` rows) on every
controller action — the two systems are independent and both server-side.

## Backup

Setup ▸ Backup (`/api/backup`, `BackupService`) shells out to `pg_dump -F c`, writing a
timestamped `.backup` file under the API's `backups/` folder. Requires PostgreSQL client tools
on the server's PATH. The database password is passed via the `PGPASSWORD` environment variable,
never on the command line.
