﻿using System;
using System.Threading.Tasks;

namespace WinTenDev.Zizi.Services.Extensions;

public static class TelegramServiceDebugExtension
{
    public static async Task GetChatInfo(this TelegramService telegramService)
    {
        var htmlMessage = HtmlMessage.Empty;
        var chatId = telegramService.ChatId;
        var chatService = telegramService.GetRequiredService<ChatService>();
        var chatParam = telegramService.GetCommandParam(0);

        if (!await telegramService.CheckUserPermission())
        {
            await telegramService.DeleteSenderMessageAsync();
            return;
        }

        htmlMessage.BoldBr("ℹ Chat Info");

        try
        {
            if (chatParam != null) chatId = chatParam.ToInt64().FixChatId();
            var chatInfo = await chatService.GetChatAsync(chatId);
            var memberCount = await chatService.GetMemberCountAsync(chatId);

            htmlMessage
                .Bold("Chat Id: ").CodeBr(chatInfo.Id.ToString())
                .Bold("Chat Title: ").CodeBr(chatInfo.Title)
                .Bold("Username: ").CodeBr(chatInfo.Username)
                .Bold("IsPublic: ").CodeBr((chatInfo.Username != null).ToString())
                .Bold("Type: ").CodeBr(chatInfo.Type.ToString())
                .Bold("Linked: ").CodeBr(chatInfo.LinkedChatId.ToString())
                .Bold("Member: ").CodeBr(memberCount.ToString());
        }
        catch (Exception e)
        {
            htmlMessage.Text(e.Message);
        }

        await telegramService.SendTextMessageAsync(
            sendText: htmlMessage.ToString(),
            scheduleDeleteAt: DateTime.UtcNow.AddMinutes(10),
            includeSenderMessage: true
        );
    }
}