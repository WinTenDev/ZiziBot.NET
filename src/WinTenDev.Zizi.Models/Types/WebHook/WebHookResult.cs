using System.Text.Json.Serialization;
using WinTenDev.Zizi.Models.Enums;

namespace WinTenDev.Zizi.Models.Types.WebHook;

public class WebHookResult
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public WebhookSource WebhookSource { get; set; }

    public string ParsedMessage { get; set; }

    public string ResponseTime { get; set; }

    public string ExecutionTime { get; set; }
}