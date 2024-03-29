﻿using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nito.AsyncEx.Synchronous;
using Telegram.Bot;
using Telegram.Bot.Framework;
using WinTenDev.Zizi.Models.Bots;
using WinTenDev.Zizi.Models.Bots.Options;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Models.Misc;
using WinTenDev.Zizi.Utils.IO;
using WinTenDev.Zizi.Utils.Telegram;
using WTelegram;

namespace WinTenDev.Zizi.Utils.Extensions;

public static class TelegramServiceExtension
{
    public static IServiceCollection AddWtTelegramApi(this IServiceCollection services)
    {
        services.AddSingleton
        (
            provider => {
                var serviceScope = provider.CreateScope().ServiceProvider;
                var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("WTelegram");
                var tdLibConfig = serviceScope.GetRequiredService<IOptionsSnapshot<TdLibConfig>>().Value;

                var apiId = tdLibConfig.ApiId;

                string Config(string what)
                {
                    logger.LogDebug("Getting config: {What}", what);

                    switch (what)
                    {
                        case "api_id": return apiId;
                        case "api_hash": return tdLibConfig.ApiHash;
                        case "phone_number": return tdLibConfig.PhoneNumber;
                        case "verification_code":
                        {
                            logger.LogInformation("Input Verification Code: ");
                            return Console.ReadLine();
                        }
                        case "session_pathname": return $"Storage/Common/WTelegram_session_{apiId}.dat".EnsureDirectory();
                        case "first_name": return tdLibConfig.FirstName;// if sign-up is required
                        case "last_name": return tdLibConfig.LastName;// if sign-up is required
                        // case "password": return "secret!";     // if user has enabled 2FA
                        default: return null;// let WTelegramClient decide the default config
                    }
                }

                Helpers.Log = (
                    logLevel,
                    logStr
                ) => logger.Log(
                    (LogLevel)logLevel,
                    "WTelegram: {S}",
                    logStr
                );

                var client = tdLibConfig.WTelegramSessionStore switch
                {
                    WTelegramSessionStore.MongoDb => new Client(Config, new WTelegramSessionMongoDbStorageStream(apiId)),
                    WTelegramSessionStore.File => new Client(Config),
                    _ => new Client(Config)
                };

                client.CollectAccessHash = true;

                var user = client.LoginUserIfNeeded().WaitAndUnwrapException();

                logger.LogInformation(
                    "We are logged-in as {Name} (id {UserId})",
                    user.username ?? user.GetFullName(),
                    user.id
                );

                return client;
            }
        );

        return services;
    }

    public static IServiceCollection AddTelegramBotClient(this IServiceCollection services)
    {
        var scope = services.BuildServiceProvider();
        var configuration = scope.GetRequiredService<IConfiguration>();
        var configSection = configuration.GetSection(nameof(TgBotConfig));
        var tgBotConfig = scope.GetRequiredService<IOptionsSnapshot<TgBotConfig>>().Value;

        services
            .AddTransient<BotClient>()
            .Configure<BotOptions<BotClient>>(configSection)
            .Configure<CustomBotOptions<BotClient>>(configSection);

        if (tgBotConfig.CustomBotServer != null)
        {
            services.AddScoped<ITelegramBotClient>(_ =>
                new TelegramBotClient(new TelegramBotClientOptions(
                        token: tgBotConfig.ApiToken,
                        baseUrl: tgBotConfig.CustomBotServer
                    )
                )
            );
        }
        else
        {
            services.AddScoped<ITelegramBotClient>(_ =>
                new TelegramBotClient(tgBotConfig.ApiToken)
            );
        }

        return services;
    }
}