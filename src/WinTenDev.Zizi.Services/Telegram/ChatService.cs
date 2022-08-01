using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace WinTenDev.Zizi.Services.Telegram;

public class ChatService
{
    private const string AdminCheckerPrefix = "admin-checker";
    private const int PrivateSettingLimit = 365;

    private readonly ILogger<ChatService> _logger;
    private readonly ITelegramBotClient _botClient;
    private readonly BotService _botService;
    private readonly ForceSubsService _forceSubsService;
    private readonly MessageHistoryService _messageHistoryService;
    private readonly SettingsService _settingsService;
    private readonly CacheService _cacheService;
    private readonly RestrictionConfig _restrictionConfig;

    public ChatService(
        ILogger<ChatService> logger,
        CacheService cacheService,
        IOptionsSnapshot<RestrictionConfig> restrictionConfig,
        ITelegramBotClient botClient,
        BotService botService,
        ForceSubsService forceSubsService,
        MessageHistoryService messageHistoryService,
        SettingsService settingsService
    )
    {
        _restrictionConfig = restrictionConfig.Value;
        _logger = logger;
        _botClient = botClient;
        _botService = botService;
        _forceSubsService = forceSubsService;
        _messageHistoryService = messageHistoryService;
        _cacheService = cacheService;
        _settingsService = settingsService;
    }

    public bool IsEnableRestriction()
    {
        var isRestricted = _restrictionConfig.EnableRestriction;
        Log.Debug("Global Restriction IsEnabled: {IsRestricted}", isRestricted);

        return isRestricted;
    }

    public bool CheckChatRestriction(long chatId)
    {
        try
        {
            var isRestricted = false;

            if (!IsEnableRestriction()) return false;

            var restrictArea = _restrictionConfig.RestrictionArea;

            if (restrictArea != null)
            {
                var match = restrictArea.FirstOrDefault(x => x == chatId.ToString());

                if (match == null) isRestricted = true;
            }

            Log.Information(
                "Check Chat restriction result on ChatId: {ChatId}? IsRestricted: {IsRestricted}",
                chatId,
                isRestricted
            );
            return isRestricted;
        }
        catch (Exception exception)
        {
            Log.Error(
                exception,
                "Error when check Chat Restriction on {ChatId}",
                chatId
            );
            return false;
        }
    }

    public bool CheckUserIdIgnored(long userId)
    {
        try
        {
            var ignored = false;
            var ignoredIds = _restrictionConfig.IgnoredIds;
            if (ignoredIds == null) return false;

            var find = ignoredIds.FirstOrDefault(l => l == userId);
            if (find != 0) ignored = true;

            Log.Information(
                "Check UserId {UserId} is ignored at Global Ignore ID? {Ignored}",
                userId,
                ignored
            );

            return ignored;
        }
        catch (Exception exception)
        {
            Log.Error(
                exception,
                "Error when check Ignore for UserId {UserId}",
                userId
            );
            return false;
        }
    }

    public async Task<Chat> GetChatAsync(
        ChatId chatId,
        bool evictBefore = false
    )
    {
        var cacheKey = "chat_" + chatId;

        var data = await _cacheService.GetOrSetAsync(
            cacheKey: cacheKey,
            evictBefore: evictBefore,
            action: async () => {
                var chat = await _botClient.GetChatAsync(chatId);

                return chat;
            }
        );

        return data;
    }

    public async Task<long> GetMemberCountAsync(long chatId)
    {
        var reducedChatId = chatId.ReduceChatId();
        var cacheKey = $"member-count_{reducedChatId}";

        var getMemberCount = await _cacheService.GetOrSetAsync(
            cacheKey,
            async () => {
                var memberCount = await _botClient.GetChatMemberCountAsync(chatId);

                return memberCount;
            }
        );

        return getMemberCount;
    }

