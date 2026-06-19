using UnityEngine;

[CreateAssetMenu(fileName = "LoadConfig", menuName = "Scriptable Objects/LoadConfig")]
public class LoadConfig : ScriptableObject
{
    [Header("Table Paths")]
    [SerializeField] private string _actorTablePath;
    [SerializeField] private string _skillTablePath;

    [Header("Addressable Labels")]
    [SerializeField] private string _actorLabel;
    [SerializeField] private string _skillLabel;
    [SerializeField] private string _backgroundLabel;

    public string ActorTablePath => _actorTablePath;
    public string SkillTablePath => _skillTablePath;

    public string ActorLabel => _actorLabel;
    public string SkillLabel => _skillLabel;
    public string BackgroundLabel => _backgroundLabel;
}
