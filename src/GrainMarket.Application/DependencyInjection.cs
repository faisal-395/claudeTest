using FluentValidation;
using GrainMarket.Application.Auth;
using GrainMarket.Application.ChartOfAccounts;
using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Application.Common.Services;
using GrainMarket.Application.Dashboard;
using GrainMarket.Application.DeductionRules;
using GrainMarket.Application.Expenses;
using GrainMarket.Application.Kachis;
using GrainMarket.Application.Ledger;
using GrainMarket.Application.MultiPurchase;
using GrainMarket.Application.MultiSale;
using GrainMarket.Application.Pakkis;
using GrainMarket.Application.Parties;
using GrainMarket.Application.Products;
using GrainMarket.Application.Purchases;
using GrainMarket.Application.Recovery;
using GrainMarket.Application.Roles;
using GrainMarket.Application.SaleInvoices;
using GrainMarket.Application.Seasons;
using GrainMarket.Application.Trading;
using GrainMarket.Application.UnitConversions;
using GrainMarket.Application.Users;
using GrainMarket.Application.Vouchers;
using Microsoft.Extensions.DependencyInjection;

namespace GrainMarket.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<ILedgerPostingService, LedgerPostingService>();
        services.AddScoped<IInvoiceNumberGenerator, InvoiceNumberGenerator>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPartyService, PartyService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IUnitConversionService, UnitConversionService>();
        services.AddScoped<IChartOfAccountService, ChartOfAccountService>();
        services.AddScoped<IDeductionRuleService, DeductionRuleService>();
        services.AddScoped<ISeasonService, SeasonService>();
        services.AddScoped<IKachiService, KachiService>();
        services.AddScoped<IPakkiService, PakkiService>();
        services.AddScoped<IMultiPurchaseService, MultiPurchaseService>();
        services.AddScoped<IMultiSaleService, MultiSaleService>();
        services.AddScoped<ISaleInvoiceService, SaleInvoiceService>();
        services.AddScoped<IPurchaseService, PurchaseService>();
        services.AddScoped<IVoucherService, VoucherService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<ILedgerQueryService, LedgerQueryService>();
        services.AddScoped<IRecoveryService, RecoveryService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ITradingService, TradingService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IUserService, UserService>();

        return services;
    }
}
