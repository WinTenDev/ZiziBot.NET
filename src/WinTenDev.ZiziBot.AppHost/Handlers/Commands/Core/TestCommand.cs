using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flurl;
using MoreLinq;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Framework.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Core;

public class TestCommand : CommandBase
{
    private readonly TelegramService _telegramService;
    private readonly EpicGamesService _epicGamesService;
    private readonly SettingsService _settingsService;
    private readonly DeepAiService _deepAiService;
    private readonly BlockListService _blockListService;
    private readonly WTelegramApiService _wTelegramApiService;

    public TestCommand(
        EpicGamesService epicGamesService,
        TelegramService telegramService,
        DeepAiService deepAiService,
        SettingsService settingsService,
        BlockListService blockListService,
        WTelegramApiService wTelegramApiService
    )
    {
        _telegramService = telegramService;
        _deepAiService = deepAiService;
        _epicGamesService = epicGamesService;
        _settingsService = settingsService;
        _blockListService = blockListService;
        _wTelegramApiService = wTelegramApiService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);

        var param1 = _telegramService.MessageTextParts.ValueOfIndex(1);
        var param2 = _telegramService.MessageTextParts.ValueOfIndex(2);

        var chatId = _telegramService.ChatId;
        var chatType = _telegramService.Message.Chat.Type;
        var fromId = _telegramService.FromId;
        var msg = _telegramService.Message;
        var msgId = msg.MessageId;

        if (!_telegramService.IsFromSudo)
        {
            Log.Warning("Test only for Sudo!");
            return;
        }

        Log.Information("Test started..");
        await _telegramService.AppendTextAsync("Sedang mengetes sesuatu");

        if (param1.IsNullOrEmpty())
        {
            await _telegramService.AppendTextAsync("No Test Param");
            return;
        }

        await _telegramService.AppendTextAsync($"Flags: {param1}");

        switch (param1)
        {
            case "ex-keyboard":
                await ExtractReplyMarkup();
                break;

            // case "mk-remove-all":
            //     MonkeyCacheRemoveAll();
            //     break;

            // case "mk-remove-expires":
            //     MonkeyCacheRemoveExpires();
            //     break;

            // case "mk-view-all":
            //     MonkeyCacheViewAll();
            //     break;

            // case "mk-save-current":
            //     MonkeyCacheSaveCurrent();
            //     break;

            case "ml-nlp":
                MachineLearningProcessNlp();
                break;

            case "ml-predict":
                await MachineLearningPredict();
                break;

            case "nsfw-detect":
                await NsfwDetect();
                break;

            case "sysinfo":
                await GetSysInfo();
                break;

            case "uniqid-gen":
                var id = StringUtil.GenerateUniqueId(param2.ToInt());
                await _telegramService.AppendTextAsync($"UniqueID: {id}");
                break;

            case "wh-check":
                await WebhookCheck();
                break;

            case "dl-gen":
                var directLink = await DirectLinkParser(param2);
                await _telegramService.AppendTextAsync($"DL: {directLink}");
                break;

            case "parse-bl":
                var url = "https://raw.githubusercontent.com/mhhakim/pihole-blocklist/master/porn.txt";
                var listUrl = await _blockListService.ParseList(url);
                var msgText = $"{listUrl.Name}" +
                              $"\n{listUrl.Source}" +
                              $"\n{listUrl.LastUpdate}" +
                              $"\n{listUrl.DomainCount}";

                await _telegramService.AppendTextAsync($"{msgText}");
                break;

            default:
                await _telegramService.AppendTextAsync($"Feature '{param1}' is not available.");
                Log.Warning("Feature '{0}' is not available", param1);

                break;
        }

        await _telegramService.AppendTextAsync("Complete");
    }

    private async Task ExtractReplyMarkup()
    {
        var repMsg = _telegramService.MessageOrEdited.ReplyToMessage;

        if (repMsg == null) return;

        var replyMarkups = repMsg.ReplyMarkup.InlineKeyboard.ToList();
        Log.Debug("ReplyMarkup: {0}", replyMarkups.ToJson(true));

        var flattened = replyMarkups.Flatten();
        var sb = new StringBuilder();
        foreach (var replyMarkup in replyMarkups)
        {
            foreach (var keyboardButton in replyMarkup)
            {
                var payload = keyboardButton.Url ?? keyboardButton.CallbackData;
                sb.Append($"{keyboardButton.Text}|{payload}");
            }
        }

        await _telegramService.AppendTextAsync($"RawBtn: {sb}");
    }

    private async Task WebhookCheck()
    {
        var client = await _telegramService.Client.GetWebhookInfoAsync();
        var pendingCount = client.PendingUpdateCount - 1;
        await _telegramService.AppendTextAsync($"PendingCount: {pendingCount}");

        await _telegramService.NotifyPendingCount();
    }

    private void MachineLearningProcessNlp()
    {
        NlpUtil.TrainModel();
    }

    private async Task MachineLearningPredict()
    {
        var msg = _telegramService.Message;
        var repMsg = msg.ReplyToMessage;
        var text = repMsg.Text;

        var predicts = NlpUtil.Predict(text);
        var sb = new StringBuilder();
        foreach (var predict in predicts)
        {
            sb.AppendLine(predict);
        }

        await _telegramService.AppendTextAsync($"Result: {sb.ToString()}");
    }

    private async Task GetSysInfo()
    {
        var send = "SysInfo";

        await _telegramService.EditMessageTextAsync(send);
    }

    private async Task NsfwDetect()
    {
        var img2 = "https://api.deepai.org/job-view-file/8756b883-a087-42a9-a73c-205292ea0336/inputs/image.jpg";
        var img3 = "D:/Personal/Documents/Private/image.jpg";

        var result = await _deepAiService.NsfwDetectCoreAsync(img2);
        var output = result.Output;

        var text = $"NSFW Score: {output.NsfwScore}";

        await _telegramService.AppendTextAsync(text);
    }

    private async Task<string> DirectLinkParser(string url)
    {
        var directLink = await url.ParseZippyShare();

        return directLink;
    }
}
