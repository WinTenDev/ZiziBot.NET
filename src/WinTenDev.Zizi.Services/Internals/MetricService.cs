﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Serilog;
using SqlKata.Execution;

namespace WinTenDev.Zizi.Services.Internals;

public class MetricService
{
    private readonly QueryService _queryService;
    private readonly CacheService _cacheService;

    public MetricService(
        QueryService queryService,
        CacheService cacheService
    )
    {
        _queryService = queryService;
        _cacheService = cacheService;
    }

    public async Task HitActivityAsync(HitActivity hitActivity)
    {
        Log.Information("Starting Hit Activity");

        // var message = telegramService.MessageOrEdited;
        // var botUser = await telegramService.GetBotMeAsync();
        //
        // var data = new Dictionary<string, object>()
        // {
        //     {"via_bot", botUser.Username},
        //     {"message_type", message.Type.ToString()},
        //     {"from_id", message.From.Id},
        //     {"from_first_name", message.From.FirstName},
        //     {"from_last_name", message.From.LastName},
        //     {"from_username", message.From.Username},
        //     {"from_lang_code", message.From.LanguageCode},
        //     {"chat_id", message.Chat.Id},
        //     {"chat_username", message.Chat.Username},
        //     {"chat_type", message.Chat.Type.ToString()},
        //     {"chat_title", message.Chat.Title},
        // };
        //
        // var insertHit = await new Query("hit_activity")
        //     .ExecForMysql(true)
        //     .InsertAsync(data);
        //
        // Log.Information("Insert Hit: {InsertHit}", insertHit);

        var path = "Storage/Caches/hit-buffer.csv".EnsureDirectory();
        // var hitActivity = new HitActivity()
        // {
        //     ViaBot = botUser.Username,
        //     MessageType = message.Type.ToString(),
        //     FromId = message.From.Id,
        //     FromFirstName = message.From.FirstName,
        //     FromLastName = message.From.LastName,
        //     FromUsername = message.From.Username,
        //     FromLangCode = message.From.LanguageCode,
        //     ChatId = message.Chat.Id.ToString(),
        //     ChatUsername = message.Chat.Username,
        //     ChatType = message.Chat.Type.ToString(),
        //     ChatTitle = message.Chat.Title,
        //     Timestamp = DateTime.Now
        // };

        var insertBuffer = hitActivity.AppendRecord(path);
        Log.Debug("Buffer Hit activity saved to {InsertBuffer}", insertBuffer);
    }

    public void HitActivityBackground()
    {
        BackgroundJob.Enqueue(() => HitActivityAsync(new HitActivity()));

        Log.Information("Hit Activity scheduled in Background");
    }

    public async Task FlushHitActivity()
    {
        Log.Information("Flushing HitActivity buffer");
        var metrics = LiteDbProvider.GetCollections<HitActivity>();

        var dateFormat = "yyyy-MM-dd HH";
        var dateFormatted = DateTime.Now.ToString(dateFormat);
        Log.Debug("Filter last hour: {DateFormatted}", dateFormatted);
        var filteredMetrics = metrics.Find(
            x =>
                x.Timestamp.ToString(dateFormat) == dateFormatted
        ).ToList();

        if (filteredMetrics.Count == 0)
        {
            Log.Debug("No HitActivity buffed need to flush");
            return;
        }

        Log.Debug(
            "Flushing {Count} of {CountAll} data..",
            filteredMetrics.Count,
            metrics.Count()
        );
        foreach (var hitActivity in filteredMetrics)
        {
            var data = new Dictionary<string, object>()
            {
                { "via_bot", hitActivity.ViaBot },
                { "update_type", hitActivity.UpdateType },
                { "from_id", hitActivity.FromId },
                { "from_first_name", hitActivity.FromFirstName },
                { "from_last_name", hitActivity.FromLastName },
                { "from_username", hitActivity.FromUsername },
                { "from_lang_code", hitActivity.FromLangCode },
                { "chat_id", hitActivity.ChatId },
                { "chat_username", hitActivity.ChatUsername },
                { "chat_type", hitActivity.ChatType },
                { "chat_title", hitActivity.ChatTitle },
                { "timestamp", hitActivity.Timestamp }
            };

            var insertHit = await _queryService
                .CreateMySqlFactory()
                .FromTable("hit_activity")
                .InsertAsync(data);

            Log.Information("Insert Hit: {InsertHit}", insertHit);
        }

        Log.Debug("Clearing local data..");
        filteredMetrics.ForEach(
            x => {
                metrics.DeleteMany(y => y.Timestamp == x.Timestamp);
            }
        );

        LiteDbProvider.Rebuild();

        Log.Information("Flush HitActivity done");
    }

    // public static async Task GetStat()
    // {
    //     var chatId = telegramService.Message.Chat.Id;
    //     var statBuilder = new StringBuilder();
    //     var monthStr = DateTime.Now.ToString("yyyy-MM");
    //     statBuilder.AppendLine($"Stat Group: {chatId}");
    //
    //     await telegramService.SendTextMessageAsync(statBuilder.ToString().Trim());
    //
    //     var monthCount = await GetMonthlyStat(telegramService);
    //     var monthRates = monthCount / 30;
    //     statBuilder.AppendLine($"This Month: {monthCount}");
    //     statBuilder.AppendLine($"Traffics: {monthRates} msg/day");
    //     await telegramService.EditMessageTextAsync(statBuilder.ToString().Trim());
    //
    //     statBuilder.AppendLine();
    //
    //     var todayCount = await GetDailyStat(telegramService);
    //     var todayRates = todayCount / 24;
    //     statBuilder.AppendLine($"Today: {todayCount}");
    //     statBuilder.AppendLine($"Traffics: {todayRates} msg/hour");
    //     await telegramService.EditMessageTextAsync(statBuilder.ToString().Trim());
    // }

    // private static async Task<int> GetMonthlyStat(this TelegramService telegramService)
    // {
    //     var chatId = telegramService.Message.Chat.Id;
    //     var statBuilder = new StringBuilder();
    //     var monthStr = DateTime.Now.ToString("yyyy-MM");
    //     // statBuilder.AppendLine($"Stat Group: {chatId}");
    //
    //     var monthActivity = (await new Query("hit_activity")
    //         .ExecForMysql(true)
    //         .WhereRaw($"str_to_date(timestamp,'%Y-%m-%d') like '{monthStr}%'")
    //         .Where("chat_id", chatId)
    //         .GetAsync()
    //         ).ToList();
    //     var monthCount = monthActivity.Count;
    //     return monthCount;
    //     // statBuilder.AppendLine($"This Month: {monthCount}");
    //     // await telegramService.EditMessageTextAsync(statBuilder.ToString());
    // }

    // private static async Task<int> GetDailyStat(this TelegramService telegramService)
    // {
    //     var chatId = telegramService.Message.Chat.Id;
    //     // var statBuilder = new StringBuilder();
    //     var todayStr = DateTime.Today.ToString("yyyy-MM-dd");
    //
    //     var todayActivity = (await new Query("hit_activity")
    //         .ExecForMysql(true)
    //         .WhereDate("timestamp", todayStr)
    //         .Where("chat_id", chatId)
    //         .GetAsync()
    //         ).ToList();
    //
    //     var todayCount = todayActivity.Count;
    //     return todayCount;
    //
    //     // statBuilder.AppendLine($"Today: {todayCount}");
    //     // await telegramService.EditMessageTextAsync(statBuilder.ToString());
    // }
}