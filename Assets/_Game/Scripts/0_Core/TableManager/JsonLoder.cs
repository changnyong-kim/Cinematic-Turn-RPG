using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public static class JsonLoader
{
    public static List<T> LoadList<T>(LoadConfig loadConfig, string relativeTablePath)
    {
        string fullPath = GetStreamingAssetsPath(relativeTablePath);
        return Load<List<T>>(fullPath);
    }

    public static List<T> LoadActorTable<T>(LoadConfig loadConfig)
    {
        return LoadList<T>(loadConfig, loadConfig.ActorTablePath);
    }

    public static List<T> LoadSkillTable<T>(LoadConfig loadConfig)
    {
        return LoadList<T>(loadConfig, loadConfig.SkillTablePath);
    }

    private static T Load<T>(string path)
    {
        if (File.Exists(path) == false)
        {
            Debug.LogError($"[JsonLoader] File not found. Path: {path}");
            return default;
        }

        string json = File.ReadAllText(path);

        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogError($"[JsonLoader] Json is empty. Path: {path}");
            return default;
        }

        return JsonConvert.DeserializeObject<T>(json);
    }

    private static string GetStreamingAssetsPath(string relativePath)
    {
        return Path.Combine(Application.streamingAssetsPath, relativePath);
    }
}