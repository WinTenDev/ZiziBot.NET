﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace WinTenDev.Zizi.Services.Extensions;

public static class CallbackQueryExtension
{
    public static async Task<bool> OnCallbackPingAsync(this TelegramService telegramService)
    {
        Log.Information("Receiving Ping callback");
        var callbackQuery = telegramService.CallbackQuery;

        var callbackData = callbackQuery.Data;
        Log.Debug("CallbackData: {CallbackData}", callbackData);

        var answerCallback = $"Callback: {callbackData}";

        await telegramService.AnswerCallbackQueryAsync(answerCallback, showAlert: true);

        return true;
    }

    public static async Task<bool> OnCallbackUnRestrictMemberAsync(this TelegramService telegramService)
    {
        Log.Information("Receiving Ping callback");
        var chatId = telegramService.ChatId;
        var fromId = telegramService.FromId;
        var userIdTarget = telegramService.GetCallbackDataAt<long>(1);

        if (!await telegramService.CheckUserPermission())
        {
            Log.Information(
                "UserId: '{UserId}' at ChatId: '{ChatId}' has no permission to delete message",
                fromId,
                chatId
            );

            await telegramService.AnswerCallbackQueryAsync("Kamu tidak mempunyai akses melakukan tindakan ini!", true);
            return true;
        }

        var result = await telegramService.UnmuteChatMemberAsync(userIdTarget);

        var chatMember = await telegramService.GetChatMemberAsync(userIdTarget);
        var nameLink = chatMember.User.GetNameLink();

        await telegramService.EditMessageCallback($"Berhasil membuka Mute {nameLink}");

        return true;
    }

    public static async Task<bool> OnCallbackDeleteAsync(this TelegramService telegramService)
    {
        var chatId = telegramService.ChatId;
        var fromId = telegramService.FromId;
        var messageTarget = telegramService.GetCallbackDataAt<string>(1);

        Log.Information(
            "Callback delete message at ChatId: {ChatId}. Target: {MessageTarget}",
            chatId,
            messageTarget
        );

        if (!await telegramService.CheckUserPermission())
        {
            Log.Information(
                "UserId: '{UserId}' at ChatId: '{ChatId}' has no permission to delete message",
                fromId,
                chatId
            );

            await telegramService.AnswerCallbackQueryAsync("Kamu tidak mempunyai akses melakukan tindakan ini!", true);
            return true;
        }

        switch (messageTarget)
        {
            case "current-message":
                await telegramService.DeleteCurrentCallbackMessageAsync();
                break;
            case "purge":
                var startMessageId = telegramService.GetCallbackDataAt<int>(2);
                var endMessageId = telegramService.GetCallbackDataAt<int>(3);
                var userId = telegramService.GetCallbackDataAt<int>(4);

                var wTelegramApiService = telegramService.GetRequiredService<WTelegramApiService>();
                var messages = await wTelegramApiService.GetAllMessagesAsync(
                    chatId: chatId,
                    startMessageId: startMessageId,
                    endMessageId: endMessageId,
                    userId: userId
                );

                var messageIds = messages.Select(message => message.ID).ToList();

                var affectedMessages = await wTelegramApiService.DeleteMessagesAsync(chatId, messageIds);

                // await messageIds.AsyncParallelForEach(maxDegreeOfParallelism: 10, body: messageId => telegramService.DeleteAsync(messageId));

                await telegramService.AnswerCallbackQueryAsync($"Sekitar {affectedMessages} pesan dihapus", true);
                await telegramService.DeleteCurrentCallbackMessageAsync();

                break;
            default:
            {
                var messageId = telegramService.GetCallbackDataAt<int>(1);
                await telegramService.DeleteAsync(messageId);
                break;
            }
        }

        return true;
    }

