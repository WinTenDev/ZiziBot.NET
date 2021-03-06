using Telegram.Bot.Framework;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.ZiziMirror.AppHost.Handlers;
using WinTenDev.ZiziMirror.AppHost.Handlers.Commands;

namespace WinTenDev.ZiziMirror.AppHost.Extensions
{
    public static class CommandBuilderExtension
    {
        public static IBotBuilder ConfigureBot()
        {
            return new BotBuilder()
                .Use<ExceptionHandler>()
                .UseWhen<NewUpdateHandler>(When.NewUpdate)
                .UseWhen(When.NewCommand, builder => builder
                    .UseCommand<AuthorizeCommand>("auth")
                    .UseCommand<UnAuthorizeAllCommand>("unauthall")
                    .UseCommand<UnAuthorizeCommand>("unauth")
                    // .UseCommand<AllDebridCommand>("ad")
                    // .UseCommand<FastDebridCommand>("fd")
                    .UseCommand<RDebridCommand>("rdb")
                    .UseCommand<CloneCommand>("clone")
                    .UseCommand<PingCommand>("ping")
                );
        }
    }
}