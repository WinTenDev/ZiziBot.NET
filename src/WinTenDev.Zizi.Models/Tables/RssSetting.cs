﻿using System;
using Newtonsoft.Json;
using SqlKata;

namespace WinTenDev.Zizi.Models.Tables;

public class RssSetting
{
    [JsonProperty("id")]
    [Column("id")]
    public int Id { get; set; }

    [JsonProperty("chat_id")]
    [Column("chat_id")]
    public long ChatId { get; set; }

    [JsonProperty("from_id")]
    [Column("from_id")]
    public long FromId { get; set; }

    [JsonProperty("url_feed")]
    [Column("url_feed")]
    public string UrlFeed { get; set; }

    [Column("is_enabled")]
    public bool IsEnabled { get; set; }

    [Column("include_attachment")]
    public bool IncludeAttachment { get; set; }

    [JsonProperty("created_at")]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonProperty("updated_at")]
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}