    public static async Task<bool> OnCallbackVerifyAsync(this TelegramService telegramService)
    {
        Log.Information("Executing Verify Callback");

        var callbackQuery = telegramService.CallbackQuery;
        var callbackData = callbackQuery.Data;
        var fromId = telegramService.FromId;
        var chatId = telegramService.ChatId;

        var stepHistoriesService = telegramService.GetRequiredService<StepHistoriesService>();
        var userProfilePhotoService = telegramService.GetRequiredService<UserProfilePhotoService>();
        var autoMapper = telegramService.GetRequiredService<IMapper>();

        Log.Debug(
            "CallbackData: {CallbackData} from {FromId}",
            callbackData,
            fromId
        );

        var partCallbackData = callbackData.Split(" ");
        var callBackParam1 = partCallbackData.ElementAtOrDefault(1);
        var answer = "Tombol ini bukan untukmu Bep!";

        Log.Debug("Verify Param1: {Param}", callBackParam1);

        Log.Information("Starting Verify from History for UserId: {UserId}", fromId);
        var needVerifyList = (await stepHistoriesService.GetStepHistoryVerifyCore(
            new StepHistoryDto()
            {
                ChatId = chatId,
                UserId = fromId
            }
        )).ToList();

        if (!needVerifyList.Any())
        {
            answer = "Kamu tidak perlu verifikasi!";
        }
        else
        {
            await userProfilePhotoService.ResetUserProfilePhotoCacheAsync(fromId);

            foreach (var step in needVerifyList)
            {
                var stepHistoryDto = new StepHistoryDto();
                stepHistoryDto = autoMapper.Map<StepHistoryDto>(step);

                // var updateHistory = step;
                // updateHistory.UpdatedAt = DateTime.Now;

                switch (step.Name)
                {
                    case StepHistoryName.ChatMemberUsername:
                        Log.Debug("Verifying Username for UserId {UserId}", fromId);
                        if (telegramService.HasUsername)
                        {
                            stepHistoryDto.Status = StepHistoryStatus.HasVerify;
                        }
                        break;

                    case StepHistoryName.ChatMemberPhoto:
                        Log.Debug("Verifying User Profile Photo for UserId {UserId}", fromId);
                        if (await userProfilePhotoService.HasUserProfilePhotosAsync(fromId))
                        {
                            stepHistoryDto.Status = StepHistoryStatus.HasVerify;
                        }
                        break;

                    case StepHistoryName.ForceSubscription:
                        var chatMember = await telegramService.ChatService.GetChatMemberAsync(
                            chatId: chatId,
                            userId: fromId,
                            evictAfter: true
                        );

                        if (chatMember.Status != ChatMemberStatus.Left)
                            stepHistoryDto.Status = StepHistoryStatus.HasVerify;

                        break;

                    case StepHistoryName.HumanVerification:
                        stepHistoryDto.Status = StepHistoryStatus.HasVerify;
                        break;

                    default:
                        break;
                }

                await stepHistoriesService.SaveStepHistory(stepHistoryDto);
            }

            var afterVerify = await stepHistoriesService.GetStepHistoryVerifyCore(
                new StepHistoryDto()
                {
                    ChatId = chatId,
                    UserId = fromId
                }
            );

            if (!afterVerify.Any())
            {
                await telegramService.UnmuteChatMemberAsync(fromId);
                answer = "Terima kasih sudah verifikasi!";
            }
            else
            {
                answer = "Silakan lakukan sesuai instruksi, lalu tekan Verifikasi";
            }
        }

        await telegramService.AnswerCallbackQueryAsync(answer);
        return true;
    }

