using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using SerilogTimings;
using Telegram.Bot.Types.Enums;

namespace WinTenDev.Zizi.Services.Extensions;

public static class TelegramServiceActivityExtension
{
    public static async Task OnCallbackReceiveAsync(this TelegramService telegramService)
    {
        var callbackQuery = telegramService.CallbackQuery;
        telegramService.CallBackMessageId = callbackQuery.Message.MessageId;

        // var pingCallback = telegramService.GetRequiredService<PingCallback>();
        // var actionCallback = telegramService.GetRequiredService<ActionCallback>();
        // var helpCallback = telegramService.GetRequiredService<HelpCallback>();
        // var rssCtlCallback = telegramService.GetRequiredService<RssCtlCallback>();
        // var settingsCallback = telegramService.GetRequiredService<SettingsCallback>();
        // var verifyCallback = telegramService.GetRequiredService<VerifyCallback>();

        Log.Verbose("CallbackQuery: {Json}", callbackQuery.ToJson(true));

        var partsCallback = callbackQuery.Data.SplitText(" ");
        Log.Debug("Callbacks: {CB}", partsCallback);
        var callbackCmd = partsCallback.ElementAtOrDefault(0);

        var callbackResult = callbackCmd?.ToLower() switch
        {
            "pong" => await telegramService.OnCallbackPingAsync(),
            "gban" => await telegramService.OnCallbackGlobalBanAsync(),
            "verify" => await telegramService.OnCallbackVerifyAsync(),
            "delete-message" => await telegramService.OnCallbackDeleteAsync(),
            "fsub" => await telegramService.OnCallbackForceSubAsync(),
            "pin-message" => await telegramService.OnCallbackPinMessageAsync(),
            "rssctl" => await telegramService.OnCallbackRssCtlAsync(),
            "setting" => await telegramService.OnCallbackSettingAsync(),
            _ => false
        };

        if (callbackResult) return;

        // switch (partsCallback.ElementAtOrDefault(0))// Level 0
        // {
        //     case "pong":
        //     case "PONG":
        //         var pingResult = await pingCallback.ExecuteAsync();
        //         Log.Information("PingResult: {0}", pingResult.ToJson(true));
        //         break;
        //
        //     case "action":
        //         var actionResult = await actionCallback.ExecuteAsync();
        //         Log.Information("ActionResult: {V}", actionResult.ToJson(true));
        //         break;
        //
        //     case "help":
        //         var helpResult = await helpCallback.ExecuteAsync();
        //         Log.Information("HelpResult: {V}", helpResult.ToJson(true));
        //         break;
        //
        //     case "rssctl":
        //         var result = await rssCtlCallback.ExecuteAsync();
        //         break;
        //
        //     case "setting":
        //         var settingResult = await settingsCallback.ExecuteToggleAsync();
        //         Log.Information("SettingsResult: {V}", settingResult.ToJson(true));
        //         break;
        //
        //     case "verify":
        //         var verifyResult = await verifyCallback.ExecuteVerifyAsync();
        //         Log.Information("VerifyResult: {V}", verifyResult.ToJson(true));
        //         break;
        // }
    }

    public static async Task<bool> OnUpdatePreTaskAsync(this TelegramService telegramService)
    {
        var op = Operation.Begin("Run PreTask for ChatId: {ChatId}", telegramService.ChatId);

        if (telegramService.IsUpdateTooOld()) return false;

        var hasRestricted = await telegramService.CheckChatRestriction();

        if (hasRestricted)
        {
            return false;
        }

        var floodCheck = await telegramService.FloodCheckAsync();
        if (floodCheck.IsFlood)
            return false;

        var featureConfig = await telegramService.GetFeatureConfig();
        if (!featureConfig.NextHandler)
        {
            return false;
        }

        await telegramService.FireAnalyzer();

        var checkUpdateHistory = await telegramService.CheckUpdateHistoryAsync();

        if (checkUpdateHistory)
        {
            return false;
        }

        if (!await telegramService.AnswerChatJoinRequestAsync()) return false;

        var checkAntiSpamResult = await telegramService.AntiSpamCheckAsync();

        if (checkAntiSpamResult.IsAnyBanned)
        {
            return false;
        }

        var checkScanMessageTask = telegramService.ScanMessageAsync();
        var userUsernameTask = telegramService.RunCheckUserUsername();
        var checkUserProfilePhotoTask = telegramService.RunCheckUserProfilePhoto();

        await Task.WhenAll(
            checkScanMessageTask,
            userUsernameTask,
            checkUserProfilePhotoTask
        );

        if (!await userUsernameTask)
        {
            return false;
        }

        if (!await checkUserProfilePhotoTask)
        {
            return false;
        }

        if (await checkScanMessageTask)
        {
            return false;
        }

        op.Complete();

        return true;
    }

