using GrainMarket.Application.Common;
using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Domain.Entities;
using GrainMarket.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Infrastructure.Persistence;

/// <summary>
/// Idempotent runtime seed: default roles/permissions, an initial admin user, the chart of
/// accounts, six deduction rules shared by Kachi and Pakki (Setup &gt; Format, AppliesTo = Both —
/// only three have a configured rate, the rest are seeded inactive), default unit conversions, the
/// current season and a starter product catalog. Everything here is editable afterwards from
/// Setup — this only seeds sane defaults so the app is usable on first run.
/// </summary>
public static class SeedData
{
    /// <summary>Change this immediately after first login — see README.</summary>
    public const string DefaultAdminUsername = "admin";
    public const string DefaultAdminPassword = "Admin@12345";

    public static async Task SeedAsync(AppDbContext db, IPasswordHasher passwordHasher, CancellationToken ct = default)
    {
        if (await db.Roles.AnyAsync(ct)) return;

        var roles = await SeedRolesAndPermissionsAsync(db, ct);
        await SeedAdminUserAsync(db, passwordHasher, roles["Owner/Admin"], ct);
        var accounts = await SeedChartOfAccountsAsync(db, roles, ct);
        await SeedDeductionRulesAsync(db, accounts, ct);
        await SeedUnitConversionsAsync(db, ct);
        await SeedSeasonAsync(db, ct);
        await SeedSampleProductsAsync(db, ct);

        await db.SaveChangesAsync(ct);
    }

    private static async Task<Dictionary<string, Role>> SeedRolesAndPermissionsAsync(AppDbContext db, CancellationToken ct)
    {
        var allModules = Enum.GetValues<ModuleName>();

        Role MakeRole(string name, string nameUrdu, Func<ModuleName, RolePermission> permissionFor)
        {
            var role = new Role { Name = name, NameUrdu = nameUrdu, IsSystemRole = true };
            foreach (var module in allModules)
            {
                role.Permissions.Add(permissionFor(module));
            }
            return role;
        }

        var owner = MakeRole("Owner/Admin", "مالک",
            m => new RolePermission { Module = m, CanView = true, CanCreate = true, CanEdit = true, CanDelete = true });

        var manager = MakeRole("Manager", "منیجر",
            m => new RolePermission
            {
                Module = m,
                CanView = true,
                CanCreate = m != ModuleName.SetupUsersRoles,
                CanEdit = m != ModuleName.SetupUsersRoles,
                CanDelete = false
            });

        var accountant = MakeRole("Accountant", "اکاؤنٹنٹ",
            m => new RolePermission
            {
                Module = m,
                CanView = true,
                CanCreate = m is ModuleName.Payment or ModuleName.Receipt or ModuleName.Journal or ModuleName.Expense
                    or ModuleName.Ledger or ModuleName.Reports or ModuleName.Recovery,
                CanEdit = m is ModuleName.Payment or ModuleName.Receipt or ModuleName.Journal or ModuleName.Expense,
                CanDelete = false
            });

        var clerk = MakeRole("Clerk", "منشی",
            m => new RolePermission
            {
                Module = m,
                CanView = m is ModuleName.Dashboard or ModuleName.Kachi or ModuleName.Pakki or ModuleName.SaleInvoice
                    or ModuleName.Purchase or ModuleName.Ledger,
                CanCreate = m is ModuleName.Kachi or ModuleName.Pakki or ModuleName.SaleInvoice or ModuleName.Purchase,
                CanEdit = m is ModuleName.Kachi,
                CanDelete = false
            });

        db.Roles.AddRange(owner, manager, accountant, clerk);
        await db.SaveChangesAsync(ct);

        return new Dictionary<string, Role>
        {
            ["Owner/Admin"] = owner,
            ["Manager"] = manager,
            ["Accountant"] = accountant,
            ["Clerk"] = clerk
        };
    }

