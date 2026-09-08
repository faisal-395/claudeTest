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

## Running it

### Prerequisites

- .NET 8 SDK
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
dotnet build -f net8.0-windows10.0.19041.0    # or open in Visual Studio and F5
```

On first launch it points at `http://localhost:5080/`. Use **Connection Settings** on the
Login screen to repoint it once the API moves to a LAN server.

Two assets are referenced but intentionally not generated here (binary font files aren't
something this environment can produce) — drop real files at:
- `Resources/Fonts/JameelNooriNastaleeq.ttf` and `Resources/Fonts/OpenSans-Regular.ttf`
  (any Nastaliq-capable Urdu font works; the CSS/MauiProgram references are already wired up)
- `wwwroot/fonts/JameelNooriNastaleeq.ttf` (same font, used by the web-rendered print templates'
  `@font-face` in `wwwroot/css/app.css`)

## A note on the MAUI client, and what's verified vs. not

This was built in a Linux sandbox with no access to the MAUI workload (offline — the workload
feed isn't reachable) or a Windows host, so `GrainMarket.Client` could not be `dotnet build`'d
end-to-end here. That said, it wasn't shipped untested:

- **Domain, Application, Infrastructure, Api, and all three test projects**: built and tested
  directly in this environment. `dotnet test GrainMarket.sln` → 40/40 passing, zero warnings.
- **Client**: every `.razor` file (all pages, layout, `_Imports.razor`, `Main.razor`) was
  compiled with the real Razor compiler in an isolated scratch project against the actual
  `GrainMarket.Application` DTOs, to catch binding/markup errors — this is exactly the same
  compilation step the MAUI build would run, just without the MAUI-specific host. It caught and
  fixed one real bug (`Products.razor` was two-way-binding directly to a `record`'s `init`-only
  properties, which doesn't compile — reworked to local mutable fields, matching every other
  Setup page's pattern). The MAUI-specific files (`MauiProgram.cs`, `App.xaml(.cs)`,
  `MainPage.xaml(.cs)`, `Platforms/Windows/*`) were checked by hand against the standard .NET 8
  MAUI Blazor Hybrid template and got as far as this sandbox's toolchain allows: NuGet restore,
  referenced-project compilation, and Resizetizer icon/splash generation all succeeded (which
  also caught and fixed an unescaped `&` in `Package.appxmanifest`); it fails past that point
  only because WinUI3's `XamlCompiler.exe` is a native Windows binary that cannot run on Linux —
  an environment limitation, not a code defect.

**If you hit a build error in `GrainMarket.Client` on Windows**, it's most likely in the
untested 5% (the MAUI host files) rather than the Razor pages — start there.

## Bilingual / Urdu support

- Kachi, Pakki and their print templates (`Pages/Print/KachiPrint.razor`,
  `Pages/Print/PakkiPrint.razor`) are full Urdu/RTL, matching the legacy screens' layout.
- General Sale/Purchase stay English-first with a Thermal/A4 × English/Urdu print toggle, as in
  the legacy "New Sale General" screen.
- `wwwroot/css/app.css` defines `.rtl-urdu` (RTL, Nastaliq font, right-aligned) and `.ltr-inline`
  (keeps invoice numbers/amounts left-to-right inside RTL text) utility classes, and bundles the
  Urdu font via `@font-face` so it never depends on what's installed on the till PC.
- `Party`, `Product` and `ChartOfAccount` all carry `Name` + `NameUrdu` side by side, so
  English-UI screens and Urdu print templates read from the same record.

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
