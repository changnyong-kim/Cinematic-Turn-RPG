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

    private List<BattleSkillTableData> _battleSkillTableList = new List<BattleSkillTableData>();
    private readonly Dictionary<BattleSkillId, BattleSkillTableData> _battleSkillTableMap = new Dictionary<BattleSkillId, BattleSkillTableData>();


    public IReadOnlyList<ActorTableData> ActorTableList => _actorTableList;
    public IReadOnlyList<BattleSkillTableData> BattleSkillTableList => _battleSkillTableList;


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

        _battleSkillTableList = JsonLoader.LoadSkillTable<BattleSkillTableData>(_loadConfig);

        if (_battleSkillTableList == null)
        {
            Debug.LogError("[TableManager] BbattleSkillTable load failed.");
            return false;
        }

        BuildBattleSkillTableMap(_battleSkillTableList);

        Debug.Log($"[TableManager] BbattleSkillTable loaded. Count: {_battleSkillTableList.Count}");

        return true;
    }

    public BattleSkillTableData GetBattleSkill(BattleSkillId skillId)
    {
        if (skillId == BattleSkillId.None)
        {
            Debug.LogError("[TableManager] Invalid BattleSkillId. SkillId is None.");
            return null;
        }

        if (_battleSkillTableMap.TryGetValue(skillId, out BattleSkillTableData skillData))
        {
            return skillData;
        }

        Debug.LogError($"[TableManager] BattleSkill not found. SkillId: {skillId}");
        return null;
    }

    private void BuildBattleSkillTableMap(IReadOnlyList<BattleSkillTableData> tableList)
    {
        for (int i = 0; i < tableList.Count; i++)
        {
            BattleSkillTableData data = tableList[i];

            if (data == null)
            {
                continue;
            }

            if (_battleSkillTableMap.ContainsKey(data.Id))
            {
                Debug.LogError($"[TableManager] Duplicated BattleSkillId. Id: {data.Id}");
                continue;
            }

            _battleSkillTableMap.Add(data.Id, data);
        }
    }
}