    private static async Task SeedAdminUserAsync(AppDbContext db, IPasswordHasher passwordHasher, Role ownerRole, CancellationToken ct)
    {
        db.Users.Add(new User
        {
            Username = DefaultAdminUsername,
            FullName = "Administrator",
            PasswordHash = passwordHasher.Hash(DefaultAdminPassword),
            RoleId = ownerRole.Id,
            IsActive = true
        });
        await db.SaveChangesAsync(ct);
    }

    private static async Task<Dictionary<string, ChartOfAccount>> SeedChartOfAccountsAsync(AppDbContext db, Dictionary<string, Role> roles, CancellationToken ct)
    {
        var accounts = new[]
        {
            new ChartOfAccount { Code = DomainConstants.CashAccountCode, Name = "Cash in Hand", NameUrdu = "نقد", AccountType = AccountType.Asset },
            new ChartOfAccount { Code = DomainConstants.BankAccountCode, Name = "Bank", NameUrdu = "بینک", AccountType = AccountType.Asset },
            new ChartOfAccount { Code = "3000", Name = "Owner's Equity", NameUrdu = "سرمایہ", AccountType = AccountType.Equity, IsProtected = true },
            new ChartOfAccount { Code = DomainConstants.SalesIncomeAccountCode, Name = "Sales Income", NameUrdu = "آمدنی فروخت", AccountType = AccountType.Income },
            new ChartOfAccount { Code = "4100", Name = "Commission Income", NameUrdu = "آمدنی کمیشن", AccountType = AccountType.Income },
            new ChartOfAccount { Code = "4200", Name = "Market Fee Income", NameUrdu = "آمدنی مارکیٹ فیس", AccountType = AccountType.Income },
            new ChartOfAccount { Code = "4110", Name = "Brokerage Income", NameUrdu = "آمدنی بروکری", AccountType = AccountType.Income },
            new ChartOfAccount { Code = "4120", Name = "Arhat Income", NameUrdu = "آمدنی آڑت", AccountType = AccountType.Income },
            new ChartOfAccount { Code = "4300", Name = "Association Fund Income", NameUrdu = "آمدنی انجمن فنڈ", AccountType = AccountType.Income },
            new ChartOfAccount { Code = "4600", Name = "Labour (Palledari) Income", NameUrdu = "آمدنی پلیداری", AccountType = AccountType.Income },
            new ChartOfAccount { Code = "4610", Name = "Bharai Income", NameUrdu = "آمدنی بھرائی", AccountType = AccountType.Income },
            new ChartOfAccount { Code = "4620", Name = "Silvai Income", NameUrdu = "آمدنی سلائی", AccountType = AccountType.Income },
            new ChartOfAccount { Code = "4630", Name = "Dhaga Lagai Income", NameUrdu = "آمدنی دھاگہ لگائی", AccountType = AccountType.Income },
            new ChartOfAccount { Code = "4640", Name = "Dumra Karai Income", NameUrdu = "آمدنی ڈمرہ کرائی", AccountType = AccountType.Income },
            new ChartOfAccount { Code = "4650", Name = "Sotli Income", NameUrdu = "آمدنی سوتلی", AccountType = AccountType.Income },
            new ChartOfAccount { Code = "4660", Name = "Bardana Income", NameUrdu = "آمدنی بردانہ", AccountType = AccountType.Income },
            new ChartOfAccount { Code = "4500", Name = "Withholding Tax Payable", NameUrdu = "ویدہولڈنگ ٹیکس", AccountType = AccountType.Liability, IsProtected = true },
            new ChartOfAccount { Code = DomainConstants.UnallocatedDeductionsAccountCode, Name = "Unallocated Deductions (Suspense)", NameUrdu = "غیر مختص کٹوتیاں", AccountType = AccountType.Income },
            new ChartOfAccount { Code = DomainConstants.PurchaseExpenseAccountCode, Name = "Purchases", NameUrdu = "خریداری", AccountType = AccountType.Expense },
            new ChartOfAccount { Code = "5100", Name = "General Expenses", NameUrdu = "عمومی اخراجات", AccountType = AccountType.Expense }
        };

        // A reconciliation migration (e.g. ReconcileKachiOnlyDeductionSeed,
        // SeedPakkiVendorDeductionRules) may already have inserted some of these codes via raw SQL
        // before this runs — migrations always apply before SeedAsync, even on a brand-new database.
        // Skip codes that already exist instead of assuming the table is empty.
        var existingAccounts = await db.ChartOfAccounts.ToDictionaryAsync(a => a.Code, ct);
        var newAccounts = accounts.Where(a => !existingAccounts.ContainsKey(a.Code)).ToArray();
        db.ChartOfAccounts.AddRange(newAccounts);
        await db.SaveChangesAsync(ct);

        var accountsByCode = accounts.ToDictionary(a => a.Code, a => existingAccounts.TryGetValue(a.Code, out var existing) ? existing : a);

        // Demonstrates the protected-account feature: Owner/Admin can always see protected
        // accounts; Accountant is additionally allowed to see the tax-liability account. Every
        // other role (Manager, Clerk) never sees these two rows, enforced server-side.
        db.ChartOfAccountRoles.AddRange(
            new ChartOfAccountRole { ChartOfAccountId = accountsByCode["3000"].Id, RoleId = roles["Owner/Admin"].Id },
            new ChartOfAccountRole { ChartOfAccountId = accountsByCode["4500"].Id, RoleId = roles["Owner/Admin"].Id },
            new ChartOfAccountRole { ChartOfAccountId = accountsByCode["4500"].Id, RoleId = roles["Accountant"].Id });
        await db.SaveChangesAsync(ct);

        return accountsByCode;
    }

