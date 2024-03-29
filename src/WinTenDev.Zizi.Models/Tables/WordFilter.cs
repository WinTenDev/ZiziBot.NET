using System;
using System.Text.Json.Serialization;
using RepoDb.Attributes;
using SqlKata;

namespace WinTenDev.Zizi.Models.Tables;

[Map("word_filter")]
public class WordFilter
{
    [JsonPropertyName("word")]
    [Column("word")]
    public string Word { get; set; }

    [JsonPropertyName(("is_global"))]
    [Column("is_global")]
    [Map("is_global")]
    public bool IsGlobal { get; set; }

    [JsonPropertyName("from_id")]
    [Column("from_id")]
    [Map("from_id")]
    public long FromId { get; set; }

    [JsonPropertyName("chat_id")]
    [Column("chat_id")]
    [Map("chat_id")]
    public long ChatId { get; set; }

    [JsonPropertyName("created_at")]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}