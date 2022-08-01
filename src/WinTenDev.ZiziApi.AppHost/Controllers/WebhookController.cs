using Microsoft.AspNetCore.Mvc;

namespace WinTenDev.ZiziApi.AppHost.Controllers;

[Route("[controller]")]
[ApiController]
public class WebhookController : ControllerBase
{
    private readonly WebHookService _webHookService;

    public WebhookController(WebHookService webHookService)
    {
        _webHookService = webHookService;
    }

    [HttpPost]
    [Route("{targetId}")]
    public async Task<IActionResult> Run(
        [FromBody] object content,
        string targetId
    )
    {
        var result = await _webHookService.ProcessingRequest(Request);

        return Ok(result);
    }
}