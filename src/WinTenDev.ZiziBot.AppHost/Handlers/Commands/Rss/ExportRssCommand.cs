using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Framework.Abstractions;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Rss;

public class ExportRssCommand : CommandBase
{
    private readonly RssService _rssService;
    private readonly TelegramService _telegramService;

    public ExportRssCommand(
        TelegramService telegramService,
        RssService rssService
    )
    {
        _telegramService = telegramService;
        _rssService = rssService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);

        var msg = _telegramService.Message;
        var chatId = msg.Chat.Id;
        var msgId = msg.MessageId;
        var msgText = msg.Text;
        var dateDate = DateTime.UtcNow.ToString("yyyy-MM-dd", new DateTimeFormatInfo());

        var checkUserPermission = await _telegramService.CheckUserPermission();
        if (!checkUserPermission)
        {
            var send = "Maaf, hanya Admin yang dapat mengekspor daftar RSS";
            await _telegramService.SendTextMessageAsync(send);
            return;
        }

        var rssSettings = await _rssService.GetRssSettingsAsync(chatId);

        Log.Information("RssSettings: {V}", rssSettings.ToJson(true));

        var listRss = new StringBuilder();
        foreach (var rss in rssSettings)
        {
            listRss.AppendLine(rss.UrlFeed);
        }

        Log.Debug("ListUrl: \n{ListRss}", listRss);

        var listRssStr = listRss.ToString().Trim();
        var sendText = "Daftar RSS ini tidak terenkripsi, dapat di pulihkan di obrolan mana saja. " +
                       "Tambahkan parameter -e agar daftar RSS terenkripsi.";

        if (msgText.Contains("-e", StringComparison.CurrentCulture))
        {
            Log.Information("List RSS will be encrypted.");
            listRssStr = listRssStr.AesEncrypt();
            sendText = "Daftar RSS ini terenkripsi, hanya dapat di pulihkan di obrolan ini!";
        }

        var filePath = $"{chatId}/rss-feed_{dateDate}_{msgId}.txt";
        await listRssStr.WriteTextAsync(filePath);

        var fileSend = Path.Combine("Storage", "Caches") + $"/{filePath}";
        await _telegramService.SendMediaAsync(
            fileSend,
            MediaType.LocalDocument,
            sendText
        );

        fileSend.DeleteFile();
    }
}
