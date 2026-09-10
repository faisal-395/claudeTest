using GrainMarket.Application.Common;
using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Domain.Entities;
using GrainMarket.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Infrastructure.Persistence;

/// <summary>
/// Idempotent runtime seed: default roles/permissions, an initial admin user, the chart of
/// accounts, the eight legacy deduction rules (Setup &gt; Format), default unit conversions and a
/// starter season/products. Everything here is editable afterwards from Setup — this only seeds
/// sane defaults so the app is usable on first run.
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
            new ChartOfAccount { Code = "4105", Name = "Kachi Commission Income", NameUrdu = "آمدنی کمیشن (کچی)", AccountType = AccountType.Income },
            new ChartOfAccount { Code = "4200", Name = "Market Fee Income", NameUrdu = "آمدنی مارکیٹ فیس", AccountType = AccountType.Income },
            new ChartOfAccount { Code = "4205", Name = "Kachi Market Fee Income", NameUrdu = "آمدنی مارکیٹ فیس (کچی)", AccountType = AccountType.Income },
            new ChartOfAccount { Code = "4300", Name = "Association Fund Income", NameUrdu = "آمدنی انجمن فنڈ", AccountType = AccountType.Income },
            new ChartOfAccount { Code = "4305", Name = "Kachi Association Fund Income", NameUrdu = "آمدنی انجمن فنڈ (کچی)", AccountType = AccountType.Income },
            new ChartOfAccount { Code = "4400", Name = "Octroi Income", NameUrdu = "آمدنی چونگی", AccountType = AccountType.Income },
            new ChartOfAccount { Code = "4500", Name = "Withholding Tax Payable", NameUrdu = "ویدہولڈنگ ٹیکس", AccountType = AccountType.Liability, IsProtected = true },
            new ChartOfAccount { Code = "4505", Name = "Kachi Withholding Tax Payable", NameUrdu = "ویدہولڈنگ ٹیکس (کچی)", AccountType = AccountType.Liability, IsProtected = true },
            new ChartOfAccount { Code = "4600", Name = "Labour (Palledari) Income", NameUrdu = "آمدنی پلیداری", AccountType = AccountType.Income },
            new ChartOfAccount { Code = "4700", Name = "Bagging/Stitching Income", NameUrdu = "آمدنی بھرائی سلائی", AccountType = AccountType.Income },
            new ChartOfAccount { Code = "4800", Name = "Freight Payable", NameUrdu = "کرایہ", AccountType = AccountType.Liability },
            new ChartOfAccount { Code = DomainConstants.UnallocatedDeductionsAccountCode, Name = "Unallocated Deductions (Suspense)", NameUrdu = "غیر مختص کٹوتیاں", AccountType = AccountType.Income },
            new ChartOfAccount { Code = DomainConstants.PurchaseExpenseAccountCode, Name = "Purchases", NameUrdu = "خریداری", AccountType = AccountType.Expense },
            new ChartOfAccount { Code = "5100", Name = "General Expenses", NameUrdu = "عمومی اخراجات", AccountType = AccountType.Expense }
        };

        db.ChartOfAccounts.AddRange(accounts);
        await db.SaveChangesAsync(ct);

        var accountsByCode = accounts.ToDictionary(a => a.Code);

        // Demonstrates the protected-account feature: Owner/Admin can always see protected
        // accounts; Accountant is additionally allowed to see the tax-liability account. Every
        // other role (Manager, Clerk) never sees these two rows, enforced server-side.
        db.ChartOfAccountRoles.AddRange(
            new ChartOfAccountRole { ChartOfAccountId = accountsByCode["3000"].Id, RoleId = roles["Owner/Admin"].Id },
            new ChartOfAccountRole { ChartOfAccountId = accountsByCode["4500"].Id, RoleId = roles["Owner/Admin"].Id },
            new ChartOfAccountRole { ChartOfAccountId = accountsByCode["4500"].Id, RoleId = roles["Accountant"].Id },
            new ChartOfAccountRole { ChartOfAccountId = accountsByCode["4505"].Id, RoleId = roles["Owner/Admin"].Id },
            new ChartOfAccountRole { ChartOfAccountId = accountsByCode["4505"].Id, RoleId = roles["Accountant"].Id });
        await db.SaveChangesAsync(ct);

        return accountsByCode;
    }

    private static async Task SeedDeductionRulesAsync(AppDbContext db, Dictionary<string, ChartOfAccount> accounts, CancellationToken ct)
    {
        var rules = new[]
        {
            new DeductionRule { Name = "Commission", NameUrdu = "بروکری / کمیشن", CalculationType = DeductionCalculationType.PercentOfGross, Value = 2m, AppliesTo = DeductionAppliesTo.Pakki, SortOrder = 1, IncomeAccountId = accounts["4100"].Id },
            new DeductionRule { Name = "Market Fee", NameUrdu = "مارکیٹ فیس", CalculationType = DeductionCalculationType.PercentOfGross, Value = 1m, AppliesTo = DeductionAppliesTo.Pakki, SortOrder = 2, IncomeAccountId = accounts["4200"].Id },
            new DeductionRule { Name = "Association Fund", NameUrdu = "انجمن فنڈ", CalculationType = DeductionCalculationType.FixedAmount, Value = 10m, AppliesTo = DeductionAppliesTo.Pakki, SortOrder = 3, IncomeAccountId = accounts["4300"].Id },
            new DeductionRule { Name = "Octroi", NameUrdu = "چونگی", CalculationType = DeductionCalculationType.PerUnitWeight, Value = 0.02m, AppliesTo = DeductionAppliesTo.Pakki, SortOrder = 4, IncomeAccountId = accounts["4400"].Id },
            new DeductionRule { Name = "Withholding Tax", NameUrdu = "ویدہولڈنگ ٹیکس", CalculationType = DeductionCalculationType.PercentOfGross, Value = 0.5m, AppliesTo = DeductionAppliesTo.Pakki, SortOrder = 5, IncomeAccountId = accounts["4500"].Id },
            new DeductionRule { Name = "Labour (Palledari)", NameUrdu = "پلیداری", CalculationType = DeductionCalculationType.PerUnitWeight, Value = 0.3m, AppliesTo = DeductionAppliesTo.Both, SortOrder = 6, IncomeAccountId = accounts["4600"].Id },
            new DeductionRule { Name = "Bagging/Stitching", NameUrdu = "بھرائی سلائی", CalculationType = DeductionCalculationType.PerUnitWeight, Value = 0.2m, AppliesTo = DeductionAppliesTo.Both, SortOrder = 7, IncomeAccountId = accounts["4700"].Id },
            new DeductionRule { Name = "Freight", NameUrdu = "کرایہ", CalculationType = DeductionCalculationType.FixedAmount, Value = 0m, AppliesTo = DeductionAppliesTo.Both, SortOrder = 8, RequiresVehicleNumber = true, IncomeAccountId = accounts["4800"].Id },

            // Kachi-stage counterparts of Commission/Market Fee/Association Fund/Withholding Tax
            // above — deliberately separate rows (own rate, own income account) so a Kachi and the
            // Pakki it later becomes can charge different amounts, edited independently under
            // Setup > Format. Seeded at 0 so nothing is charged twice until a rate is chosen here.
            new DeductionRule { Name = "Commission (Kachi)", NameUrdu = "کمیشن (کچی)", CalculationType = DeductionCalculationType.PercentOfGross, Value = 0m, AppliesTo = DeductionAppliesTo.Kachi, SortOrder = 9, IncomeAccountId = accounts["4105"].Id },
            new DeductionRule { Name = "Market Fee (Kachi)", NameUrdu = "مارکیٹ فیس (کچی)", CalculationType = DeductionCalculationType.PercentOfGross, Value = 0m, AppliesTo = DeductionAppliesTo.Kachi, SortOrder = 10, IncomeAccountId = accounts["4205"].Id },
            new DeductionRule { Name = "Association Fund (Kachi)", NameUrdu = "انجمن فنڈ (کچی)", CalculationType = DeductionCalculationType.FixedAmount, Value = 0m, AppliesTo = DeductionAppliesTo.Kachi, SortOrder = 11, IncomeAccountId = accounts["4305"].Id },
            new DeductionRule { Name = "Withholding Tax (Kachi)", NameUrdu = "ویدہولڈنگ ٹیکس (کچی)", CalculationType = DeductionCalculationType.PercentOfGross, Value = 0m, AppliesTo = DeductionAppliesTo.Kachi, SortOrder = 12, IncomeAccountId = accounts["4505"].Id }
        };

        db.DeductionRules.AddRange(rules);
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedUnitConversionsAsync(AppDbContext db, CancellationToken ct)
    {
        // Defaults only — the legacy app hardcoded these per screen; here they are editable under
        // Setup > Unit Conversions, and may be overridden per product. 1 Man (maund) = 40 kg is the
        // common Punjab grain-market convention; adjust to match local practice.
        db.UnitConversions.AddRange(
            new UnitConversion { Unit = WeightUnit.Kilo, FactorToKg = 1m },
            new UnitConversion { Unit = WeightUnit.Gram, FactorToKg = 0.001m },
            new UnitConversion { Unit = WeightUnit.Man, FactorToKg = 40m },
            new UnitConversion { Unit = WeightUnit.Bori, FactorToKg = 100m }
        );
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedSeasonAsync(AppDbContext db, CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var label = today.Month >= 7 ? $"{today.Year}-{(today.Year + 1) % 100:D2}" : $"{today.Year - 1}-{today.Year % 100:D2}";
        db.Seasons.Add(new Season { Name = label, StartDate = new DateTime(today.Year, 1, 1), IsActive = true });
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedSampleProductsAsync(AppDbContext db, CancellationToken ct)
    {
        db.Products.AddRange(
            new Product { Name = "Wheat", NameUrdu = "گندم", Category = "Grain", BaseUnit = "kg", DefaultRate = 0m },
            new Product { Name = "Rice", NameUrdu = "چاول", Category = "Grain", BaseUnit = "kg", DefaultRate = 0m },
            new Product { Name = "Maize", NameUrdu = "مکئی", Category = "Grain", BaseUnit = "kg", DefaultRate = 0m }
        );
        await db.SaveChangesAsync(ct);
    }
}
