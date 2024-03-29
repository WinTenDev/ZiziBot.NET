﻿using System;
using System.Globalization;

namespace WinTenDev.Zizi.Utils;

public static class NumberUtil
{
    public static string SizeFormat(this double size, string suffix = null)
    {
        string[] norm = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        var count = norm.Length - 1;
        var x = 0;

        while (size >= 1000 && x < count)
        {
            size /= 1024;
            x++;
        }

        return string.Format($"{Math.Round(size, 2):N} {norm[x]}{suffix}", MidpointRounding.ToZero);
    }

    public static string SizeFormat(this long size, string suffix = null)
    {
        var sizeD = size.ToDouble();
        return SizeFormat(sizeD, suffix);
    }

    public static string NumberSeparator(this int number)
    {
        return number.ToString("N0", new CultureInfo("id-ID"));
    }

    public static int RandomInt(int min = 0, int max = 0)
    {
        Random ran = new();
        return ran.Next(min, max);
    }
}