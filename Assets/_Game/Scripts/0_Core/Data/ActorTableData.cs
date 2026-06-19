using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

[System.Serializable]
public class ActorTableData
{
    [SerializeField]
    [JsonProperty("id")]
    private int _id;

    [SerializeField]
    [JsonProperty("type")]
    [JsonConverter(typeof(StringEnumConverter))]
    private ActorType _type;

    [SerializeField]
    [JsonProperty("name")]
    private string _name;

    [SerializeField]
    [JsonProperty("prefabKey")]
    private string _prefabKey;

    [SerializeField]
    [JsonProperty("maxHp")]
    private int _maxHp;

    [SerializeField]
    [JsonProperty("attack")]
    private int _attack;


    public int Id => _id;
    public ActorType Type => _type;
    public string Name => _name;
    public string PrefabKey => _prefabKey;
    public int MaxHp => _maxHp;
    public int Attack => _attack;
}