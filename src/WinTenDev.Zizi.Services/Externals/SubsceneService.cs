﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Flurl.Http;
using Humanizer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Entities;
using Serilog;
using SerilogTimings;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenDev.Zizi.Services.Google;

namespace WinTenDev.Zizi.Services.Externals;

public class SubsceneService
{
    private readonly SubsceneConfig _subsceneConfig;
    private readonly GoogleCloudConfig _googleCloudConfig;
    private readonly ILogger<SubsceneService> _logger;
    private readonly CacheService _cacheService;
    private readonly EventLogService _eventLogService;
    private readonly GoogleApiService _googleApiService;
    private readonly QueryService _queryService;
    private readonly DatabaseService _databaseService;
    private bool CanUseFeature => _subsceneConfig.IsEnabled;

    public SubsceneService(
        IOptionsSnapshot<GoogleCloudConfig> googleCloudConfig,
        IOptionsSnapshot<SubsceneConfig> subsceneConfig,
        ILogger<SubsceneService> logger,
        CacheService cacheService,
        EventLogService eventLogService,
        GoogleApiService googleApiService,
        QueryService queryService,
        DatabaseService databaseService
    )
    {
        _subsceneConfig = subsceneConfig.Value;
        _googleCloudConfig = googleCloudConfig.Value;
        _logger = logger;
        _cacheService = cacheService;
        _eventLogService = eventLogService;
        _googleApiService = googleApiService;
        _queryService = queryService;
        _databaseService = databaseService;
    }

    public async Task<int> SaveSourceUrl(SubsceneSource subsceneSource)
    {
        var find = await DB.Find<SubsceneSource>()
            .ManyAsync(x =>
                x.SearchTitleUrl == subsceneSource.SearchTitleUrl
            );

        if (find.Count != 0) return 0;

        await subsceneSource.InsertAsync();
        return 1;
    }

    public async Task<List<SubsceneSource>> GetSourcesAsync()
    {
        var find = await DB.Find<SubsceneSource>()
            .ExecuteAsync();

        return find;
    }

    public async Task<InlineKeyboardMarkup> GetSourcesAsButtonAsync()
    {
        var sources = await GetSourcesAsync();
        var buttonsList = sources.Select(source => {
                var btnEmoji = source.IsActive ? "✅" : "❌";
                var baseUrl = source.SearchTitleUrl.GetBaseUrl();
                var btnText = $"{btnEmoji} {baseUrl}";

                return InlineKeyboardButton.WithCallbackData(btnText, "sub-src " + source.ID);
            })
            .Chunk(1)
            .ToList();

        buttonsList.Add(new InlineKeyboardButton[]
        {
            InlineKeyboardButton.WithCallbackData("Tutup", "delete-message current-message"),
        });

        return buttonsList.ToButtonMarkup();
    }

    public async Task SetSourceUrlAsync(string sourceId)
    {
        _logger.LogInformation("Resetting Subscene Source Url state");
        var resetResult = await DB.Update<SubsceneSource>()
            .Match(_ => true)
            .Modify(source => source.IsActive, false)
            .ExecuteAsync();
        _logger.LogDebug("Reset result: {@ResetResult}", resetResult);

        _logger.LogInformation("Updating Source status by SourceId: {SourceId}", sourceId);
        var updateResult = DB.Update<SubsceneSource>()
            .Match(source => source.ID == sourceId)
            .Modify(source => source.IsActive, true)
            .ExecuteAsync();
        _logger.LogDebug("Update result: {@UpdateResult}", updateResult);
    }

    public async Task<SubsceneSource> GetActiveSourceAsync()
    {
        var sources = await GetSourcesAsync();
        var subsceneSource = sources.FirstOrDefault(source => source.IsActive);

        return subsceneSource;
    }

