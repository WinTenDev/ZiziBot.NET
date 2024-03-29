﻿using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Framework.Abstractions;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Welcome;

public class WelcomeDocumentCommand : CommandBase
{
    private readonly TelegramService _telegramService;
    private readonly SettingsService _settingsService;

    public WelcomeDocumentCommand(
        TelegramService telegramService,
        SettingsService settingsService
    )
    {
        _telegramService = telegramService;
        _settingsService = settingsService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);

        var msg = _telegramService.Message;
        var chatId = _telegramService.ChatId;

        if (!await _telegramService.CheckFromAdminOrAnonymous()) return;

        if (msg.ReplyToMessage == null)
        {
            await _telegramService.SendTextMessageAsync("Balas sebuah gambar, video, atau dokumen yang akan di jadikan Welcome media");
            return;
        }

        var repMsg = msg.ReplyToMessage;
        var mediaFileId = repMsg.GetFileId();

        if (mediaFileId.IsNotNullOrEmpty())
        {
            var mediaType = repMsg.Type;

            await _telegramService.SendTextMessageAsync("Sedang menyimpan Welcome Media..");
            Log.Information("MediaId: {MediaFileId}", mediaFileId);

            await _settingsService.UpdateCell(chatId, "welcome_media", mediaFileId);
            await _settingsService.UpdateCell(chatId, "welcome_media_type", mediaType);
            Log.Information("Save media success..");

            await _telegramService.EditMessageTextAsync
            (
                "Welcome Media berhasil di simpan." +
                "\nKetik /welcome untuk melihat perubahan"
            );
        }
        else
        {
            await _telegramService.SendTextMessageAsync("Media tidak terdeteksi di pesan yg di reply tersebut.");
        }
    }
}
