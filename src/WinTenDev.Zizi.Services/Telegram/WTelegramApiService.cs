using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TL;
using WTelegram;

namespace WinTenDev.Zizi.Services.Telegram;

public class WTelegramApiService
{
    private readonly ILogger<WTelegramApiService> _logger;
    private readonly CacheService _cacheService;
    private readonly Client _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="WTelegramApiService"/> class
    /// </summary>
    /// <param name="logger">The logger</param>
    /// <param name="cacheService"></param>
    /// <param name="client">WTelegram client</param>
    public WTelegramApiService(
        ILogger<WTelegramApiService> logger,
        CacheService cacheService,
        Client client
    )
    {
        _logger = logger;
        _cacheService = cacheService;
        _client = client;
    }

    private async Task<Channel> GetChannel(long chatId)
    {
        var channelId = chatId.ReduceChatId();

        var channel = await _cacheService.GetOrSetAsync(
            cacheKey: "tdlib_get-channel_" + channelId,
            staleAfter: "30s",
            action: async () => {
                var chats = await _client.Messages_GetAllChats();
                var channel = (Channel) chats.chats.Values.FirstOrDefault(chat => chat.ID == channelId);

                return channel;
            }
        );

        return channel;
    }

    public async Task<Users_UserFull> GetMeAsync()
    {
        var fullUser = await _cacheService.GetOrSetAsync(
            cacheKey: "tdlib_get-me-probe",
            staleAfter: "5m",
            action: async () => {
                var fullUser = await _client.Users_GetFullUser(new InputUserSelf());

                return fullUser;
            }
        );

        return fullUser;
    }

    public async Task<bool> IsProbeHereAsync(long chatId)
    {
        var channel = await GetChannel(chatId);

        var isProbeHere = channel != null;

        _logger.LogDebug(
            "Is User Probe added to {ChatId}? {IsAdmin}",
            chatId,
            isProbeHere
        );

        return isProbeHere;
    }

