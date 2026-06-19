using System.Collections.Generic;
using UnityEngine;

public class AddressablePreloadKeyProvider : MonoBehaviour
{

    [SerializeField]
    private List<string> _requiredAddressKeys = new List<string>();

    public List<object> CollectRequiredAddressableKeys()
    {
        HashSet<string> keySet = new HashSet<string>();

        for (int i = 0; i < _requiredAddressKeys.Count; i++)
        {
            string originalKey = _requiredAddressKeys[i];
            string key = originalKey?.Trim();

            if (string.IsNullOrWhiteSpace(key))
            {
                Debug.LogWarning($"[IntroFlow] Invalid inspector Addressable key. Index:{i}, Value:'{originalKey}'");
                continue;
            }

            keySet.Add(key);
        }

        AddActorTableAddressableKeys(keySet);

        List<object> keys = new List<object>();

        foreach (string key in keySet)
        {
            keys.Add(key);
        }

        return keys;
    }

    private void AddActorTableAddressableKeys(HashSet<string> keySet)
    {
        if (TableManager.Instance == null)
        {
            Debug.LogError("[IntroFlow] TableManager is null. ActorTable Addressable keys cannot be collected.");
            return;
        }

        IReadOnlyList<ActorTableData> actorTableList = TableManager.Instance.ActorTableList;

        if (actorTableList == null)
        {
            Debug.LogError("[IntroFlow] ActorTableList is null.");
            return;
        }

        for (int i = 0; i < actorTableList.Count; i++)
        {
            ActorTableData actorTableData = actorTableList[i];

            if (actorTableData == null)
            {
                Debug.LogWarning($"[IntroFlow] ActorTableData is null. Index:{i}");
                continue;
            }

            string originalPrefabKey = actorTableData.PrefabKey;
            string prefabKey = originalPrefabKey?.Trim();

            if (string.IsNullOrWhiteSpace(prefabKey))
            {
                Debug.LogWarning($"[IntroFlow] Invalid Actor PrefabKey. Index:{i}, ActorId:{actorTableData.Id}, Value:'{originalPrefabKey}'");
                continue;
            }

            keySet.Add(prefabKey);
        }
    }
}