using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

[System.Serializable]
public sealed class BattleSkillTableData
{
    [SerializeField]
    [JsonProperty("id")]
    [JsonConverter(typeof(StringEnumConverter))]
    private BattleSkillId _id;

    [SerializeField]
    [JsonProperty("name")]
    private string _name;

    [SerializeField]
    [JsonProperty("powerRate")]
    private float _powerRate;

    [SerializeField]
    [JsonProperty("applyStatus")]
    [JsonConverter(typeof(StringEnumConverter))]
    private ActorStatusType _applyStatus;

    [SerializeField]
    [JsonProperty("noticeText")]
    private string _noticeText;

    [SerializeField]
    [JsonProperty("canParry")]
    private bool _canParry;

    [SerializeField]
    [JsonProperty("cinematicKey")]
    private string _cinematicKey;

    public BattleSkillId Id => _id;
    public string Name => _name;
    public float PowerRate => _powerRate;
    public ActorStatusType ApplyStatus => _applyStatus;
    public string NoticeText => _noticeText;
    public bool CanParry => _canParry;
    public string CinematicKey => _cinematicKey;
}