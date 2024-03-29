﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenDev.Zizi.Services.Google;

namespace WinTenDev.Zizi.Services.Extensions;

public static class InlineQueryExtension
{
    private static async Task AnswerInlineQueryAsync(
        this TelegramService telegramService,
        IEnumerable<InlineQueryResult> inlineQueryResults
    )
    {
        var inlineQuery = telegramService.InlineQuery;
        var fromId = inlineQuery.From.Id;
        var inlineQueryId = inlineQuery.Id;

        var reducedResult = inlineQueryResults
            .DistinctBy(result => result.Id)
            .Take(50)
            .ToList();

        try
        {
            await telegramService.Client.AnswerInlineQueryAsync(
                inlineQueryId: inlineQueryId,
                results: reducedResult,
                cacheTime: 5
            );
        }
        catch (Exception exception)
        {
            Log.Error(
                exception,
                "Error when answering inline query: {Id} fromId: {FromId}",
                inlineQueryId,
                fromId
            );
        }
    }

    public static async Task OnInlineQueryAsync(this TelegramService telegramService)
    {
        var inlineQuery = telegramService.InlineQuery;
        Log.Debug("InlineQuery: {@Obj}", inlineQuery);

        var inlineQueryCmd = telegramService.GetInlineQueryAt<string>(0).Trim();

        var inlineQueryExecutionResult = inlineQueryCmd switch
        {
            "ping" => await telegramService.OnInlineQueryPingAsync(),
            "message" => await telegramService.OnInlineQueryMessageAsync(),
            "subscene" => await telegramService.OnInlineQuerySubsceneSearchAsync(),
            "subscene-dl" => await telegramService.OnInlineQuerySubsceneDownloadAsync(),
            "uup" => await telegramService.OnInlineQuerySearchUupAsync(),
            "kbbi" => await telegramService.OnInlineQuerySearchKbbiAsync(),
            "yt" => await telegramService.OnInlineQuerySearchYoutubeAsync(),
            _ => await telegramService.OnInlineQueryGuideAsync()
        };

        inlineQueryExecutionResult.Stopwatch.Stop();

        Log.Debug("Inline Query execution result: {@Result}", inlineQueryExecutionResult);
    }

