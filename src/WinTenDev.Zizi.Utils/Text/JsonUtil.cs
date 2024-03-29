﻿using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using WinTenDev.Zizi.Models.JsonSettings;
using WinTenDev.Zizi.Utils.IO;
using YamlDotNet.Serialization;

namespace WinTenDev.Zizi.Utils.Text;

public static class JsonUtil
{
    private static readonly string workingDir = "Storage/Caches";

    public static string ToJson<T>(
        this T data,
        bool indented = false,
        bool followProperty = false
    )
    {
        var serializerSetting = new JsonSerializerSettings();

        if (followProperty) serializerSetting.ContractResolver = new CamelCaseFollowProperty();
        serializerSetting.Formatting = indented ? Formatting.Indented : Formatting.None;

        return JsonConvert.SerializeObject(data, serializerSetting);
    }

    public static T MapObject<T>(this string json)
    {
        return JsonConvert.DeserializeObject<T>(json);
    }

    public static JArray ToArray(this string data)
    {
        return JArray.Parse(data);
    }

    public static string JsonToYaml(this string json)
    {
        var obj = json.MapObject<dynamic>();

        var yaml = obj.ToYaml();

        return yaml;
    }

    public static string ToYaml(this object obj)
    {
        var serializer = new SerializerBuilder()
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();

        var yaml = serializer.Serialize(obj)
            .Trim();

        return yaml;
    }

    public static async Task<string> WriteToFileAsync<T>(
        this T data,
        string fileJson,
        bool indented = true
    )
    {
        var filePath = $"{workingDir}/{fileJson}".EnsureDirectory();
        var json = data.ToJson(indented);

        await json.ToFile(filePath);
        Log.Debug("Writing file complete. FileName: {FilePath}", filePath);

        return filePath;
    }

    public static string JsonFormat(
        this string content,
        Formatting formatting = Formatting.Indented
    )
    {
        if (formatting == Formatting.None) return content;

        var formatJson = JToken.Parse(content).ToString(formatting);

        return formatJson;
    }
}