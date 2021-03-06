using System;
using Dapper.FluentMap.Mapping;
using Newtonsoft.Json;
using SqlKata;
using WinTenDev.Zizi.Models.Enums;

namespace WinTenDev.Zizi.Models.Tables;

public class ChatSetting
{
    [Column("chat_id")]
    [JsonProperty("chat_id")]
    public long ChatId { get; set; }

    [Column("chat_type")]
    [JsonProperty("chat_type")]
    public ChatTypeEx ChatType { get; set; }

    [Column("member_count")]
    [JsonProperty("member_count")]
    public long MemberCount { get; set; }

    [Column("event_log_chat_id")]
    [JsonProperty("event_log_chat_id")]
    public long EventLogChatId { get; set; }

    [Column("is_admin")]
    [JsonProperty("is_admin")]
    public bool IsAdmin { get; set; }

    [Column("welcome_message")]
    [JsonProperty("welcome_message")]
    public string WelcomeMessage { get; set; }

    [Column("welcome_button")]
    [JsonProperty("welcome_button")]
    public string WelcomeButton { get; set; }

    [Column("welcome_media")]
    [JsonProperty("welcome_media")]
    public string WelcomeMedia { get; set; }

    [Column("welcome_media_type")]
    [JsonProperty("welcome_media_type")]
    public MediaType WelcomeMediaType { get; set; } = MediaType.Unknown;

    [Column("rules_text")]
    [JsonProperty("rules_text")]
    public string RulesText { get; set; }

    [Column("last_tags_message_id")]
    [JsonProperty("last_tags_message_id")]
    public long LastTagsMessageId { get; set; }

    [Column("last_warn_username_message_id")]
    [JsonProperty("last_warn_username_message_id")]
    public long LastWarnUsernameMessageId { get; set; }

    [Column("last_welcome_message_id")]
    [JsonProperty("last_welcome_message_id")]
    public long LastWelcomeMessageId { get; set; }

    [Column("enable_afk_status")]
    [JsonProperty("enable_afk_status")]
    public bool EnableAfkStatus { get; set; } = true;

    [Column("enable_global_ban")]
    [JsonProperty("enable_global_ban")]
    public bool EnableGlobalBan { get; set; } = true;

    [Column("enable_human_verification")]
    [JsonProperty("enable_human_verification")]
    public bool EnableHumanVerification { get; set; } = true;

    [JsonProperty("enable_fed_cas_ban")]
    [Column("enable_fed_cas_ban")]
    public bool EnableFedCasBan { get; set; } = true;

    [Column("enable_fed_es2_ban")]
    [JsonProperty("enable_fed_es2_ban")]
    public bool EnableFedEs2 { get; set; } = true;

    [Column("enable_fed_spamwatch")]
    [JsonProperty("enable_fed_spamwatch")]
    public bool EnableFedSpamWatch { get; set; } = true;

    [Column("enable_find_notes")]
    [JsonProperty("enable_find_notes")]
    public bool EnableFindNotes { get; set; }

    [Column("enable_find_tags")]
    [JsonProperty("enable_find_tags")]
    public bool EnableFindTags { get; set; } = true;

    public bool EnableFireCheck { get; set; }
    public bool EnableFloodCheck { get; set; }
    public bool EnableForceSubscription { get; set; }
    public bool EnablePrivacyMode { get; set; }
    public bool EnableReplyNotification { get; set; }
    public bool EnableSpellCheck { get; set; }

    [Column("enable_word_filter_group")]
    [JsonProperty("enable_word_filter_group")]
    public bool EnableWordFilterPerGroup { get; set; } = true;

    [Column("enable_word_filter_global")]
    [JsonProperty("enable_word_filter_global")]
    public bool EnableWordFilterGroupWide { get; set; } = true;

    [Column("enable_warn_username")]
    [JsonProperty("enable_warn_username")]
    public bool EnableWarnUsername { get; set; } = true;

    [Column("enable_check_profile_photo")]
    public bool EnableCheckProfilePhoto { get; set; } = true;

    [Column("enable_welcome_message")]
    [JsonProperty("enable_welcome_message")]
    public bool EnableWelcomeMessage { get; set; } = true;

    [Column("enable_zizi_mata")]
    [JsonProperty("enable_zizi_mata")]
    public bool EnableZiziMata { get; set; } = true;

    [Column("created_at")]
    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    [JsonProperty("updated_at")]
    public DateTime UpdatedAt { get; set; }

    public int FloodOffset { get; set; }
    public string TimeZoneOffset { get; set; }
    public string LanguageCode { get; set; }
}

public class ChatSettingMap : EntityMap<ChatSetting>
{
    public ChatSettingMap()
    {
        Map(p => p.EnableFedEs2).ToColumn("enable_fed_es2_ban");
        Map(p => p.EnableWordFilterPerGroup).ToColumn("enable_word_filter_group");
        Map(p => p.EnableWordFilterGroupWide).ToColumn("enable_word_filter_global");
    }
}