    public async Task<ChatMember> GetChatMemberAsync(
        ChatId chatId,
        long userId,
        bool evictBefore = false,
        bool evictAfter = false
    )
    {
        var cacheKey = "chat-member_" + chatId + $"_{userId}";

        var data = await _cacheService.GetOrSetAsync(
            cacheKey: cacheKey,
            evictBefore: evictBefore,
            evictAfter: evictAfter,
            action: async () => {
                var chat = await _botClient.GetChatMemberAsync(chatId, userId);

                return chat;
            }
        );

        return data;
    }

    public async Task<List<ChannelSubscriptionResult>> CheckChatMemberSubscriptionToAllAsync(
        long chatId,
        long userId
    )
    {
        var subscriptionResults = new List<ChannelSubscriptionResult>();

        var subscriptionIntoLinkedChannel = await CheckChatMemberSubscriptionIntoLinkedChannelAsync(chatId, userId);

        if (!subscriptionIntoLinkedChannel.IsSubscribed)
        {
            subscriptionResults.Add(subscriptionIntoLinkedChannel);
        }

        var subscriptionIntoAddedChannel = await CheckChatMemberSubscriptionIntoAddedChannelAsync(chatId, userId);

        if (!subscriptionIntoAddedChannel.IsSubscribedToAll)
        {
            subscriptionResults.AddRange(subscriptionIntoAddedChannel.ChannelSubscriptions);
        }

        return subscriptionResults
            .DistinctBy(result => result.ChannelId)
            .ToList();
    }

    public async Task<ChannelSubscriptionResult> CheckChatMemberSubscriptionIntoLinkedChannelAsync(
        long chatId,
        long userId
    )
    {
        var subscriptionResult = new ChannelSubscriptionResult()
        {
            IsSubscribed = true
        };

        var getChat = await GetChatAsync(chatId);

        var linkedChatId = getChat.LinkedChatId ?? 0;

        if (getChat.LinkedChatId == null) return subscriptionResult;

        try
        {
            var chatLinked = await GetChatAsync(linkedChatId, evictBefore: true);

            if (chatLinked.Username == null)
            {
                Log.Information(
                    "Force Subs for ChatId: {ChatId} is disabled because linked channel with Id: {LinkedChatId} is not a Public Channel",
                    chatId,
                    linkedChatId
                );

                return subscriptionResult;
            }

            var inviteLink = chatLinked.InviteLink ?? chatLinked.GetChatLink();

            subscriptionResult.ChannelId = linkedChatId;
            subscriptionResult.ChannelName = chatLinked.Title;
            subscriptionResult.InviteLink = inviteLink;

            var chatMember = await GetChatMemberAsync(
                chatId: linkedChatId,
                userId: userId,
                evictBefore: true
            );

            var isSubscribed = chatMember.Status != ChatMemberStatus.Left;

            subscriptionResult.IsSubscribed = isSubscribed;

            return subscriptionResult;
        }
        catch (Exception exception)
        {
            var isMeHere = await IsMeHereAsync(
                chatId: linkedChatId,
                evictBefore: true
            );

            if (isMeHere)
            {
                subscriptionResult.IsSubscribed = false;

                _logger.LogDebug("Seem UserId {UserId} is not subscribed into Linked ChannelId: {ChatId}",
                    userId,
                    linkedChatId
                );
            }
            else
            {
                Log.Error(
                    exception,
                    "Error check Subscription for UserId: {UserId} into Linked ChannelId: {ChatId}. Is Bot there? {IsMeHere}",
                    userId,
                    linkedChatId,
                    isMeHere
                );
            }

            return subscriptionResult;
        }
    }

