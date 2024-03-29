﻿using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Group;

public class DemoteCommand : CommandBase
{
    private readonly TelegramService _telegramService;

    public DemoteCommand(TelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(IUpdateContext context, UpdateDelegate next, string[] args)
    {
        await _telegramService.AddUpdateContext(context);

        var msg = context.Update.Message;
        if (msg.ReplyToMessage != null)
        {
            msg = msg.ReplyToMessage;
        }

        var userId = msg.From.Id;
        var nameLink = msg.GetFromNameLink();

        var sendText = $"{nameLink} tidak lagi Admin";

        var promote = await _telegramService.DemoteChatMemberAsync(userId);
        if (!promote.IsSuccess)
        {
            var errorCode = promote.ErrorCode;
            var errorMessage = promote.ErrorMessage;

            sendText = $"Demote {nameLink} gagal" +
                       $"\nPesan: {errorMessage}";
        }

        await _telegramService.SendTextMessageAsync(sendText);
    }
}
