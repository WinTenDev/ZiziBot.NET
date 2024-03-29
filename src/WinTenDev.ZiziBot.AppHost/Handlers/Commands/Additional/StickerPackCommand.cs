﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Framework.Abstractions;
using Telegram.Bot.Types.Enums;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Additional;

public class StickerPackCommand : CommandBase
{
    private readonly TelegramService _telegramService;

    public StickerPackCommand(TelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(IUpdateContext context, UpdateDelegate next, string[] args)
    {
        await _telegramService.AddUpdateContext(context);

        var client = _telegramService.Client;
        var message = _telegramService.Message;
        var chatId = _telegramService.ChatId;
        var reducedChatId = _telegramService.ReducedChatId;

        if (!await _telegramService.IsBeta()) return;

        if (message.ReplyToMessage == null)
        {
            await _telegramService.SendTextMessageAsync("Balas pesan Stiker untuk membangun StikerPack");
            return;
        }

        var repMsg = message.ReplyToMessage;
        var repMsgId = repMsg.MessageId;

        if (repMsg.Type != MessageType.Sticker)
        {
            await _telegramService.SendTextMessageAsync("Balas pesan Stiker untuk membangun StikerPack.");

            return;
        }

        await _telegramService.SendTextMessageAsync("Sedang mengumpulkan StikerSet..");

        var sticker = repMsg.Sticker;
        Log.Debug("Sticker: {0}", sticker.ToJson(true));

        var setName = sticker.SetName;
        var stickerPack = await _telegramService.Client.GetStickerSetAsync(setName);
        Log.Debug("StikerPack: {0}", stickerPack.ToJson(true));

        var listStickers = stickerPack.Stickers;

        var sendEdit = $"Sedang mengunduh {listStickers.Length} Stiker" +
                       $"\nNama: {stickerPack.Name}" +
                       $"\nJudul: {stickerPack.Title}";

        await _telegramService.EditMessageTextAsync(sendEdit);
        var cachePath = Path.Combine("Storage", "Caches", reducedChatId + "_stikerpack-" + repMsgId).SanitizeSlash();
        var packsPath = Path.Combine(cachePath, "stiker-packs").EnsureDirectory(true);

        foreach (var listSticker in listStickers)
        {
            var fileId = listSticker.FileId;
            var filePath = Path.Combine(packsPath, fileId + ".webp").SanitizeSlash();
            Log.Debug("Downloading Sticker: {0} to {1}", fileId, filePath);
            var fileStream = new FileStream(filePath, FileMode.OpenOrCreate);
            await client.GetInfoAndDownloadFileAsync(fileId, fileStream);
            fileStream.Close();
            fileStream.Dispose();
        }

        await _telegramService.EditMessageTextAsync("Sedang membangun StikerPack..");
        var listStikerItem = new List<StickerItem>();
        foreach (var listSticker in listStickers)
        {
            var filePath = listSticker.FileId + ".webp";
            listStikerItem.Add(new StickerItem()
            {
                Emojis = new List<string>() { listSticker.Emoji },
                ImageFile = filePath
            });
        }

        var listStikerPacks = new List<StickerPack>
        {
            new StickerPack()
            {
                Identifier = "stiker-packs",
                Name = sticker.SetName,
                Publisher = "ZiZi StikerPack Kit",
                TrayImageFile = listStikerItem.First().ImageFile,
                ImageDataVersion = 1,
                AvoidCache = false,
                PublisherEmail = "zizibot@azhe.space",
                PublisherWebsite = new Uri("https://github.com/WinTenDev/WinTenBot.NET"),
                PrivacyPolicyWebsite = "",
                LicenseAgreementWebsite = "https://github.com/WinTenDev/WinTenBot.NET/blob/master/LICENSE",
                Stickers = listStikerItem
            }
        };

        var stikerPacksJson = new StickerAppItem()
        {
            AndroidPlayStoreLink = new Uri("https://play.google.com/store/apps/details?id=com.kanelai.stickerapp"),
            IosAppStoreLink = "",
            StickerPacks = listStikerPacks
        }.ToJson(true);

        var contents = Path.Combine(cachePath, "contents.json");

        await _telegramService.EditMessageTextAsync("Sedang menulis Metadata..");
        await File.WriteAllTextAsync(contents, stikerPacksJson);

        await _telegramService.EditMessageTextAsync("Sedang Membuat paket StikerPacks..");
        var zipFileName = $"zizi-stikerpacks-{sticker.SetName}-{repMsgId}.stikerpacks";
        var packName = Path.Combine(cachePath, $"zizi-stikerpacks-{sticker.SetName}-{repMsgId}.stikerpacks");
        var files = Directory.GetFiles(cachePath, "*.*", SearchOption.AllDirectories)
            .Where(x => !x.Contains(".stikerpacks"));
        var zipPack = files.CreateZip(packName);

        await _telegramService.SendMediaAsync(packName, MediaType.LocalDocument);
    }
}
