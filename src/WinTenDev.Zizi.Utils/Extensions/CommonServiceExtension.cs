﻿using DotNurse.Injector;
using Microsoft.Extensions.DependencyInjection;
using WinTenDev.Zizi.Models.Interfaces;

namespace WinTenDev.Zizi.Utils.Extensions;

public static class CommonServiceExtension
{
    public static IServiceCollection AddEntityFrameworkMigrations(this IServiceCollection services)
    {
        services.AddServicesFrom("WinTenDev.Zizi.DbMigrations.EfMigrations", ServiceLifetime.Scoped);

        return services;
    }

    public static IServiceCollection AddCommonService(this IServiceCollection services)
    {
        services.AddServicesFrom("WinTenDev.Zizi.Services.Callbacks", ServiceLifetime.Scoped);
        services.AddServicesFrom("WinTenDev.Zizi.Services.Externals", ServiceLifetime.Scoped);
        services.AddServicesFrom("WinTenDev.Zizi.Services.Google", ServiceLifetime.Scoped);
        services.AddServicesFrom("WinTenDev.Zizi.Services.Internals", ServiceLifetime.Scoped);
        services.AddServicesFrom("WinTenDev.Zizi.Services.Starts", ServiceLifetime.Scoped);
        services.AddServicesFrom("WinTenDev.Zizi.Services.Telegram", ServiceLifetime.Scoped);

        services.AddServicesFrom("WinTenDev.Zizi.Services.NMemory", ServiceLifetime.Singleton);

        services.AddServicesFrom("WinTenDev.Zizi.Services.StartupTasks", ServiceLifetime.Scoped,
            options => options.ImplementationBase = typeof(IStartupTask)
        );

        services.AddLocalTunnelClient();

        services.AddImagingLibrary();

        return services;
    }

    public static IServiceCollection AddImagingLibrary(this IServiceCollection services)
    {
        services.AddQRCodeDecoder();

        return services;
    }
}