    public async Task<ChannelSubscriptionIntoAddedChannelResult> CheckChatMemberSubscriptionIntoAddedChannelAsync(
        long chatId,
        long userId
    )
    {
        var subscriptionResult = new ChannelSubscriptionIntoAddedChannelResult();

        var checkSubs = await _forceSubsService.GetSubsAsync(chatId);

        var checkSubscriptionTasks = checkSubs.Select(
                async subscription => {
                    var result = new ChannelSubscriptionIntoChannelResult
                    {
                        ChannelId = subscription.ChannelId
                    };

                    try
                    {
                        var chatMember = await GetChatMemberAsync(
                            chatId: subscription.ChannelId,
                            userId: userId,
                            evictBefore: true,
                            evictAfter: true
                        );

                        result.ChatMember = chatMember;
                    }
                    catch (Exception exception)
                    {
                        Log.Error(
                            exception,
                            "Error when check UserId: {UserId} Subscription into ChannelId: {ChannelId}",
                            userId,
                            subscription.ChannelId
                        );
                    }

                    return result;
                }
            )
            .Select(task => task.Result)
            .ToList();

        var notSubscribed = checkSubscriptionTasks
            .Where(x => x.ChatMember?.Status == ChatMemberStatus.Left)
            .ToList();

        subscriptionResult.IsSubscribedToAll = notSubscribed.Count == 0;
        subscriptionResult.ChannelSubscriptions.AddRange(
            notSubscribed.Select(
                (member) => {
                    var channel = checkSubs
                        .FirstOrDefault(subscription => subscription.ChannelId == member.ChannelId);

                    return new ChannelSubscriptionResult()
                    {
                        IsSubscribed = member.ChatMember.Status != ChatMemberStatus.Left,
                        ChannelId = member.ChannelId,
                        InviteLink = channel?.InviteLink,
                        ChannelName = channel?.ChannelTitle
                    };
                }
            )
        );

        return subscriptionResult;
    }

    public async Task<bool> IsMeHereAsync(
        long chatId,
        bool evictBefore = false
    )
    {
        var me = await _botService.GetMeAsync();
        var chatMember = await GetChatMemberAsync(
            chatId: chatId,
            userId: me.Id,
            evictBefore: evictBefore
        );

        var isHere = chatMember.Status is not (ChatMemberStatus.Left or ChatMemberStatus.Kicked);

        return isHere;
    }

    public async Task<bool> IsPrivateChat(long chatId)
    {
        var chat = await GetChatAsync(chatId);
        var isPrivateChat = chat.Type == ChatType.Private;

        return isPrivateChat;
    }

    public bool CheckGroupRestriction(long chatId)
    {
        Log.Information("Checking Chat Restriction for {ChatId}", chatId);

        if (!_restrictionConfig.EnableRestriction)
        {
            Log.Debug("Chat Restriction is not enabled in this chat!");
            return false;
        }

        var checkRestricted = !_restrictionConfig.RestrictionArea.Contains(chatId.ToString());
        Log.Debug(
            "Is ChatId {ChatId} restricted? {Check}",
            chatId,
            checkRestricted
        );

        return checkRestricted;
    }

    public StringAnalyzer FireAnalyzer(string text)
    {
        StringAnalyzer result = new();

        if (text == null)
        {
            _logger.LogDebug("No Message Text/Caption detected");

            return result;
        }

        result = text.AnalyzeString();
        var fireRatio = result.FireRatio;
        var wordCount = result.WordsCount;

        if (wordCount < 3)
        {
            _logger.LogDebug("String analyzer stop, because Words count is less than 3");
            return result;
        }

        var resultNote = fireRatio switch
        {
            >= 1 => "Tolong matikan CAPS LOCK sebelum mengetik pesan.",
            >= 0.6 => "Tolong kurangi penggunaan huruf kapital yang berlebihan.",
            _ => ""
        };

        result.ResultNote = resultNote;
        result.IsFired = fireRatio >= 0.6;

        return result;
    }

    public async Task DeleteMessageHistory(
        Func<MessageHistory, bool> predicate,
        int skipLast = 1
    )
    {
        var listMessageHistory = await _messageHistoryService.GetMessageHistoryAsync(null);
        var filtered = listMessageHistory
            .SkipLast(skipLast)
            .Where(predicate)
            .ToList();

        await DeleteMessageHistoryCoreAsync(filtered);
    }

