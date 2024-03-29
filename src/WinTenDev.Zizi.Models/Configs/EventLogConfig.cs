﻿using BotFramework.Config;

namespace WinTenDev.Zizi.Models.Configs;

public class EventLogConfig
{
    public bool IsEnabled { get; set; }
    public string BotToken { get; set; }
    public long ChannelId { get; set; }
    public string AppName { get; set; }
    public BotConfig BotConfig { get; set; }
    public TgBotConfig TgBotConfig { get; set; }
}
