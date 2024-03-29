﻿using System;
using System.Net;
using System.Text;
using MoreLinq;
using Telegram.Bot.Types;

namespace WinTenDev.Zizi.Models.Types;

// Source: https://raw.githubusercontent.com/AleXr64/Telegram-bot-framework/master/TGBotFramework/BotFramework/Utils/HtmlString.cs

public class HtmlMessage
{
    private readonly StringBuilder sb = new StringBuilder();

    public static HtmlMessage Empty => new HtmlMessage();

    public HtmlMessage Bold(string text) => TagBuilder("b", text);
    public HtmlMessage BoldBr(string text) => TagBuilder("b", text).Br();

    public HtmlMessage Bold(HtmlMessage inner) => TagBuilder("b", inner);

    public HtmlMessage Italic(string text) => TagBuilder("i", text);

    public HtmlMessage Italic(HtmlMessage inner) => TagBuilder("i", inner);

    public HtmlMessage Underline(string text) => TagBuilder("u", text);

    public HtmlMessage Underline(HtmlMessage inner) => TagBuilder("u", inner);

    public HtmlMessage Strike(string text) => TagBuilder("s", text);

    public HtmlMessage Strike(HtmlMessage inner) => TagBuilder("s", inner);

    public HtmlMessage Url(
        string url,
        string text
    ) => UrlTagBuilder(
        "a",
        $"href=\"{url}\"",
        text
    );

    public HtmlMessage User(
        long id,
        string text
    ) => Url($"tg://user?id={id}", text);

    public HtmlMessage Text(
        string text,
        bool encoded = false
    )
    {
        sb.Append(encoded ? WebUtility.HtmlEncode(text) : text);

        return this;
    }

    public HtmlMessage TextBr(
        string text,
        bool encoded = false
    ) => Text(text + Environment.NewLine, encoded);

    public HtmlMessage Br()
    {
        sb.Append(Environment.NewLine);
        return this;
    }

    public HtmlMessage Append(HtmlMessage message)
    {
        sb.Append(message);
        return this;
    }

    public HtmlMessage UserMention(User user)
    {
        var name = string.Empty;
        var appended = false;

        if (user.FirstName?.Length > 0)
        {
            name = user.FirstName;
            appended = true;
        }

        if (user.LastName?.Length > 0)
        {
            if (appended)
            {
                name += " ";
            }

            name += user.LastName;
        }

        var fullName = name.Length > 0 ? name : user.Username;
        return User(user.Id, fullName);
    }

    public HtmlMessage Code(string text) => TagBuilder("code", text);
    public HtmlMessage CodeBr(string text) => TagBuilder("code", text).Br();

    public HtmlMessage Pre(string text) => TagBuilder("pre", text);

    public HtmlMessage CodeWithStyle(
        string style,
        string text
    )
    {
        var str = WebUtility.HtmlEncode(text);
        sb.Append($"<pre><code class=\"{style}\">");
        sb.Append(str);
        sb.Append("</code></pre>");
        return this;
    }

    private HtmlMessage TagBuilder(
        string tag,
        string text
    )
    {
        var str = WebUtility.HtmlEncode(text);
        sb.Append($"<{tag}>");
        sb.Append(str);
        sb.Append($"</{tag}>");
        return this;
    }

    private HtmlMessage UrlTagBuilder(
        string tag,
        string tagParams,
        string text
    )
    {
        var str = WebUtility.HtmlEncode(text);
        sb.Append($"<{tag} {tagParams}>");
        sb.Append(str);
        sb.Append($"</{tag}>");
        return this;
    }

    private HtmlMessage TagBuilder(
        string tag,
        HtmlMessage innerSting
    )
    {
        sb.Append($"<{tag}>");
        sb.Append(innerSting);
        sb.Append($"</{tag}>");
        return this;
    }

    public HtmlMessage PopLine(int count = 1)
    {
        var sbStr = sb.ToString();

        sb.Clear();

        sbStr.Split(Environment.NewLine)
            .SkipLast(count)
            .ForEach(s => TextBr(s.Trim()));

        return this;
    }

    public override string ToString() => sb.ToString().Trim();
}
