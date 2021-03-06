using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Group;

public class NewChatMembersHandler : IUpdateHandler
{
    private readonly TelegramService _telegramService;

    public NewChatMembersHandler(TelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        CancellationToken cancellationToken
    )
    {
        await _telegramService.AddUpdateContext(context);

        _telegramService.SendWelcomeMessageAsync().InBackground();
    }
}