    public static async Task<bool> OnCallbackRssCtlAsync(this TelegramService telegramService)
    {
        var chatId = telegramService.ChatId;
        var chatTitle = telegramService.ChatTitle;
        var messageId = telegramService.CallBackMessageId;
        var answerHeader = $"RSS Control for {chatTitle}";
        var answerDescription = string.Empty;
        var part = telegramService.CallbackQuery.Data?.Split(" ");
        var rssId = part!.ElementAtOrDefault(2);
        var page = 0;
        const int take = 5;

        if (!await telegramService.CheckUserPermission())
        {
            await telegramService.AnswerCallbackQueryAsync("Anda tidak mempunyai akses", true);

            return false;
        }

        var rssService = telegramService.GetRequiredService<RssService>();
        var rssFeedService = telegramService.GetRequiredService<RssFeedService>();
        var messageHistoryService = telegramService.GetRequiredService<MessageHistoryService>();

        var rssFind = new RssSettingFindDto()
        {
            ChatId = chatId
        };

        var updateValue = new Dictionary<string, object>()
        {
            { "updated_at", DateTime.UtcNow }
        };

        switch (part.ElementAtOrDefault(1))
        {
            case "stop-all":
                updateValue.Add("is_enabled", false);
                answerDescription = $"Semua service berhasil dimatikan";
                break;

            case "start-all":
                updateValue.Add("is_enabled", true);
                answerDescription = "Semua service berhasil diaktifkan";
                break;

            case "start":
                rssFind.Id = rssId.ToInt64();
                updateValue.Add("is_enabled", true);
                answerDescription = "Service berhasil di aktifkan";
                break;

            case "stop":
                rssFind.Id = rssId.ToInt64();
                updateValue.Add("is_enabled", false);
                answerDescription = "Service berhasil dimatikan";
                break;

            case "attachment-off":
                rssFind.Id = rssId.ToInt64();
                updateValue.Add("include_attachment", false);
                answerDescription = "Attachment tidak akan ditambahkan";
                break;

            case "attachment-on":
                rssFind.Id = rssId.ToInt64();
                updateValue.Add("include_attachment", true);
                answerDescription = "Berhasil mengaktifkan attachment";
                break;

            case "delete":
                await rssService.DeleteRssAsync(
                    chatId: chatId,
                    columnName: "id",
                    columnValue: rssId
                );
                answerDescription = "Service berhasil dihapus";
                break;

            case "navigate-page":
                var toPage = part.ElementAtOrDefault(2).ToInt();
                page = toPage;
                if (toPage < 0)
                {
                    page = 0;
                    answerDescription = "Halaman 1";
                    await telegramService.AnswerCallbackQueryAsync("Sepertinya ini halaman pertama", true);
                }
                else
                {
                    answerDescription = "Halaman " + (toPage + 1);
                }
                break;
        }

        await rssService.UpdateRssSettingAsync(rssFind, updateValue);

        await rssFeedService.ReRegisterRssFeedByChatId(chatId);

        var answerCombined = answerHeader + Environment.NewLine + answerDescription;

        var btnMarkupCtl = await rssService.GetButtonMarkup(
            chatId: chatId,
            page: page,
            take: take
        );

        if (answerDescription.IsNotNullOrEmpty())
        {
            if (btnMarkupCtl.InlineKeyboard.Any())
                await telegramService.EditMessageCallback(answerCombined, btnMarkupCtl);
            else
                await telegramService.AnswerCallbackQueryAsync("Sepertinya ini halaman terakhir", true);

            if (part.ElementAtOrDefault(1)?.Contains("all") ?? false)
                await telegramService.AnswerCallbackQueryAsync(answerCombined, true);
        }

        await messageHistoryService.UpdateDeleteAtAsync(
            new MessageHistoryFindDto()
            {
                ChatId = chatId,
                MessageId = messageId
            },
            DateTime.UtcNow.AddMinutes(10)
        );

        return true;
    }

    public static async Task<bool> OnCallbackSettingAsync(this TelegramService telegramService)
    {
        var callbackQuery = telegramService.CallbackQuery;
        var chatId = callbackQuery.Message.Chat.Id;
        var fromId = callbackQuery.From.Id;
        var msgId = callbackQuery.Message.MessageId;

        if (!await telegramService.CheckUserPermission())
        {
            Log.Information(
                "UserId: {UserId} don't have permission at {ChatId}",
                fromId,
                chatId
            );
            return false;
        }

        Log.Information("Processing Setting Callback");
        var settingsService = telegramService.GetRequiredService<SettingsService>();

        var callbackData = callbackQuery.Data;
        var partedData = callbackData.Split(" ");
        var callbackParam = partedData.ValueOfIndex(1);
        var partedParam = callbackParam.Split("_");
        var valueParamStr = partedParam.ValueOfIndex(0);
        var keyParamStr = callbackParam.Replace(valueParamStr, "");
        var currentVal = valueParamStr.ToBoolInt();

        Log.Information("Param : {KeyParamStr}", keyParamStr);
        Log.Information("CurrentVal : {CurrentVal}", currentVal);

        var columnTarget = "enable" + keyParamStr;
        var newValue = currentVal == 0 ? 1 : 0;

        Log.Information(
            "Column: {ColumnTarget}, Value: {CurrentVal}, NewValue: {NewValue}",
            columnTarget,
            currentVal,
            newValue
        );

        var data = new Dictionary<string, object>()
        {
            ["chat_id"] = chatId,
            [columnTarget] = newValue
        };

        await settingsService.SaveSettingsAsync(data);

        var settingBtn = await settingsService.GetSettingButtonByGroup(chatId);
        var btnMarkup = await settingBtn.ToJson().JsonToButton(chunk: 2);
        Log.Debug("Settings: {Count}", settingBtn.Count);

        telegramService.SentMessageId = msgId;

        var editText = $"Settings Toggles" +
                       $"\nParam: {columnTarget} to {newValue}";
        await telegramService.EditMessageCallback(editText, btnMarkup);

        await settingsService.UpdateCacheAsync(chatId);

        return true;
    }

