﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog;

namespace WinTenDev.Zizi.Utils.IO;

public static class DirUtil
{
    public static string EnsureDirectory(
        this string dirPath,
        bool isDir = false
    )
    {
        Log.Debug("EnsuringDir: {DirPath}", dirPath);

        var path = Path.GetDirectoryName(dirPath);
        if (isDir) path = dirPath;

        if (path.IsNullOrEmpty()) return dirPath;
        if (Directory.Exists(path)) return dirPath;

        Log.Debug("Creating directory {Path}..", path);

        if (path != null)
            Directory.CreateDirectory(path);

        return dirPath;
    }

    public static string DeleteDirectory(this string dirPath)
    {
        Log.Information("Delete recursively directory: {DirPath}", dirPath);
        Directory.Delete(dirPath, recursive: true);
        return dirPath;
    }

    public static long DirSize(this string path)
    {
        var d = new DirectoryInfo(path);
        // Add file sizes.
        var fileInfos = d.GetFiles();
        var size = fileInfos.Sum(fi => fi.Length);

        // Add subdirectory sizes.
        var directoryInfos = d.GetDirectories();
        size += directoryInfos.Sum(unused => DirSize(unused.FullName));

        Log.Information(
            "{Path} size is {Size}",
            path,
            size
        );
        return size;
    }

    public static string SanitizeSlash(this string path)
    {
        if (path.IsNullOrEmpty()) return path;

        return path.Replace(
                @"\",
                "/",
                StringComparison.CurrentCulture
            )
            .Replace(
                "\\",
                "/",
                StringComparison.CurrentCulture
            );
    }

    public static string GetDirectory(this string path)
    {
        return Path.GetDirectoryName(path) ?? path;
    }

    public static string RemoveFiles(
        this string path,
        string filter = ""
    )
    {
        Log.Information("Deleting files in {Path}", path);

        var files = Directory.GetFiles(path)
            .Where(
                file =>
                    file.Contains(filter, StringComparison.CurrentCulture)
            );

        foreach (var file in files)
            File.Delete(file);

        return path;
    }

    public static string RemoveFiles(
        this string path,
        Func<string, bool> predicate
    )
    {
        Log.Information("Deleting files in {Path}", path);

        var files = Directory.GetFiles(path)
            .Where(predicate);

        foreach (var file in files)
            File.Delete(file);

        return path;
    }

    public static string RemoveDirs(
        this string path,
        Func<string, bool> predicate
    )
    {
        try
        {
            Log.Information("Deleting files in {Path}", path);

            var directories = Directory.GetDirectories(path)
                .Where(predicate)
                .ToList();

            Log.Debug("Directory to remove: {Directory}", directories);

            foreach (var directory in directories)
                directory.DeleteDirectory();

            Log.Information("Total removed Directories: {Count}", directories.Count);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error removing dirs");
        }

        return path;
    }


    public static IEnumerable<string> RemoveFiles(this IEnumerable<string> paths)
    {
        Log.Information("Deleting files in {Path}", paths.Count());

        foreach (var file in paths) File.Delete(file);

        return paths;
    }

    public static bool IsDirectory(this string path)
    {
        var fa = File.GetAttributes(path);
        return (fa & FileAttributes.Directory) != 0;
    }

    public static string TrimStartPath(this string filePath)
    {
        var trimStart = filePath.TrimStart(Path.DirectorySeparatorChar).TrimStart(Path.AltDirectorySeparatorChar);
        Log.Debug(
            "Path trimmed from {FilePath} to {TrimStart}",
            filePath,
            trimStart
        );
        return trimStart;
    }

    public static string GetPath(this string path)
    {
        var fileName = Path.GetFileName(path);
        return path.TrimStartPath().Replace(fileName, "");
    }

    public static string PathCombine(
        bool prependCurrentDir,
        params string[] paths
    )
    {
        var combinedPath = prependCurrentDir
            ? Path.Combine(
                Environment.CurrentDirectory,
                Path.Combine(paths)
            )
            : Path.Combine(paths);

        return combinedPath.SanitizeSlash();
    }

    public static string CleanCacheFiles(Func<string, bool> predicate)
    {
        return "Storage/Caches".RemoveFiles(predicate);
    }

    public static string CleanCacheDirs(Func<string, bool> predicate)
    {
        return "Storage/Caches".RemoveDirs(predicate);
    }

    public static string DeleteCachesSubDir(this string dirPath)
    {
        var path = Path.Combine("Storage/Caches/", dirPath).DeleteDirectory();
        return path;
    }
}