    private static async Task SeedDeductionRulesAsync(AppDbContext db, Dictionary<string, ChartOfAccount> accounts, CancellationToken ct)
    {
        // Labour/Brokerage/Withholding Tax/Association Fund/Arhat are Kachi-only here (charged to
        // the Seller/farmer) with a separate Pakki-only twin below charged to the Buyer instead — in
        // Pakki, every tax is paid by the Buyer, unlike Kachi where the farmer bears these. Commission
        // is the one rule that's already Buyer-charged for both stages, so it stays AppliesTo = Both
        // with no twin needed. Only Labour/Brokerage/Commission have a real rate; the rest are seeded
        // inactive at 0 until the market's actual rate is entered under Setup > Format. IsActive is
        // what the Kachi/Pakki tax summary filters on (see KachiRules in Kachi.razor and its Pakki
        // equivalent) and what DeductionEngine actually charges, so an inactive placeholder is safe
        // to leave seeded — it shows nowhere and charges nothing until switched on.
        var rules = new[]
        {
            new DeductionRule { Name = "Labour (Palledari)", NameUrdu = "پلیداری", CalculationType = DeductionCalculationType.PercentOfGross, Value = 0.75m, AppliesTo = DeductionAppliesTo.Kachi, ChargedTo = DeductionChargedTo.Seller, SortOrder = 1, IsActive = true, IncomeAccountId = accounts["4600"].Id },
            new DeductionRule { Name = "Brokerage", NameUrdu = "بروکری", CalculationType = DeductionCalculationType.PercentOfGross, Value = 0.15m, AppliesTo = DeductionAppliesTo.Kachi, ChargedTo = DeductionChargedTo.Seller, SortOrder = 2, IsActive = true, IncomeAccountId = accounts["4110"].Id },
            new DeductionRule { Name = "Commission", NameUrdu = "کمیشن", CalculationType = DeductionCalculationType.PercentOfGross, Value = 1.60m, AppliesTo = DeductionAppliesTo.Both, ChargedTo = DeductionChargedTo.Buyer, SortOrder = 3, IsActive = true, IncomeAccountId = accounts["4100"].Id },
            new DeductionRule { Name = "Withholding Tax", NameUrdu = "ویدہولڈنگ ٹیکس", CalculationType = DeductionCalculationType.PercentOfGross, Value = 0m, AppliesTo = DeductionAppliesTo.Kachi, ChargedTo = DeductionChargedTo.Seller, SortOrder = 4, IsActive = false, IncomeAccountId = accounts["4500"].Id },
            new DeductionRule { Name = "Association Fund", NameUrdu = "انجمن فنڈ", CalculationType = DeductionCalculationType.FixedAmount, Value = 0m, AppliesTo = DeductionAppliesTo.Kachi, ChargedTo = DeductionChargedTo.Seller, SortOrder = 5, IsActive = false, IncomeAccountId = accounts["4300"].Id },
            new DeductionRule { Name = "Arhat", NameUrdu = "آڑت", CalculationType = DeductionCalculationType.PercentOfGross, Value = 0m, AppliesTo = DeductionAppliesTo.Kachi, ChargedTo = DeductionChargedTo.Seller, SortOrder = 6, IsActive = false, IncomeAccountId = accounts["4120"].Id },

            // Pakki-only twins of the five Kachi rules above — same rate/account, Buyer-charged
            // instead of Seller-charged, kept as separate rules (not a shared ChargedTo) since the
            // two stages now genuinely differ on who pays, and rates may also diverge later.
            new DeductionRule { Name = "Labour (Palledari) (Pakki)", NameUrdu = "پلیداری (پکی)", CalculationType = DeductionCalculationType.PercentOfGross, Value = 0.75m, AppliesTo = DeductionAppliesTo.Pakki, ChargedTo = DeductionChargedTo.Buyer, SortOrder = 15, IsActive = true, IncomeAccountId = accounts["4600"].Id },
            new DeductionRule { Name = "Brokerage (Pakki)", NameUrdu = "بروکری (پکی)", CalculationType = DeductionCalculationType.PercentOfGross, Value = 0.15m, AppliesTo = DeductionAppliesTo.Pakki, ChargedTo = DeductionChargedTo.Buyer, SortOrder = 16, IsActive = true, IncomeAccountId = accounts["4110"].Id },
            new DeductionRule { Name = "Withholding Tax (Pakki)", NameUrdu = "ویدہولڈنگ ٹیکس (پکی)", CalculationType = DeductionCalculationType.PercentOfGross, Value = 0m, AppliesTo = DeductionAppliesTo.Pakki, ChargedTo = DeductionChargedTo.Buyer, SortOrder = 17, IsActive = false, IncomeAccountId = accounts["4500"].Id },
            new DeductionRule { Name = "Association Fund (Pakki)", NameUrdu = "انجمن فنڈ (پکی)", CalculationType = DeductionCalculationType.FixedAmount, Value = 0m, AppliesTo = DeductionAppliesTo.Pakki, ChargedTo = DeductionChargedTo.Buyer, SortOrder = 18, IsActive = false, IncomeAccountId = accounts["4300"].Id },
            new DeductionRule { Name = "Arhat (Pakki)", NameUrdu = "آڑت (پکی)", CalculationType = DeductionCalculationType.PercentOfGross, Value = 0m, AppliesTo = DeductionAppliesTo.Pakki, ChargedTo = DeductionChargedTo.Buyer, SortOrder = 19, IsActive = false, IncomeAccountId = accounts["4120"].Id },

            // Pakki-only bag-handling charges (bharai, silvai, dhaga lagai, dumra karai, sotli,
            // bardana) — charged to the Vendor/Buyer, same percent-of-gross shape as Labour/
            // Brokerage/Commission above. Seeded inactive at 0% until the market's actual rate is
            // entered under Setup > Format.
            new DeductionRule { Name = "Bharai", NameUrdu = "بھرائی", CalculationType = DeductionCalculationType.PercentOfGross, Value = 0m, AppliesTo = DeductionAppliesTo.Pakki, ChargedTo = DeductionChargedTo.Buyer, SortOrder = 7, IsActive = false, IncomeAccountId = accounts["4610"].Id },
            new DeductionRule { Name = "Silvai", NameUrdu = "سلائی", CalculationType = DeductionCalculationType.PercentOfGross, Value = 0m, AppliesTo = DeductionAppliesTo.Pakki, ChargedTo = DeductionChargedTo.Buyer, SortOrder = 8, IsActive = false, IncomeAccountId = accounts["4620"].Id },
            new DeductionRule { Name = "Dhaga Lagai", NameUrdu = "دھاگہ لگائی", CalculationType = DeductionCalculationType.PercentOfGross, Value = 0m, AppliesTo = DeductionAppliesTo.Pakki, ChargedTo = DeductionChargedTo.Buyer, SortOrder = 9, IsActive = false, IncomeAccountId = accounts["4630"].Id },
            new DeductionRule { Name = "Dumra Karai", NameUrdu = "ڈمرہ کرائی", CalculationType = DeductionCalculationType.PercentOfGross, Value = 0m, AppliesTo = DeductionAppliesTo.Pakki, ChargedTo = DeductionChargedTo.Buyer, SortOrder = 10, IsActive = false, IncomeAccountId = accounts["4640"].Id },
            new DeductionRule { Name = "Sotli", NameUrdu = "سوتلی", CalculationType = DeductionCalculationType.PercentOfGross, Value = 0m, AppliesTo = DeductionAppliesTo.Pakki, ChargedTo = DeductionChargedTo.Buyer, SortOrder = 11, IsActive = false, IncomeAccountId = accounts["4650"].Id },
            new DeductionRule { Name = "Bardana", NameUrdu = "بردانہ", CalculationType = DeductionCalculationType.PercentOfGross, Value = 0m, AppliesTo = DeductionAppliesTo.Pakki, ChargedTo = DeductionChargedTo.Buyer, SortOrder = 12, IsActive = false, IncomeAccountId = accounts["4660"].Id },

            // Market Fee — Rs 2 per 100kg (0.02/kg), charged to the Buyer, on top of the price (same
            // shape as Commission). Kept as two separate rules (not AppliesTo = Both) since the rate
            // can differ by stage; both start at the same 2/100kg figure and are independently
            // editable under Setup > Format.
            new DeductionRule { Name = "Market Fee (Kachi)", NameUrdu = "مارکیٹ فیس (کچی)", CalculationType = DeductionCalculationType.PerUnitWeight, Value = 0.02m, AppliesTo = DeductionAppliesTo.Kachi, ChargedTo = DeductionChargedTo.Buyer, SortOrder = 13, IsActive = true, IncomeAccountId = accounts["4200"].Id },
            new DeductionRule { Name = "Market Fee (Pakki)", NameUrdu = "مارکیٹ فیس (پکی)", CalculationType = DeductionCalculationType.PerUnitWeight, Value = 0.02m, AppliesTo = DeductionAppliesTo.Pakki, ChargedTo = DeductionChargedTo.Buyer, SortOrder = 14, IsActive = true, IncomeAccountId = accounts["4200"].Id }
        };

        // Same reconciliation-migration overlap as SeedChartOfAccountsAsync above: skip any rule a
        // migration already inserted, keyed the same way those migrations key their own NOT EXISTS
        // checks — (Name, AppliesTo). The global soft-delete query filter already excludes
        // IsDeleted rows, matching the migrations' "AND IsDeleted = FALSE" condition.
        var existingRuleKeys = (await db.DeductionRules.Select(r => new { r.Name, r.AppliesTo }).ToListAsync(ct))
            .Select(r => (r.Name, r.AppliesTo))
            .ToHashSet();
        var newRules = rules.Where(r => !existingRuleKeys.Contains((r.Name, r.AppliesTo))).ToArray();
        db.DeductionRules.AddRange(newRules);
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedUnitConversionsAsync(AppDbContext db, CancellationToken ct)
    {
        // Defaults only — the legacy app hardcoded these per screen; here they are editable under
        // Setup > Unit Conversions, and may be overridden per product. 1 Man (maund) = 40 kg is the
        // common Punjab grain-market convention; adjust to match local practice.
        var conversions = new[]
        {
            new UnitConversion { Unit = WeightUnit.Kilo, FactorToKg = 1m },
            new UnitConversion { Unit = WeightUnit.Gram, FactorToKg = 0.001m },
            new UnitConversion { Unit = WeightUnit.Man, FactorToKg = 40m },
            new UnitConversion { Unit = WeightUnit.Bori, FactorToKg = 100m },
            new UnitConversion { Unit = WeightUnit.Dhrn, FactorToKg = 5m }
        };

        // A reconciliation migration (e.g. DhrnAsDisplayBreakdown, which inserts the Dhrn row
        // directly) may already have inserted one of these — same overlap as ChartOfAccounts/
        // DeductionRules above, keyed the same way: (Unit, ProductId).
        var existingKeys = (await db.UnitConversions.Select(c => new { c.Unit, c.ProductId }).ToListAsync(ct))
            .Select(c => (c.Unit, c.ProductId))
            .ToHashSet();
        var newConversions = conversions.Where(c => !existingKeys.Contains((c.Unit, c.ProductId))).ToArray();
        db.UnitConversions.AddRange(newConversions);
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedSeasonAsync(AppDbContext db, CancellationToken ct)
    {
        // A reconciliation migration (ExpandDeductionRulesToBothStagesAndSeedSeason) may already
        // have inserted this season directly — same overlap as ChartOfAccounts/DeductionRules above.
        const string name = "2026-27";
        if (await db.Seasons.AnyAsync(s => s.Name == name, ct)) return;

        db.Seasons.Add(new Season { Name = name, StartDate = new DateTime(2026, 7, 1), IsActive = true });
        await db.SaveChangesAsync(ct);
    }

    /// <summary>The ten crops most commonly traded through a Punjab grain market (mandi) — capped
    /// at 10 deliberately, not an exhaustive product catalog. More can always be added from Setup >
    /// Products afterwards.</summary>
    private static async Task SeedSampleProductsAsync(AppDbContext db, CancellationToken ct)
    {
        var products = new[]
        {
            new Product { Name = "Wheat", NameUrdu = "گندم", Category = "Grain", BaseUnit = "kg", DefaultRate = 0m },
            new Product { Name = "Rice", NameUrdu = "چاول", Category = "Grain", BaseUnit = "kg", DefaultRate = 0m },
            new Product { Name = "Maize", NameUrdu = "مکئی", Category = "Grain", BaseUnit = "kg", DefaultRate = 0m },
            new Product { Name = "Gram", NameUrdu = "چنا", Category = "Grain", BaseUnit = "kg", DefaultRate = 0m },
            new Product { Name = "Barley", NameUrdu = "جو", Category = "Grain", BaseUnit = "kg", DefaultRate = 0m },
            new Product { Name = "Millet", NameUrdu = "باجرہ", Category = "Grain", BaseUnit = "kg", DefaultRate = 0m },
            new Product { Name = "Sorghum", NameUrdu = "جوار", Category = "Grain", BaseUnit = "kg", DefaultRate = 0m },
            new Product { Name = "Mustard", NameUrdu = "سرسوں", Category = "Grain", BaseUnit = "kg", DefaultRate = 0m },
            new Product { Name = "Moong", NameUrdu = "مونگ", Category = "Grain", BaseUnit = "kg", DefaultRate = 0m },
            new Product { Name = "Masoor", NameUrdu = "مسور", Category = "Grain", BaseUnit = "kg", DefaultRate = 0m }
        };

        // A reconciliation migration (SeedAdditionalGrainProducts) may already have inserted seven of
        // these directly — same overlap as ChartOfAccounts/DeductionRules above, keyed by Name (the
        // same key that migration's own NOT EXISTS check uses).
        var existingNames = (await db.Products.Select(p => p.Name).ToListAsync(ct)).ToHashSet();
        var newProducts = products.Where(p => !existingNames.Contains(p.Name)).ToArray();
        db.Products.AddRange(newProducts);
        await db.SaveChangesAsync(ct);
    }
}
