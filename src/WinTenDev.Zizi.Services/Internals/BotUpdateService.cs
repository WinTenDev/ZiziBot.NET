﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using MongoDB.Entities;
using MySqlConnector;
using RepoDb;

namespace WinTenDev.Zizi.Services.Internals;

public class BotUpdateService
{
    private readonly ILogger<BotUpdateService> _logger;
    private readonly QueryService _queryService;

    public MySqlConnection DbConnection => _queryService.CreateMysqlConnectionCore();

    public BotUpdateService(
        ILogger<BotUpdateService> logger,
        QueryService queryService
    )
    {
        _logger = logger;
        _queryService = queryService;
    }

    public async Task<bool> IsBotUpdateExistAsync(
        long chatId,
        long userId
    )
    {
        var isExist = await DbConnection.ExistsAsync<BotUpdate>(
            update =>
                update.ChatId == chatId &&
                update.UserId == userId,
            trace: new DefaultTraceLog()
        );

        _logger.LogInformation(
            "Is Update for UserId: {UserId} on ChatId: {ChatId}  Exist? {IsExist}",
            userId,
            chatId,
            isExist
        );

        return isExist;
    }

    public async Task SaveUpdateAsync(BotUpdate botUpdate)
    {
        await DbConnection.InsertAsync(botUpdate, trace: new DefaultTraceLog());
    }

    public async Task SaveUpdateAsync(BotUpdateEntity botUpdateEntity)
    {
        await botUpdateEntity.InsertAsync();
    }

    public async Task<IEnumerable<BotUpdate>> GetUpdateAsync()
    {
        return await DbConnection.QueryAllAsync<BotUpdate>(trace: new DefaultTraceLog());
    }

    public async Task<List<BotUpdate>> GetUpdateAsync(
        long chatId,
        long userId
    )
    {
        var botUpdates = await DbConnection.QueryAsync<BotUpdate>(
            where: update =>
                update.ChatId == chatId &&
                update.UserId == userId,
            trace: new DefaultTraceLog()
        );

        return botUpdates.ToList();
    }

    [JobDisplayName("Delete Old Updates")]
    public async Task DeleteOldUpdateAsync()
    {
        var oldOffset = DateTime.UtcNow.AddMonths(-2);
        _logger.LogInformation("Deleting old updates. Older than offset: {OldOffset}", oldOffset);
        var delete = await DbConnection.DeleteAsync<BotUpdate>(
            where: update =>
                update.CreatedAt <= oldOffset,
            trace: new DefaultTraceLog()
        );

        _logger.LogInformation("Deleted old Updates. Total {Delete} item(s)", delete);

        var secondOldOffset = DateTime.UtcNow.AddDays(-14);
        var secondOldOffsetStr = secondOldOffset.ToString("yyyy-MM-dd");
        _logger.LogInformation("Deleting old updates. Older than second offset: {OldOffset}", oldOffset);

        var sql =
            "delete from bot_update " +
            "where ( " +
            "`update` not like '%@%' " +
            "or `update` not like '%http%' " +
            "or user_id in (0, 777000)) " +
                  $"and created_at < '{secondOldOffsetStr}' " +
            "order by created_at desc;";

        var deleteSecond = await DbConnection.ExecuteNonQueryAsync(sql);

        _logger.LogInformation("Deleted old Updates. Total {DeleteSecond} item(s)", deleteSecond);
    }
}