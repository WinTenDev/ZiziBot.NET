using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Serilog;
using Telegram.Bot.Framework.Abstractions;
using Telegram.Bot.Types.ReplyMarkups;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Additional;

public class AllDebridCommand : CommandBase
{
    private readonly AllDebridConfig _allDebridConfig;
    private readonly TelegramService _telegramService;
    private readonly AllDebridService _allDebridService;

    public AllDebridCommand(
        IOptionsSnapshot<AllDebridConfig> allDebridConfig,
        AllDebridService allDebridService,
        TelegramService telegramService
    )
    {
        _allDebridConfig = allDebridConfig.Value;
        _telegramService = telegramService;
        _allDebridService = allDebridService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);

        var isEnabled = _allDebridConfig.IsEnabled;

        if (!isEnabled)
        {
            Log.Warning("AllDebrid feature is disabled on AppSettings");
            return;
        }

        var txtParts = _telegramService.MessageTextParts;
        var urlParam = txtParts.ValueOfIndex(1);

        if (!_telegramService.IsFromSudo &&
            _telegramService.IsChatRestricted)
        {
            Log.Information("AllDebrid is restricted only to some Chat ID");
            var limitFeature = "Convert link via AllDebrid hanya boleh di grup <b>WinTen Mirror</b>.";
            var groupBtn = new InlineKeyboardMarkup(
                new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithUrl("⬇Ke WinTen Mirror", "https://t.me/WinTenMirror")
                    }
                }
            );

            await _telegramService.SendTextMessageAsync(limitFeature, groupBtn);
            return;
        }

        if (urlParam == null)
        {
            await _telegramService.SendTextMessageAsync("Sertakan url yang akan di Debrid");
            return;
        }

        Log.Information("Converting url: {0}", urlParam);
        await _telegramService.SendTextMessageAsync("Sedang mengkonversi URL via Alldebrid.");

        var result = await _allDebridService.ConvertUrl(
            urlParam,
            s =>
                Log.Debug("Progress: {S}", s)
        );

        if (result.Status != "success")
        {
            var errorMessage = result.DebridError.Message;
            var fail = "Sepertinya Debrid gagal." +
                       $"\nNote: {errorMessage}";

            await _telegramService.EditMessageTextAsync(fail);
            return;
        }

        var urlResult = result.DebridData.Link.AbsoluteUri;
        var fileName = result.DebridData.Filename;
        var fileSize = result.DebridData.Filesize;

        var text = "✅ Debrid berhasil" +
                   $"\n📁 Nama: <code>{fileName}</code>" +
                   $"\n📦 Ukuran: <code>{fileSize.SizeFormat()}</code>";

        var inlineKeyboard = new InlineKeyboardMarkup(
            new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithUrl("⬇️ Download", urlResult)
                }
            }
        );

        await _telegramService.EditMessageTextAsync(text, inlineKeyboard);
    }
}
