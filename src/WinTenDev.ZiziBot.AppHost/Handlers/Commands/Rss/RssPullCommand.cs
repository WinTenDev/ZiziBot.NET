﻿using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Rss;

public class RssPullCommand : CommandBase
{
    private readonly TelegramService _telegramService;

    public RssPullCommand(TelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);

        var chatId = _telegramService.ChatId;

        var checkUserPermission = await _telegramService.CheckUserPermission();
        if (!checkUserPermission)
        {
            Log.Warning("You must Admin or Private chat");

            return;
        }

        Log.Information("Pulling RSS in {0}", chatId);

#pragma warning disable 4014
        Task.Run(async () =>
#pragma warning restore 4014
        {
            await _telegramService.SendTextMessageAsync("Sedang memeriksa RSS feed baru..");

            var reducedChatId = _telegramService.ReducedChatId;
            var recurringId = $"rss-{reducedChatId}";
            var count = HangfireUtil.TriggerJobs(recurringId);

            if (count == 0)
            {
                await _telegramService.EditMessageTextAsync("Tampaknya tidak ada RSS baru saat ini");
            }
            else
            {
                await _telegramService.DeleteAsync(_telegramService.SentMessageId);
            }
        });
    }
}