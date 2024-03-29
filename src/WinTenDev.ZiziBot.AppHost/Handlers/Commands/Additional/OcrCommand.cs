﻿using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Additional;

public class OcrCommand : CommandBase
{
    private readonly TelegramService _telegramService;

    public OcrCommand(TelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        _telegramService.OptiicDevOcrAsync().InBackground();
    }
}