    private static async Task<InlineQueryExecutionResult> OnInlineQueryGuideAsync(this TelegramService telegramService)
    {
        var learnMore = "https://docs.zizibot.winten.my.id/features/inline-query";
        var inlineResult = new InlineQueryExecutionResult();

        var learnMoreContent = $"Silakan pelajari selengkapnya" +
                               $"\n{learnMore}" +
                               $"\n\nAtau tekan salah satu tombol dibawah ini";

        var replyMarkup = new InlineKeyboardMarkup(
            new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Ping", $"ping")
                },
                new[]
                {
                    InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Buat pesan dengan tombol", $"message ")
                },
                new[]
                {
                    InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Cari subtitle", "subscene ")
                },
                new[]
                {
                    InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Cari KBBI", "kbbi ")
                },
                new[]
                {
                    InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Cari UUP dump", "uup ")
                },
                new[]
                {
                    InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Cari Youtube", "yt ")
                },
            }
        );

        await telegramService.AnswerInlineQueryAsync(
            new List<InlineQueryResult>()
            {
                new InlineQueryResultArticle(
                    id: "guide-1",
                    title: "Bagaimana cara menggunakannya?",
                    inputMessageContent: new InputTextMessageContent(learnMoreContent)
                    {
                        DisableWebPagePreview = true
                    }
                )
                {
                    ReplyMarkup = replyMarkup
                },
                new InlineQueryResultArticle(
                    id: "guide-2",
                    title: "Cobalah ketikkan 'ping'",
                    inputMessageContent: new InputTextMessageContent(learnMoreContent)
                    {
                        DisableWebPagePreview = true
                    }
                )
                {
                    ReplyMarkup = replyMarkup
                }
            }
        );

        inlineResult.IsSuccess = true;

        return inlineResult;
    }

    private static async Task<InlineQueryExecutionResult> OnInlineQueryPingAsync(this TelegramService telegramService)
    {
        var inlineResult = new InlineQueryExecutionResult();

        await telegramService.AnswerInlineQueryAsync(
            new List<InlineQueryResult>()
            {
                new InlineQueryResultArticle(
                    "ping-result",
                    "Pong!",
                    new InputTextMessageContent("Pong!")
                )
            }
        );

        inlineResult.IsSuccess = true;

        return inlineResult;
    }

    private static async Task<InlineQueryExecutionResult> OnInlineQueryMessageAsync(this TelegramService telegramService)
    {
        var executionResult = new InlineQueryExecutionResult();

        var inlineQuery = telegramService.InlineQuery.Query;
        var parseMessage = inlineQuery
            .Split(new[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(s => s.Contains('='))
            .ToDictionary(
                s => s.Split('=')[0],
                s => s.Split('=')[1]
            );

        if (parseMessage.Count == 0)
        {
            var learnMore = "Pelajari cara membuat Pesan dengan Tombol via InlineQuery";
            var urlArticle = "https://docs.zizibot.winten.my.id/features/inline-query/pesan-dengan-tombol";

            await telegramService.AnswerInlineQueryAsync(
                new List<InlineQueryResult>()
                {
                    new InlineQueryResultArticle(
                        "iq-learn-mode",
                        "Pesan dengan tombol via InlineQuery",
                        new InputTextMessageContent(learnMore)
                        {
                            DisableWebPagePreview = true
                        }
                    )
                    {
                        Description = learnMore,
                        ReplyMarkup = new InlineKeyboardMarkup(
                            new[]
                            {
                                new[]
                                {
                                    InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Mulai membuat", "message ")
                                },
                                new[]
                                {
                                    InlineKeyboardButton.WithUrl("Pelajari selengkapnya..", urlArticle)
                                }
                            }
                        )
                    }
                }
            );

            return executionResult;
        }

        var caption = parseMessage.GetValueOrDefault("caption", string.Empty);
        var replyMarkup = parseMessage.GetValueOrDefault("button").ToInlineKeyboardButton().ToButtonMarkup();

        await telegramService.AnswerInlineQueryAsync(
            new List<InlineQueryResult>()
            {
                new InlineQueryResultArticle(
                    "123",
                    caption,
                    new InputTextMessageContent(caption)
                )
                {
                    ReplyMarkup = replyMarkup
                }
            }
        );

        executionResult.IsSuccess = true;

        return executionResult;
    }

    private static async Task<InlineQueryExecutionResult> OnInlineQuerySubsceneSearchAsync(this TelegramService telegramService)
    {
        var executionResult = new InlineQueryExecutionResult();
        List<SubsceneMovieSearch> subsceneMovieSearches;

        var queryCmd = telegramService.InlineQueryCmd;
        var queryValue = telegramService.InlineQueryValue;
        Log.Information("Starting find Subtitle with title: '{QueryValue}'", queryValue);

        var subsceneService = telegramService.GetRequiredService<SubsceneService>();

        if (queryValue.IsNotNullOrEmpty())
        {
            subsceneMovieSearches = await subsceneService.GetOrFeedMovieByTitle(queryValue);

            if (subsceneMovieSearches == null)
            {
                var title = "Tidak di temukan hasil, silakan cari judul yang lain";
                if (queryValue.IsNullOrEmpty())
                {
                    title = "Silakan masukkan judul yang ingin dicari";
                }

                await telegramService.AnswerInlineQueryAsync(
                    new List<InlineQueryResult>()
                    {
                        new InlineQueryResultArticle(
                            id: StringUtil.NewGuid(),
                            title: title,
                            inputMessageContent: new InputTextMessageContent("Tekan tombol dibawah ini untuk memulai pencarian")
                        )
                        {
                            ReplyMarkup = new InlineKeyboardMarkup(
                                new[]
                                {
                                    InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Pencarian baru", "subscene ")
                                }
                            )
                        }
                    }
                );
                executionResult.IsSuccess = false;

                return executionResult;
            }
        }
        else
        {
            subsceneMovieSearches = await subsceneService.GetOrFeedMovieByTitle("");
        }

        Log.Information(
            "Found about {AllCount} title with query: '{QueryValue}'",
            subsceneMovieSearches.Count,
            queryValue
        );

        var inlineQueryResultArticles = subsceneMovieSearches
            .OrderByDescending(search => search.CreatedOn)
            .Select(
                item => {
                    var movieTitle = item.MovieName;
                    var pathName = item.MovieUrl;
                    var subtitleCount = item.SubtitleCount;
                    var moviePath = pathName.Split("/").Take(3).JoinStr("/");
                    var slug = pathName.Split("/").ElementAtOrDefault(2);
                    var subsceneUrl = $"https://subscene.com{moviePath}";

                    var titleHtml = HtmlMessage.Empty
                        .Bold("Judul: ").CodeBr(movieTitle)
                        .Bold("Tersedia : ").CodeBr(subtitleCount);

                    var article = new InlineQueryResultArticle(
                        id: item.ID,
                        title: movieTitle,
                        inputMessageContent: new InputTextMessageContent(titleHtml.ToString())
                        {
                            ParseMode = ParseMode.Html,
                            DisableWebPagePreview = true
                        }
                    )
                    {
                        Description = $"Tersedia: {subtitleCount}",
                        ReplyMarkup = new InlineKeyboardMarkup(
                            new[]
                            {
                                new[]
                                {
                                    InlineKeyboardButton.WithUrl("Tautan Subscene", subsceneUrl)
                                },
                                new[]
                                {
                                    InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Pencarian lanjut", $"subscene {queryValue} "),
                                    InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Pencarian baru", "subscene ")
                                },
                                new[]
                                {
                                    InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Mulai unduh", $"subscene-dl {slug} "),
                                }
                            }
                        )
                    };

                    return article;
                }
            );

        // }
        await telegramService.AnswerInlineQueryAsync(inlineQueryResultArticles);
        executionResult.IsSuccess = true;

        return executionResult;
    }

    private static async Task<InlineQueryExecutionResult> OnInlineQuerySubsceneDownloadAsync(this TelegramService telegramService)
    {
        var executionResult = new InlineQueryExecutionResult();
        var queryCmd = telegramService.GetInlineQueryAt<string>(0);
        var query1 = telegramService.GetInlineQueryAt<string>(1);
        var query2 = telegramService.GetInlineQueryAt<string>(2);
        var queryValue = telegramService.InlineQueryValue;
        Log.Information("Starting find Subtitle file with title: '{QueryValue}'", query1);

        var subsceneService = telegramService.GetRequiredService<SubsceneService>();
        var searchBySlug = await subsceneService.GetOrFeedSubtitleBySlug(query1);
        Log.Information(
            "Found about {AllCount} subtitle by slug: '{QueryValue}'",
            searchBySlug.Count,
            query1
        );

        var filteredSearch = searchBySlug.Where(
            element => {
                if (query2.IsNullOrEmpty()) return true;

                return element.Language.Contains(query2, StringComparison.CurrentCultureIgnoreCase) ||
                       element.MovieName.Contains(query2, StringComparison.CurrentCultureIgnoreCase) ||
                       element.Owner.Contains(query2, StringComparison.CurrentCultureIgnoreCase);
            }
        ).ToList();

        Log.Information(
            "Found about {FilteredCount} of {AllCount} subtitle with title: '{QueryValue}'",
            filteredSearch.Count,
            searchBySlug.Count,
            query2
        );

        if (filteredSearch.Count == 0)
        {
            await telegramService.AnswerInlineQueryAsync(
                new List<InlineQueryResult>()
                {
                    new InlineQueryResultArticle(
                        id: StringUtil.NewGuid(),
                        title: "Tidak di temukan hasil, silakan cari bahasa/judul yang lain",
                        inputMessageContent: new InputTextMessageContent("Tekan tombol dibawah ini untuk memulai pencarian")
                    )
                    {
                        ReplyMarkup = new InlineKeyboardMarkup(
                            new[]
                            {
                                InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Pencarian baru", "subscene ")
                            }
                        )
                    }
                }
            );
            executionResult.IsSuccess = false;

            return executionResult;
        }

        var urlStart = await telegramService.GetUrlStart("");

        var result = filteredSearch.Select(
            element => {
                var documentId = element.ID;
                var languageSub = element.Language;
                var movieName = element.MovieName;
                var movieUrl = element.MovieUrl;
                var ownerSub = element.Owner;
                var slug = element.MovieUrl?.Split("/").Skip(2).JoinStr("/");
                var subtitleUrl = "https://subscene.com" + movieUrl;

                Log.Debug(
                    "Appending Movie with slug: '{0}' => {1}",
                    slug,
                    movieName
                );

                var titleResult = $"{languageSub} | {ownerSub}";

                var content = HtmlMessage.Empty
                    .Bold("Nama/Judul: ").CodeBr(movieName)
                    .Bold("Bahasa: ").CodeBr(languageSub)
                    .Bold("Pemilik: ").Text(element.Owner);

                // var startDownloadUrl = urlStart + "start=sub-dl_" + slug.Replace("/", "=");
                var startDownloadUrl = urlStart + "start=sub-dl_" + documentId;

                var article = new InlineQueryResultArticle(
                    id: documentId,
                    title: titleResult,
                    inputMessageContent: new InputTextMessageContent(content.ToString())
                    {
                        ParseMode = ParseMode.Html,
                        DisableWebPagePreview = true
                    }
                )
                {
                    Description = movieName,
                    ReplyMarkup = new InlineKeyboardMarkup(
                        new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithUrl("Tautan subtitle", subtitleUrl)
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Pencarian lanjut", $"subscene-dl {queryValue} "),
                                InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Ulang pencarian", $"subscene-dl {query1} "),
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithUrl("Unduh subtitle", startDownloadUrl),
                                InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Pencarian baru", "subscene ")
                            }
                        }
                    )
                };

                return article;
            }
        );

        await telegramService.AnswerInlineQueryAsync(result);

        executionResult.IsSuccess = true;

        return executionResult;
    }

    private static async Task<InlineQueryExecutionResult> OnInlineQuerySearchUupAsync(this TelegramService telegramService)
    {
        var inlineQueryExecution = new InlineQueryExecutionResult();
        var uupService = telegramService.GetRequiredService<UupDumpService>();
        var query1 = telegramService.GetInlineQueryAt<string>(1);
        var query2 = telegramService.GetInlineQueryAt<string>(2);
        var queryValue = telegramService.InlineQueryValue;
        var builds = await uupService.GetUpdatesAsync(queryValue);

        var inlineQueryResults = builds.Response.Builds.Select(
            build => {
                var title = build.BuildNumber + " - " + build.Arch.ToString().ToUpper();

                var downloadLink = build.Arch == Arch.Arm64
                    ? $"https://uupdump.net/download.php?id={build.Uuid}&pack=en-us&edition=core;professional"
                    : $"https://uupdump.net/download.php?id={build.Uuid}&pack=en-us&edition=core;coren;professional;professionaln";

                var htmlDescription = HtmlMessage.Empty
                    .TextBr(build.Created.ToString("yyyy-MM-dd HH:mm:ss tt zz"))
                    .TextBr(build.Title);

                var htmlContent = HtmlMessage.Empty
                    .Bold("Title: ").CodeBr(build.Title)
                    .Bold("Version: ").CodeBr(build.BuildNumber)
                    .Bold("Date: ").CodeBr(build.Created.ToString("yyyy-MM-dd HH:mm:ss tt zz"))
                    .Bold("Arch: ").CodeBr(build.Arch.ToString().ToUpper());

                var result = new InlineQueryResultArticle(
                    id: build.Uuid,
                    title: title,
                    inputMessageContent: new InputTextMessageContent(htmlContent.ToString())
                    {
                        ParseMode = ParseMode.Html,
                        DisableWebPagePreview = true
                    }
                )
                {
                    Description = htmlDescription.ToString(),
                    ReplyMarkup = new InlineKeyboardMarkup(
                        new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithUrl("Unduh", downloadLink)
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Pencarian lanjut", $"uup {queryValue} "),
                                InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Ulang pencarian", $"uup "),
                            }
                        }
                    )
                };

                return result;
            }
        );

        await telegramService.AnswerInlineQueryAsync(inlineQueryResults);

        return inlineQueryExecution;
    }

    public static async Task<InlineQueryExecutionResult> OnInlineQuerySearchKbbiAsync(this TelegramService telegramService)
    {
        var kbbiService = telegramService.GetRequiredService<KbbiService>();
        var inlineValue = telegramService.InlineQueryValue;

        var kbbiUrl = string.Empty;
        var articleTitle = string.Empty;
        var articleContent = string.Empty;
        var articleId = StringUtil.NewGuid();
        var htmlContent = HtmlMessage.Empty
            .Bold("KBBI (Kamus Besar Bahasa Indonesia)").Br();

        InlineKeyboardMarkup keyboardMarkup = InlineKeyboardMarkup.Empty();

        if (inlineValue.IsNullOrEmpty())
        {
            articleTitle = "KBBI (Kamus Besar Bahasa Indonesia)";
            articleContent = "Mulai ketikkan kata yang ingin Anda cari.";
            htmlContent.Text(articleContent).Br().Br()
                .Text("Kiat: Cobalah klik salah satu tombol dibawah ini");

            keyboardMarkup = new InlineKeyboardMarkup(
                new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Mulai pencarian", $"kbbi ")
                    }
                }
            );
        }
        else
        {
            var kbbiSearch = await kbbiService.SearchWord(inlineValue);

            kbbiUrl = kbbiSearch.Url;
            articleId = kbbiSearch.Url;
            articleTitle = $"Kata: {inlineValue}";
            articleContent = kbbiSearch.Content;

            htmlContent
                .Bold("Kata: ").CodeBr(inlineValue)
                .TextBr(kbbiSearch.Content);

            keyboardMarkup = new InlineKeyboardMarkup(
                new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithUrl("KBBI", kbbiUrl),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Pencarian lanjut", $"kbbi {inlineValue} "),
                        InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Ulang pencarian", $"kbbi "),
                    }
                }
            );
        }

        var result = new InlineQueryResultArticle(
            id: articleId,
            title: articleTitle,
            inputMessageContent: new InputTextMessageContent(htmlContent.ToString())
            {
                ParseMode = ParseMode.Html,
                DisableWebPagePreview = true
            }
        )
        {
            Description = articleContent,
            ReplyMarkup = keyboardMarkup
        };

        await telegramService.AnswerInlineQueryAsync(
            new List<InlineQueryResult>()
            {
                result
            }
        );

        return default;
    }

    public static async Task<InlineQueryExecutionResult> OnInlineQuerySearchYoutubeAsync(this TelegramService telegramService)
    {
        var executionResult = new InlineQueryExecutionResult();
        var inlineValue = telegramService.InlineQueryValue;
        var youtubeService = telegramService.GetRequiredService<YoutubeService>();
        var searchResults = await youtubeService.SearchVideoByTitle(inlineValue);

        var urlStartBase = await telegramService.GetUrlStart("");

        var inlineQueryResults = await searchResults.SelectAsync(async result => {
            var header = result.Author + " - " + result.Duration;
            var content = result.Title;
            var thumbs = result.Thumbnails.FirstOrDefault();

            var urlStart = await telegramService.GetUrlStart("start=yt-dl_" + result.Id);

            return new InlineQueryResultArticle(
                id: "yt_" + result.Url,
                title: header,
                inputMessageContent: new InputTextMessageContent(content)
                {
                    ParseMode = ParseMode.Html
                }
            )
            {
                ThumbUrl = thumbs?.Url,
                Description = content,
                ReplyMarkup = new InlineKeyboardMarkup(
                    new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithUrl("🌐 Open", result.Url),
                            InlineKeyboardButton.WithUrl("⬇ Download", urlStart),
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Pencarian lanjut", $"yt {inlineValue} "),
                            InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Pencarian baru", "yt ")
                        }
                    }
                )
            };
        });

        await telegramService.AnswerInlineQueryAsync(inlineQueryResults);

        return executionResult;
    }
}