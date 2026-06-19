using System.Collections.Generic;
using UnityEngine;

public sealed class TableManager : MonoBehaviour
{
    public static TableManager Instance
    {
        get; private set;
    }

    [SerializeField]
    private LoadConfig _loadConfig;

    private List<ActorTableData> _actorTableList = new List<ActorTableData>();

    public IReadOnlyList<ActorTableData> ActorTableList => _actorTableList;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool LoadTables()
    {
        if (_loadConfig == null)
        {
            Debug.LogError("[TableManager] LoadConfig is null.");
            return false;
        }

        _actorTableList = JsonLoader.LoadActorTable<ActorTableData>(_loadConfig);

        if (_actorTableList == null)
        {
            Debug.LogError("[TableManager] ActorTable load failed.");
            return false;
        }

        Debug.Log($"[TableManager] ActorTable loaded. Count: {_actorTableList.Count}");
        return true;
    }
}