    public async Task<List<SubsceneMovieItem>> FeedPopularTitles()
    {
        var document = await AnglesharpUtil.DefaultContext.OpenAsync(_subsceneConfig.PopularTitleUrl);
        var htmlTableRows = document.All
            .Where(element => element.NodeName == "TR")
            .Skip(1)
            .OfType<IHtmlTableRowElement>();

        var movieResult = htmlTableRows.Select(
            element => {
                var innerText = element.TextContent.Split("\n", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                var item = new SubsceneMovieItem
                {
                    MovieUrl = element.QuerySelector<IHtmlAnchorElement>("a[href ^= '/subtitles']")?.PathName,
                    Language = innerText.FirstOrDefault(),
                    MovieName = innerText.ElementAtOrDefault(1),
                    Owner = element.Cells.FirstOrDefault(cellElement => cellElement.ClassName == "a5")?.TextContent.Trim(),
                    UploadDate = element.Cells
                        .FirstOrDefault(cellElement => cellElement.ClassName == "a6")
                        ?.Children.FirstOrDefault()?.GetAttribute("title")
                };

                return item;
            }
        ).ToList();

        try
        {
            await DB.DeleteAsync<SubsceneMovieItem>(item => item.CreatedOn < DateTime.Now.AddDays(-3));
            var insert = await movieResult.SaveAsync();

            _logger.LogInformation("Inserted {Inserted}", insert.InsertedCount);
        }
        catch (MongoBulkWriteException<SubsceneMovieItem> bulkWriteException)
        {
            _logger.LogError(bulkWriteException, "Error while save Popular movie");
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Error while save Popular movie");
        }

        return movieResult;
    }

    public async Task<List<SubsceneMovieSearch>> FeedMovieByTitle(string title)
    {
        var activeSource = await GetActiveSourceAsync();
        // var searchUrl = _subsceneConfig.SearchTitleUrl;
        var searchUrl = activeSource.SearchTitleUrl;

        Log.Information("Preparing parse {Url}", searchUrl);
        Log.Debug("Loading web {Url}", searchUrl);
        var searchUrlQuery = searchUrl + "?query=" + title;

        var document = await AnglesharpUtil.DefaultContext.OpenAsync(searchUrlQuery);
        var list = document.All
            .FirstOrDefault(element => element.ClassName == "search-result")?.Children
            .Where(element => element.LocalName == "ul")
            .SelectMany(element => element.Children)
            .OfType<IHtmlListItemElement>();

        _logger.LogDebug("Extracting data from {Url}", searchUrlQuery);
        var movieResult = list?.Select(
            element => {
                var movieName = element.TextContent.Split("\n", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                var movieUrl = element.QuerySelector<IHtmlAnchorElement>("a[href ^= '/subtitles']")?.PathName;

                var movie = new SubsceneMovieSearch()
                {
                    MovieName = movieName.FirstOrDefault(),
                    SubtitleCount = movieName.LastOrDefault(),
                    MovieUrl = movieUrl
                };

                return movie;
            }
        ).ToList();

        _logger.LogDebug(
            "Extracted {Count} title(s) from {Url}",
            movieResult?.Count,
            searchUrlQuery
        );

        try
        {
            if (!movieResult.AnyOrNotNull()) return default;

            _logger.LogDebug("Saving Subtitle Search to database. {rows} item(s)", movieResult?.Count);

            await DB.DeleteAsync<SubsceneMovieSearch>(
                search =>
                    search.CreatedOn <= DateTime.UtcNow.AddDays(-30)
            );

            var insert = await movieResult.SaveAsync();

            _logger.LogInformation("Inserted {Inserted}", insert.InsertedCount);

            var logMessage = HtmlMessage.Empty
                .BoldBr("Indexing Movie Item")
                .TextBr($"Sekitar {insert.InsertedCount} judul ditambahkan");

            await _eventLogService.SendEventLogCoreAsync(logMessage.ToString());
        }
        catch (MongoBulkWriteException bulkWriteException)
        {
            _logger.LogWarning(bulkWriteException, "Error while Mongo BulkWrite Search");
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Error while inserting movie result");
        }

        return movieResult;
    }

    public async Task<List<SubsceneSubtitleItem>> FeedSubtitleBySlug(string slug)
    {
        var activeSource = await GetActiveSourceAsync();
        var subtitleUrl = activeSource.SearchSubtitleUrl;

        var searchSubtitleFileUrl = $"{subtitleUrl}/{slug}";

        var document = await AnglesharpUtil.DefaultContext.OpenAsync(searchSubtitleFileUrl);
        var list = document.All
            .Where(element => element.LocalName == "tr")
            .Skip(2)
            .OfType<IHtmlTableRowElement>();

        var movieList = list.Select(
                element => {
                    var movieLangAndName = element.Children.FirstOrDefault();
                    var movieNameContents = movieLangAndName?.TextContent.Split("\n", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    var ownerSubsElement = element.Children.ElementAtOrDefault(3);
                    var commentElement = element.Children.ElementAtOrDefault(4);

                    var item = new SubsceneSubtitleItem()
                    {
                        Language = movieNameContents?.FirstOrDefault(),
                        MovieName = movieNameContents?.LastOrDefault(),
                        MovieUrl = element.QuerySelector<IHtmlAnchorElement>("a[href ^= '/subtitles']")?.PathName,
                        Owner = ownerSubsElement?.TextContent.Trim(),
                        Comment = commentElement?.TextContent.Trim()
                    };

                    return item;
                }
            )
            .Where(item => item.Language != null)
            .ToList();

        _logger.LogDebug(
            "Extracted {Count} subtitle(s) from {Url}",
            movieList?.Count,
            searchSubtitleFileUrl
        );

        try
        {
            _logger.LogDebug("Saving Subtitle language item Search to database. {rows} item(s)", movieList?.Count);
            await _queryService.MongoDbOpen("shared");
            var insert = await movieList.SaveAsync();

            _logger.LogInformation("Inserted {Inserted}", insert.InsertedCount);

            var logMessage = HtmlMessage.Empty
                .BoldBr("Indexing Subtitle Item")
                .TextBr($"Sekitar {insert.InsertedCount} subjudul ditambahkan");

            await _eventLogService.SendEventLogCoreAsync(logMessage.ToString());
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Error while inserting movie result");
        }

        return movieList;
    }

    public async Task<SubsceneMovieDetail> GetSubtitleFileAsync(string subtitleId)
    {
        _logger.LogInformation("Preparing download subtitle file {Slug}", subtitleId);
        var activeSource = await GetActiveSourceAsync();
        var searchSubtitleUrl = activeSource.SearchSubtitleUrl;

        var subsceneMovieDetail = new SubsceneMovieDetail();
        var subtitleItem = await DB.Find<SubsceneSubtitleItem>().OneAsync(subtitleId);
        var moviePath = subtitleItem.MovieUrl.Split("/").Skip(2).JoinStr("/");

        var address = $"{searchSubtitleUrl}/{moviePath}";
        var document = await AnglesharpUtil.DefaultContext.OpenAsync(address);
        var all = document.All
            .Where(element => element.ClassName == "top left")
            .OfType<IHtmlDivElement>()
            .FirstOrDefault();

        if (all == null)
        {
            subsceneMovieDetail.SubtitleMovieUrl = searchSubtitleUrl + "/" + moviePath;
            return subsceneMovieDetail;
        }

        var subtitleListUrl = subtitleItem.MovieUrl;
        var language = moviePath.Split("/").ElementAtOrDefault(1);
        var posterElement = (all?.QuerySelector<IHtmlAnchorElement>("a[href]")?.Children.FirstOrDefault() as IHtmlImageElement)?.Source;
        var headerElement = all?.QuerySelector<IHtmlDivElement>("div.header");
        var movieTitle = ((headerElement?.Children.FirstOrDefault() as IHtmlHeadingElement)?.Children.FirstOrDefault() as IHtmlSpanElement)?.TextContent.Trim();
        var releaseInfo = headerElement?.QuerySelector<IHtmlListItemElement>("li.release")?.Children.OfType<IHtmlDivElement>()
            .Select(element => element.TextContent.Trim()).ToList();
        var authorElement = headerElement?.QuerySelector<IHtmlAnchorElement>("a[href ^= '/u']");
        var comment = headerElement?.QuerySelector<IHtmlDivElement>("div.comment");
        var subtitleDownloadUrl = document.QuerySelectorAll<IHtmlAnchorElement>("a[href ^= '/subtitles']")
            .FirstOrDefault(element => element.Href.Contains("text"))?.Href;

        var pathOnDrive = "subtitles/" + moviePath.Split("/").SkipLast(1).JoinStr("/");

        var file = await _googleApiService.UploadFileToDrive(
            parentId: _googleCloudConfig.ZiziBotDrive,
            sourceFile: subtitleDownloadUrl,
            locationPath: pathOnDrive,
            preventDuplicate: true
        );

        var indexLocation = pathOnDrive + "/" + file.Name;

        var subtitleDownloadCdn = _googleCloudConfig.DriveIndexCdnUrl + "/" + indexLocation;

        subtitleItem.DriveFileId = file.Id;
        subtitleItem.IndexLocation = indexLocation;

        await subtitleItem.SaveAsync();

        subsceneMovieDetail = new SubsceneMovieDetail()
        {
            SubtitleMovieUrl = subtitleListUrl,
            MovieName = movieTitle,
            Language = language.Titleize(),
            CommentaryUrl = authorElement?.PathName,
            CommentaryUser = authorElement?.TextContent.Trim(),
            PosterUrl = posterElement,
            ReleaseInfo = releaseInfo?.JoinStr("\n"),
            ReleaseInfos = releaseInfo,
            Comment = comment?.TextContent.Trim(),
            SubtitleDownloadUrl = subtitleDownloadUrl,
            IndexLocationUrl = subtitleDownloadCdn,
            IndexLocation = indexLocation,
            SubtitleItem = subtitleItem
        };

        return subsceneMovieDetail;
    }

    public async Task SaveSearchTitle(List<IHtmlAnchorElement> searchByTitles)
    {
        _logger.LogInformation("Saving subscene search result. Count: {Count}", searchByTitles.Count);

        try
        {
            await _queryService.MongoDbOpen("shared");
            if (searchByTitles.Count == 0)
            {
                _logger.LogInformation("No title to save");
                return;
            }

            var subsceneMovieItems = searchByTitles.Select(
                element => new SubsceneMovieItem()
                {
                    MovieName = element.Text,
                    MovieUrl = element.PathName
                }
            );

            await subsceneMovieItems
                .DistinctBy(item => item.MovieUrl)
                .InsertAsync();

            _logger.LogInformation("Saved subscene search result. Count: {Count}", searchByTitles.Count);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error saving subscene search result");
        }
    }

    public async Task<List<SubsceneMovieSearch>> GetMovieByTitle(string title)
    {
        var movies = await DB.Find<SubsceneMovieSearch>().ManyAsync(
            item =>
                item.MovieName.Contains(title) ||
                item.MovieUrl.Contains(title)
        );

        return movies;
    }

    public async Task<List<SubsceneMovieItem>> GetPopularMovieByTitle()
    {
        var op = Operation.Begin("Get popular Movie by Title");

        var movies = await DB.Find<SubsceneMovieItem>()
            .Sort(item => item.UploadDate, Order.Ascending)
            .ExecuteAsync();

        var popular = movies
            .Select(
                item => new SubsceneMovieItem()
                {
                    MovieName = item.MovieName,
                    MovieUrl = item.MovieUrl,
                    UploadDate = item.UploadDate
                }
            )
            .DistinctBy(item => item.MovieName)
            .OrderByDescending(item => item.UploadDate)
            .ToList();

        _logger.LogInformation("Retrieved popular movies list, an about {Count} item(s)", popular.Count);
        op.Complete();

        return popular;
    }

    public async Task<List<SubsceneSubtitleItem>> GetSubtitleBySlug(string slug)
    {
        var subtitles = await DB.Find<SubsceneSubtitleItem>()
            .ManyAsync(
                item =>
                    new ExpressionFilterDefinition<SubsceneSubtitleItem>(
                        subtitleItem =>
                            subtitleItem.MovieUrl.Contains(slug)
                    )
            );

        return subtitles;
    }

    public async Task<List<SubsceneMovieItem>> GetOrFeedPopularMovie()
    {
        var getMovie = await GetPopularMovieByTitle();

        if (getMovie.Count > 0)
        {
            await FeedPopularTitles();

            return getMovie;
        }

        var feedMovie = await FeedPopularTitles();

        return feedMovie;
    }

    public async Task<List<SubsceneMovieSearch>> GetOrFeedMovieByTitle(string title)
    {
        var getMovieByTitle = await GetMovieByTitle(title);

        if (getMovieByTitle.Count > 0)
        {
            FeedMovieByTitle(title).InBackground();

            return getMovieByTitle;
        }

        var feedMovieByTitle = await FeedMovieByTitle(title);

        return feedMovieByTitle;
    }

    public async Task<List<SubsceneSubtitleItem>> GetOrFeedSubtitleBySlug(string slug)
    {
        var movieBySlug = await GetSubtitleBySlug(slug);

        if (movieBySlug.Count > 0)
        {
            FeedSubtitleBySlug(slug).InBackground();

            return movieBySlug;
        }

        var feedSubtitleBySlug = await FeedSubtitleBySlug(slug);

        return feedSubtitleBySlug;
    }
}