    public static async Task<bool> OnCallbackGlobalBanAsync(this TelegramService telegramService)
    {
        var chatId = telegramService.ChatId;
        var fromId = telegramService.FromId;
        var chatUsername = telegramService.Chat.Username;
        var message = telegramService.CallbackMessage;
        var callbackDatas = telegramService.CallbackQueryDatas;

        if (!await telegramService.CheckFromAdminOrAnonymous())
        {
            Log.Debug(
                "UserId: {UserId} is not admin at ChatId: {ChatId}",
                fromId,
                chatId
            );

            return false;
        }

        var globalBanService = telegramService.GetRequiredService<GlobalBanService>();
        var eventLogService = telegramService.GetRequiredService<EventLogService>();

        var action = telegramService.GetCallbackDataAt<string>(1);
        var userId = telegramService.GetCallbackDataAt<long>(2);

        var replyToMessageId = telegramService.ReplyToMessage?.MessageId ?? -1;

        var answerCallback = string.Empty;

        var messageLog = HtmlMessage.Empty
            .TextBr("Global Ban di tambahkan baru")
            .Bold("Ban By: ").CodeBr(fromId.ToString())
            .Bold("UserId: ").CodeBr(userId.ToString());

        switch (action)
        {
            case "add":
                await globalBanService.SaveBanAsync(
                    new GlobalBanItem
                    {
                        UserId = userId,
                        ReasonBan = "@" + chatUsername,
                        BannedBy = fromId,
                        BannedFrom = chatId,
                        CreatedAt = DateTime.UtcNow
                    }
                );

                await globalBanService.UpdateCache(userId);

                await telegramService.KickMemberAsync(userId, untilDate: DateTime.Now.AddSeconds(30));

                await eventLogService.SendEventLogAsync(
                    chatId: chatId,
                    message: message,
                    text: messageLog.ToString(),
                    forwardMessageId: replyToMessageId,
                    deleteForwardedMessage: true,
                    messageFlag: MessageFlag.GBan
                );

                telegramService.DeleteMessageManyAsync(customUserId: userId).InBackground();

                answerCallback = "Berhasil Memblokir Pengguna!";

                break;

            case "del":
                await globalBanService.DeleteBanAsync(userId);
                await globalBanService.UpdateCache(userId);

                answerCallback = "Terima kasih atas konfirmasinya!";

                break;
        }

        await telegramService.AnswerCallbackQueryAsync(answerCallback, true);
        await telegramService.DeleteCurrentCallbackMessageAsync();

        return true;
    }

    public static async Task<bool> OnCallbackPinMessageAsync(this TelegramService telegramService)
    {
        var chatId = telegramService.ChatId;

        if (!await telegramService.CheckFromAdminOrAnonymous())
        {
            await telegramService.AnswerCallbackQueryAsync("Kamu tidak memiliki izin untuk melakukan tidakan!", true);

            return true;
        }

        var messageId = telegramService.GetCallbackDataAt<int>(1);
        var pinMode = telegramService.GetCallbackDataAt<string>(2);
        var disableNotification = pinMode == "silent";

        var client = telegramService.Client;

        await client.UnpinChatMessageAsync(chatId, messageId);
        await client.PinChatMessageAsync(
            chatId: chatId,
            messageId: messageId,
            disableNotification: disableNotification
        );

        await telegramService.DeleteCurrentCallbackMessageAsync();

        return true;
    }

    public static async Task<bool> OnCallbackForceSubAsync(this TelegramService telegramService)
    {
        var chatId = telegramService.ChatId;
        Log.Information("Receiving Ping callback");

        if (!await telegramService.CheckFromAdminOrAnonymous())
        {
            await telegramService.AnswerCallbackQueryAsync("Kamu tidak memiliki izin untuk melakukan tidakan!", true);

            return true;
        }

        var fSubsService = telegramService.GetRequiredService<ForceSubsService>();

        var callbackAction = telegramService.GetCallbackDataAt<string>(1);
        var channelId = telegramService.GetCallbackDataAt<long>(2);

        await fSubsService.DeleteSubsAsync(chatId, channelId);

        await telegramService.EditMessageCallback("Channel berhasil dihapus dari daftar subs!");

        return true;
    }
}