using Newtonsoft.Json;
using UnityEngine;

[System.Serializable]
public class ActorTableData
{
    [SerializeField]
    [JsonProperty("id")]
    private int _id;

    [SerializeField]
    [JsonProperty("type")]
    private string _type;

    [SerializeField]
    [JsonProperty("name")]
    private string _name;

    [SerializeField]
    [JsonProperty("prefabKey")]
    private string _prefabKey;

    public int Id => _id;
    public string Type => _type;
    public string Name => _name;
    public string PrefabKey => _prefabKey;
}