using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Externals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Text;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Additional;

public class CheckResiCommand : CommandBase
{
    private readonly TelegramService _telegramService;
    private readonly CekResiService _cekResiService;
    private readonly FeatureService _featureService;

    public CheckResiCommand(
        CekResiService cekResiService,
        FeatureService featureService,
        TelegramService telegramService
    )
    {
        _cekResiService = cekResiService;
        _featureService = featureService;
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);

        var featureConfig = await _telegramService.GetFeatureConfig();

        if (!featureConfig.NextHandler) return;

        if (!_telegramService.IsFromSudo)
        {
            await _telegramService.SendTextMessageAsync("Sabar ya ngab, fitur sedang di perbaiki");
            return;
        }

        var resi = _telegramService.MessageTextParts.ElementAtOrDefault(1);

        if (resi.IsNullOrEmpty())
        {
            await _telegramService.SendTextMessageAsync("⚠ Silakan sertakan nomor resi yang mau di cek.");
            return;
        }

        await _telegramService.SendTextMessageAsync("🔍 Sedang mencari nomor Resi");

        BinderByteCekResi(resi).InBackground();
    }

    private async Task BinderByteCekResi(string resi)
    {
        var result = await _cekResiService.BinderByteCekResiBatchesAsync(resi);

        await _telegramService.EditMessageTextAsync(result);
    }

    private async Task ParsePigooraCheckResi(string resi)
    {
        await _telegramService.EditMessageTextAsync("🔍 Sedang memeriksa nomor Resi");
        var runCekResi = await _cekResiService.CekResi(resi);
        Log.Debug("Check Results: {0}", runCekResi.ToJson(true));

        if (runCekResi.Result == null)
        {
            await _telegramService.EditMessageTextAsync
            (
                $"Sepertinya resi tidak ditemukan, silakan periksa kembali. " +
                $"\nNo Resi: <code>{resi}</code>"
            );
            return;
        }

        var result = runCekResi.Result;
        var summary = result.Summary;
        var manifests = result.Manifest;

        var manifestStr = new StringBuilder();

        foreach (var manifest in manifests)
        {
            manifestStr.AppendLine($"⏰ {manifest.ManifestDate:dd MMM yyyy} {manifest.ManifestTime:HH:mm zzz}");
            manifestStr.AppendLine($"└ {manifest.ManifestCode} - {manifest.ManifestDescription}");
            manifestStr.AppendLine();
        }

        var send =
            $"Status: {summary.Status}" +
            $"\nKurir: {summary.CourierCode} - {summary.CourierName} {summary.ServiceCode}" +
            $"\nNoResi: {summary.WaybillNumber}" +
            $"\nPengirim: {summary.ShipperName}" +
            $"\nOrigin: {summary.Origin}" +
            $"\nPenerima: {summary.ReceiverName}" +
            $"\nTujuan: {summary.Destination}" +
            $"\n\nDetail" +
            $"\n{manifestStr.ToString().Trim()}";

        await _telegramService.EditMessageTextAsync(send);
    }
}