using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public static class AddressableTableValidator
{
    private const string ActorTablePath = "Assets/Data/ActorTable.json";

    [MenuItem("Tools/Addressables/Validate Actor Table")]
    public static void ValidateActorTable()
    {
        Debug.Log("[Addressables Table Validator] ActorTable.json 검사 시작...");

        List<ActorTableData> actorTable = LoadJson<List<ActorTableData>>(ActorTablePath);

        if (actorTable == null)
        {
            Debug.LogError($"ActorTable 로드 실패: {ActorTablePath}");
            return;
        }

        HashSet<string> addressableKeys = CollectAddressableKeys();

        foreach (ActorTableData actor in actorTable)
        {
            if (string.IsNullOrEmpty(actor.PrefabKey))
            {
                Debug.LogError($"❌ PrefabKey 비어 있음 - Id:{actor.Id}, Name:{actor.Name}");
                continue;
            }

            if (addressableKeys.Contains(actor.PrefabKey))
            {
                Debug.Log($"✓ {actor.PrefabKey} 존재");
            }
            else
            {
                Debug.LogError($"❌ {actor.PrefabKey} Key 없음 - Id:{actor.Id}, Name:{actor.Name}");
            }
        }

        Debug.Log("[Addressables Table Validator] 검사 완료");
    }

    private static T LoadJson<T>(string path)
    {
        if (File.Exists(path) == false)
        {
            Debug.LogError($"JSON 파일 없음: {path}");
            return default;
        }

        string json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<T>(json);
    }

    private static HashSet<string> CollectAddressableKeys()
    {
        HashSet<string> keys = new HashSet<string>();

        AddressableAssetSettings settings =
            AddressableAssetSettingsDefaultObject.Settings;

        if (settings == null)
        {
            Debug.LogError("AddressableAssetSettings를 찾을 수 없습니다.");
            return keys;
        }

        foreach (AddressableAssetGroup group in settings.groups)
        {
            if (group == null)
            {
                continue;
            }

            foreach (AddressableAssetEntry entry in group.entries)
            {
                if (entry == null)
                {
                    continue;
                }

                keys.Add(entry.address);
            }
        }

        return keys;
    }
}