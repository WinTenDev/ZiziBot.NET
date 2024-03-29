﻿namespace WinTenDev.Zizi.Models.Tables;

public class LearnData
{
    public int Id { get; set; }
    public string Label { get; set; }
    public string Message { get; set; }
    public long FromId { get; set; }
    public long ChatId { get; set; }
    public string TimeStamp { get; set; }
}

public class LearnCsv
{
    public string Label { get; set; }
    public string Message { get; set; }
}