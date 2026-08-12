using DesfudenciFy.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DesfudenciFy.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<BalanceService>();
        services.AddScoped<AuthService>();
        services.AddScoped<UserService>();
        services.AddScoped<BankAccountService>();
        services.AddScoped<InvestmentTypeService>();
        services.AddScoped<IncomeTypeService>();
        services.AddScoped<ReserveService>();
        services.AddScoped<EntryService>();
        services.AddScoped<InvestmentService>();
        services.AddScoped<PropertyService>();
        services.AddScoped<FixedCostService>();
        services.AddScoped<IncomeSourceService>();
        services.AddScoped<PurchaseService>();
        services.AddScoped<DashboardService>();
        return services;
    }
}
