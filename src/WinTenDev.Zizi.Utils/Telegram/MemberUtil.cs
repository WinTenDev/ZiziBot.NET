﻿using System.Collections.Generic;
using Telegram.Bot.Types;
using WinTenDev.Zizi.Utils.Text;

namespace WinTenDev.Zizi.Utils.Telegram;

public static class MemberUtil
{
    public static string GetNameLink(
        this long userId,
        string name
    )
    {
        return $"<a href='tg://user?id={userId}'>{name}</a>";
    }

    public static string GetNameLink(
        this long userId,
        params string[] names
    )
    {
        var fullName = names.JoinStr(" ").Trim();
        return $"<a href='tg://user?id={userId}'>{fullName}</a>";
    }

    public static string GetNameLink(this User user)
    {
        if (user == null) return string.Empty;

        var fullName = user.GetFullName();

        return $"<a href='tg://user?id={user.Id}'>{fullName}</a>";
    }

    public static string GetMention(this long userId)
    {
        return userId.GetNameLink("&#8203;");
    }

    public static string GetFullName(this User user)
    {
        var firstName = user.FirstName;
        var lastName = user.LastName;

        return (firstName + " " + lastName).Trim().HtmlEncode();
    }

    public static string GetFullName(this TL.User user)
    {
        var firstName = user.first_name;
        var lastName = user.last_name;

        return (firstName + " " + lastName).Trim();
    }

    public static string GetNameLink(this TL.User user)
    {
        return $"<a href='tg://user?id={user.id}'>{user.GetFullName()}</a>";
    }

    public static string GetFromNameLink(this Message message)
    {
        var fromId = message.From.Id;
        var fullName = message.From.GetFullName();

        return $"<a href='tg://user?id={fromId}'>{fullName}</a>";
    }

    #region Random Name

    public static List<string> GetRandomNames()
    {
        var listStr = new List<string>()
        {
            "fulan",
            "fulanah"
        };

        return listStr;
    }

    public static string GetRandomName()
    {
        return GetRandomNames().RandomElement();
    }

    public static string GetRandomFullName()
    {
        var randomChild = GetRandomName();
        var bin = randomChild == "fulan" ? "bin" : "binti";

        return $"{randomChild} {bin} fulan";
    }

    #endregion
}