    [JobDisplayName("Delete Old Message History")]
    public async Task DeleteOldMessageHistoryAsync()
    {
        var listMessageHistory = await _messageHistoryService.GetMessageHistoryAsync(null);
        var filteredHistory = listMessageHistory.Where(
            history =>
                DateTime.UtcNow >= history.DeleteAt
        ).ToList();

        await DeleteMessageHistoryCoreAsync(filteredHistory);
    }

    public async Task DeleteMessageHistoryCoreAsync(List<MessageHistory> listMessageHistory)
    {
        if (listMessageHistory.Count == 0)
        {
            _logger.LogInformation("No Message History to delete");
            return;
        }

        _logger.LogInformation(
            "Start deleting old message history. Items: {Count}",
            listMessageHistory.Count
        );

        await listMessageHistory.ForEachAsync(
            5,
            async history => {
                var chatId = history.ChatId;
                var messageId = history.MessageId.ToInt();

                try
                {
                    _logger.LogDebug(
                        "Deleting Message {MessageId} in Chat {ChatId}",
                        messageId,
                        chatId
                    );

                    await _botClient.DeleteMessageAsync(
                        chatId,
                        messageId
                    );

                    await _messageHistoryService.DeleteMessageHistoryAsync(
                        new MessageHistoryFindDto()
                        {
                            MessageFlag = history.MessageFlag,
                            ChatId = history.ChatId,
                            MessageId = history.MessageId
                        }
                    );
                }
                catch (Exception exception)
                {
                    if (exception.Contains("message to delete not found") ||
                        exception.Contains("message can't be deleted") ||
                        exception.Contains("bot was kicked") ||
                        exception.Contains("not a member") ||
                        exception.Contains("chat was upgraded") ||
                        exception.Contains("chat not found")
                    )
                    {
                        _logger.LogInformation(
                            "Error delete message History with Id: '{MessageId}' at ChatId: '{ChatId}'",
                            messageId,
                            chatId
                        );

                        await _messageHistoryService.DeleteMessageHistoryAsync(
                            new MessageHistoryFindDto()
                            {
                                MessageFlag = history.MessageFlag,
                                MessageId = history.MessageId,
                                ChatId = chatId
                            }
                        );
                    }
                    else
                    {
                        _logger.LogError(
                            exception,
                            "Error delete message History with Id: '{MessageId}' at ChatId: '{ChatId}'",
                            messageId,
                            chatId
                        );
                    }
                }
            }
        );

        _logger.LogInformation("Delete Message History done");
    }

    public async Task RegisterChatHealth()
    {
        Log.Information("Starting Check bot is Admin on all Group!");

        await _settingsService.PurgeSettings(PrivateSettingLimit);

        var allSettings = await _settingsService.GetAllSettings();

        Parallel.ForEach(
            allSettings,
            (
                chatSetting,
                state,
                args
            ) => {
                var chatId = chatSetting.ChatId.ToInt64();
                var reducedChatId = chatId.ReduceChatId();

                var adminCheckerId = AdminCheckerPrefix + "-" + reducedChatId;

                if (chatSetting.ChatType != ChatTypeEx.Private)
                {
                    Log.Debug("Creating Chat Jobs for ChatID '{ChatId}'", chatId);

                    HangfireUtil.RegisterJob(
                        adminCheckerId,
                        (PrivilegeService service) =>
                            service.AdminCheckerJobAsync(chatId),
                        Cron.Daily,
                        queue: "admin-checker",
                        fireAfterRegister: false
                    );
                }
                else
                {
                    var dateNow = DateTime.UtcNow;
                    var diffDays = (dateNow - chatSetting.UpdatedAt).TotalDays;
                    Log.Debug(
                        "Last activity days in '{ChatId}' is {DiffDays:N2} days",
                        chatId,
                        diffDays
                    );
                }
            }
        );
    }
}