    public static Task OnUpdatePostTaskAsync(this TelegramService telegramService)
    {
        var op = Operation.Begin("Run PostTask");

        var nonAwaitTasks = new List<Task>
        {
            telegramService.EnsureChatSettingsAsync(),
            telegramService.AfkCheckAsync(),
            telegramService.CheckNameChangesAsync(),
            telegramService.EnsureForceSubscriptionAsync(),
            telegramService.EnsureChatAdminAsync(),
            telegramService.EnsureReplyNotificationAsync(),
            telegramService.BotSlowdownNotification(),
            telegramService.RunSpellingAsync(),
            telegramService.SaveUpdateAsync(),
            telegramService.WarningCompressImageWhenPossibleAsync()
        };

        nonAwaitTasks.InBackgroundAll();

        op.Complete();

        return Task.CompletedTask;
    }

    public static async Task AfkCheckAsync(this TelegramService telegramService)
    {
        var operation = Operation.Begin("AFK Check");

        var chatId = telegramService.ChatId;
        var fromId = telegramService.FromId;
        var chatSetting = await telegramService.GetChatSetting();

        try
        {
            Log.Information("Starting check AFK");

            var message = telegramService.MessageOrEdited;

            if (!chatSetting.EnableAfkStatus)
            {
                Log.Information("Afk Stat is disabled in this Group!");
                return;
            }

            if (telegramService.MessageOrEdited == null) return;

            if (message.Text != null &&
                message.Text.StartsWith("/afk")) return;

            if (message.ReplyToMessage != null)
            {
                var repMsg = message.ReplyToMessage;
                var repFromId = repMsg.From.Id;

                var isAfkReply = await telegramService.AfkService.GetAfkById(repFromId);

                if (isAfkReply?.IsAfk ?? false)
                {
                    var repNameLink = repMsg.GetFromNameLink();
                    await telegramService.SendTextMessageAsync(
                        sendText: $"{repNameLink} sedang afk",
                        scheduleDeleteAt: DateTime.UtcNow.AddMinutes(5),
                        messageFlag: MessageFlag.Afk
                    );

                    telegramService.ChatService
                        .DeleteMessageHistory(
                            history =>
                                history.MessageFlag == MessageFlag.Afk &&
                                history.ChatId == chatId
                        )
                        .InBackground();
                }
                else
                {
                    Log.Debug("No AFK data for '{FromId}' because never recorded as AFK", repFromId);
                }
            }

            var fromAfk = await telegramService.AfkService.GetAfkById(fromId);

            if (fromAfk == null)
            {
                Log.Debug("No AFK data for '{FromId}' because never recorded as AFK", fromId);
                return;
            }

            if (fromAfk.IsAfk)
            {
                var nameLink = message.GetFromNameLink();

                if (fromAfk.IsAfk) await telegramService.SendTextMessageAsync($"{nameLink} sudah tidak afk");

                var data = new Dictionary<string, object>
                {
                    { "chat_id", chatId },
                    { "user_id", fromId },
                    { "is_afk", 0 },
                    { "afk_reason", "" },
                    { "afk_end", DateTime.Now }
                };

                await telegramService.AfkService.SaveAsync(data);
                await telegramService.AfkService.UpdateAfkByIdCacheAsync(fromId);
            }
        }
        catch (Exception exception)
        {
            Log.Error(
                exception,
                "AFK Check - Error occured on {ChatId}",
                chatId
            );
        }

        operation.Complete();
    }

    public static async Task SaveUpdateAsync(this TelegramService telegramService)
    {
        var chatId = telegramService.ChatId;
        var botUpdateService = telegramService.GetRequiredService<BotUpdateService>();
        var chatSettings = await telegramService.GetChatSetting();

        if (telegramService.InlineQuery != null)
        {
            Log.Debug("Check Bot Admin disabled because Update is '{UpdateType}'", telegramService.Update.Type);
            return;
        }

        if (chatSettings.EnablePrivacyMode)
        {
            Log.Debug("Privacy Mode is enabled for ChatId {ChatId}", chatId);

            return;
        }

        if (telegramService.Chat?.Username == null ||
            telegramService.Chat?.Type == ChatType.Private)
        {
            Log.Debug("Save update only for Public group/channel!");
            return;
        }

        try
        {
            await botUpdateService.SaveUpdateAsync(
                new BotUpdate()
                {
                    BotName = telegramService.BotUsername,
                    ChatId = chatId,
                    UserId = telegramService.FromId,
                    Update = telegramService.Update,
                    CreatedAt = DateTime.UtcNow
                }
            );
        }
        catch (Exception exception)
        {
            Log.Error(
                exception,
                "Save Update - Error occured on {ChatId}",
                telegramService.ChatId
            );
        }
    }
}