    public async Task<bool> IsProbeAdminAsync(long chatId)
    {
        try
        {
            var getMe = await GetMeAsync();
            var meId = getMe.full_user.id;

            var adminList = await GetChatAdministratorsCore(chatId);
            var isCreator = adminList.ParticipantCreator.users.Any(pair => pair.Value.id == meId);
            var isAdmin = adminList.ParticipantAdmin.users.Any(pair => pair.Value.id == meId);
            var isCreatorOrAdmin = isCreator || isAdmin;

            _logger.LogDebug(
                "User Probe is Admin at {ChatId}? {IsAdmin}",
                chatId,
                isCreatorOrAdmin
            );

            return isCreatorOrAdmin;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to check if User Probe is Admin at {ChatId}",
                chatId
            );

            return false;
        }
    }

    public async Task<Channels_ChannelParticipants> GetAllParticipants(
        long chatId,
        ChannelParticipantsFilter channelParticipantsFilter = null,
        bool evictAfter = false,
        bool disableCache = false
    )
    {
        var channelId = chatId.ReduceChatId();

        var isProbeAdmin = await IsProbeAdminAsync(chatId);

        var channelParticipants = await _cacheService.GetOrSetAsync(
            cacheKey: "tdlib_channel-participants_" + channelId,
            evictAfter: evictAfter,
            disableCache: disableCache,
            staleAfter: "5m",
            action: async () => {
                var channel = await GetChannel(chatId);

                var allParticipants = await _client.Channels_GetAllParticipants(channel: channel, includeKickBan: isProbeAdmin);

                return allParticipants;
            }
        );

        return channelParticipants;
    }

    public async Task<Channels_ChannelParticipants> GetAllParticipantsCore(long chatId)
    {
        var channel = await GetChannel(chatId);

        var allParticipants = await _client.Channels_GetAllParticipants(channel);

        return allParticipants;
    }

    public async Task<ChannelParticipants> GetChatAdministratorsCore(
        long chatId,
        bool disableCache = false,
        bool evictAfter = false
    )
    {
        var channelId = chatId.ReduceChatId();
        var channel = await GetChannel(chatId);

        var channelParticipants = await _cacheService.GetOrSetAsync(
            cacheKey: "tdlib_channel-administrator_" + channelId,
            evictAfter: evictAfter,
            disableCache: disableCache,
            staleAfter: "30s",
            action: async () => {
                var channelsParticipants = await _client.Channels_GetParticipants(
                    channel: channel,
                    filter: new ChannelParticipantsAdmins(),
                    offset: 0,
                    limit: 0,
                    hash: 0
                );

                var participantCreator = channelsParticipants.participants
                    .Where(x => x.GetType() == typeof(ChannelParticipantCreator))
                    .Select(x => x as ChannelParticipantCreator);

                var participantAdmins = channelsParticipants.participants
                    .Where
                    (
                        x =>
                            x.GetType() == typeof(ChannelParticipantAdmin) ||
                            x.UserID != participantCreator.FirstOrDefault().UserID
                    )
                    .Select(x => x as ChannelParticipantAdmin);

                var participants = new ChannelParticipants()
                {
                    ParticipantCreator = new Channels_ChannelParticipants()
                    {
                        participants = participantCreator.ToArray(),
                        users = channelsParticipants.users.Where(x => x.Value.ID == participantCreator.FirstOrDefault()?.UserID)
                            .ToDictionary(x => x.Key, x => x.Value)
                    },
                    ParticipantAdmin = new Channels_ChannelParticipants()
                    {
                        participants = participantAdmins.ToArray(),
                        users = channelsParticipants.users.Where(x => x.Value.ID != participantCreator.FirstOrDefault()?.UserID)
                            .Where(x => participantAdmins.Any(y => y.UserID == x.Value.ID))
                            .ToDictionary(x => x.Key, x => x.Value)
                    }
                };

                return participants;
            }
        );

        return channelParticipants;
    }

    public async Task<List<Message>> GetAllMessagesAsync(
        long chatId,
        int startMessageId,
        int endMessageId,
        long userId = -1
    )
    {
        var peer = await GetChannel(chatId);

        var listMessage = new List<Message>();

        for (var offsetId = 0;;)
        {
            var messages = await _client.Messages_GetHistory(
                peer,
                offsetId,
                min_id: endMessageId - 1,
                max_id: startMessageId
            );

            if (messages.Messages.Length == 0) break;

            foreach (var msgBase in messages.Messages)
            {
                if (msgBase is not Message msg) continue;

                listMessage.Add(msg);
            }

            offsetId = messages.Messages[^1].ID;

            await Task.Delay(10);

            // if (listMessage.Count > limit)
            // {
            //     break;
            // }
        }

        if (userId == -1) return listMessage;

        var filteredUser = listMessage.Where(x => x.From.ID == userId).ToList();

        return filteredUser;
    }

    public async Task<List<int>> GetMessagesIdByUserId(
        long chatId,
        long userId,
        int lastMessageId
    )
    {
        _logger.LogInformation(
            "Getting list messageId from UserId {UserId} in ChatId {ChatId}",
            userId,
            chatId
        );

        var messageIds = await _cacheService.GetOrSetAsync(
            cacheKey: "tdlib_list-message-id_" + chatId + "_" + userId,
            action: async () => {
                var offset = 200;
                var channel = await GetChannel(chatId);

                var messageRanges = Enumerable
                    .Range(lastMessageId - offset, offset)
                    .Reverse()
                    .Select(id => new InputMessageID() { id = id })
                    .Cast<InputMessage>()
                    .ToArray();

                var allMessages = await _client.Channels_GetMessages(channel, messageRanges);
                var filteredMessage = allMessages.Messages
                    .Where(messageBase => messageBase.GetType() == typeof(Message))
                    .Where(messageBase => messageBase.From.ID == userId);
                var messageIds = filteredMessage.Select(messageBase => messageBase.ID);

                return messageIds.ToList();
            }
        );

        return messageIds;
    }

    public async Task DeleteMessageByUserId(
        long chatId,
        long userId,
        int lastMessageId
    )
    {
        try
        {
            _logger.LogInformation(
                "Deleting messages from UserId {UserId} in ChatId {ChatId}",
                userId,
                chatId
            );

            var channel = await GetChannel(chatId);
            var messageIds = await GetMessagesIdByUserId(
                chatId,
                userId,
                lastMessageId
            );

            var deleteMessages = await _client.Channels_DeleteMessages(channel, messageIds.ToArray());

            _logger.LogDebug("Deleted {@AffectedHistory} messages", deleteMessages);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Error deleting messages from UserId {UserId} in ChatId {ChatId}",
                userId,
                chatId
            );
        }
    }

    public async Task<int> DeleteMessagesAsync(
        long chatId,
        List<int> messageIds
    )
    {
        var affectedCount = 0;
        var channel = await GetChannel(chatId);

        await messageIds.Chunk(100)
            .AsyncParallelForEach(
                maxDegreeOfParallelism: 20,
                body: async ints => {
                    var delete = await _client.Channels_DeleteMessages(channel, ints.ToArray());

                    affectedCount += delete.pts_count;
                }
            );

        return messageIds.Count;
    }

    public async Task<Contacts_ResolvedPeer> FindPeerByUsername(string username)
    {
        try
        {
            var resolvedPeer = await _cacheService.GetOrSetAsync(
                cacheKey: "tdlib_resolved-peer_" + username,
                staleAfter: "1h",
                expireAfter: "24h",
                action: async () => {
                    var resolvedPeer = await _client.Contacts_ResolveUsername(username);

                    return resolvedPeer;
                }
            );

            return resolvedPeer;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Error finding peer by username {Username}",
                username
            );

            return default;
        }
    }

    public async Task<Users_UserFull> GetFullUser(long userId)
    {
        var fullUser = await _cacheService.GetOrSetAsync(
            cacheKey: "tdlib_full-user_" + userId,
            staleAfter: "1h",
            expireAfter: "24h",
            action: async () => {
        var fullUser = await _client.Users_GetFullUser(new InputUser(userId, _client.GetAccessHashFor<Users_UserFull>(userId)));

        return fullUser;
            });

        _logger.LogDebug("Full user for UserId: {UserId} => {@FullUser}",
            userId, fullUser
        );

        return fullUser;
    }
}