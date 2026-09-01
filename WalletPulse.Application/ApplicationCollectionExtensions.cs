using WalletPulse.Application.Interface;
using WalletPulse.Application.Repository;
using WalletPulse.Application.Service;
using Microsoft.Extensions.DependencyInjection;

namespace WalletPulse.Application;

public static class ApplicationCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection service)
    {
        service.AddScoped<ICustomerRepository, CustomerRepository>();
        service.AddScoped<ICustomerWalletRepository, CustomerWalletRepository>();
        service.AddScoped<ICustomerWalletService, CustomerWalletService>();
        return service;
    }

}