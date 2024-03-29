﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Framework.Abstractions;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Additional;

public class TranslateCommand : CommandBase
{
    private readonly TelegramService _telegramService;

    public TranslateCommand(TelegramService telegramService)
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

        var message = _telegramService.MessageOrEdited;
        var userLang = message.From?.LanguageCode;

        if (message?.ReplyToMessage == null)
        {
            var hint = await "Balas pesan yang ingin anda terjemahkan".GoogleTranslatorAsync(userLang);
            await _telegramService.SendTextMessageAsync(hint);

            return;
        }

        var param = message.Text.SplitText(" ").ToArray();
        var param1 = param.ElementAtOrDefault(1) ?? "";

        if (param1.IsNullOrEmpty())
        {
            param1 = message.From?.LanguageCode;
        }

        var forTranslate = message.ReplyToMessage.Text ?? message.ReplyToMessage.Caption;

        Log.Information("Param: {V}", param);

        await _telegramService.SendTextMessageAsync("🔄 Translating into Your language..");

        try
        {
            var translate = await forTranslate.GoogleTranslatorAsync(param1);

            await _telegramService.EditMessageTextAsync(translate);
        }
        catch (Exception ex)
        {
            Log.Error(ex.Demystify(), "Error translation");

            var messageError = "Error translation" +
                               $"\nMessage: {ex.Message}";

            await _telegramService.EditMessageTextAsync(messageError);
        }
    }
}
