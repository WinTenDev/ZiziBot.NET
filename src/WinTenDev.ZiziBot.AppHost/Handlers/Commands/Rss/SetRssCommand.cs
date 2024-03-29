﻿using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Rss;

public class SetRssCommand : CommandBase
{
    private readonly RssService _rssService;
    private readonly RssFeedService _rssFeedService;
    private readonly TelegramService _telegramService;

    public SetRssCommand(
        TelegramService telegramService,
        RssService rssService,
        RssFeedService rssFeedService
    )
    {
        _telegramService = telegramService;
        _rssService = rssService;
        _rssFeedService = rssFeedService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);

        _telegramService.AddRssUrlAsync().InBackground();
    }
}
