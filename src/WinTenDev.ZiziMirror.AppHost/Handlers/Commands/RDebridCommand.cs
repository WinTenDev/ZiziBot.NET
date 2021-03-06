using System.Linq;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Html.Dom;
using Telegram.Bot.Framework.Abstractions;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Services.Externals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.ZiziMirror.AppHost.Handlers.Commands
{
    public class RDebridCommand : CommandBase
    {
        private TelegramService _telegramService;
        private AppConfig _appConfig;
        private readonly HeirroService _heirroService;

        public RDebridCommand(
            AppConfig appConfig,
            HeirroService heirroService
        )
        {
            _appConfig = appConfig;
            _heirroService = heirroService;
        }

        public override async Task HandleAsync(IUpdateContext context, UpdateDelegate next, string[] args)
        {
            await _telegramService.AddUpdateContext(context);
            var from = _telegramService.Message.From;

            var url = _telegramService.MessageTextParts.ValueOfIndex(1);

            if (url.IsNullOrEmpty())
            {
                await _telegramService.SendTextMessageAsync("Sertakan Url yg akan di konversi");
                return;
            }

            if (!url.CheckUrlValid())
            {
                await _telegramService.SendTextMessageAsync("Masukan URL yang valid");
                return;
            }

            await _telegramService.SendTextMessageAsync($"🔄 Sedang menjalankan Debrid: " +
                                                        $"\n<b>URL:</b> {url}");
            var flurlResponse = await _heirroService.Debrid(url);
            var content = await flurlResponse.GetStringAsync();

            var config = Configuration.Default;
            var browsingContext = BrowsingContext.New(config);
            var document = await browsingContext.OpenAsync(req => req.Content(content));

            var div = document.QuerySelectorAll("div").FirstOrDefault();
            var divText = div.TextContent;
            var partsDiv = divText.Split(" ");
            var size = (partsDiv.ValueOfIndex(2) + partsDiv.ValueOfIndex(3)).RemoveThisChar("()");


            var aHref = document.QuerySelectorAll("a").OfType<IHtmlAnchorElement>().FirstOrDefault();
            var href = aHref.Href;
            var text = aHref.InnerHtml;

            if (text.IsNullOrEmpty())
            {
                await _telegramService.EditMessageTextAsync("Sepertinya Debrid tidak berhasil. Silakan periksa URL Anda.");
                return;
            }

            var sendText = $"📁 <b>Name:</b> {text}" +
                           $"\n📦 <b>Ukuran:</b> {size}" +
                           $"\n👽 <b>Pengguna:</b> {from}";

            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithUrl("👥 Download", href),
                    InlineKeyboardButton.WithUrl("👥 Source", url),
                    InlineKeyboardButton.WithUrl("❤️ Bergabung", "https://t.me/WinTenChannel")
                }
            });

            await _telegramService.EditMessageTextAsync(sendText, inlineKeyboard);
        }
    }
}