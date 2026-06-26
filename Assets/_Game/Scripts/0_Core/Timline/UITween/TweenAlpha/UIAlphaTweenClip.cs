using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[System.Serializable]
public sealed class UIAlphaTweenClip : PlayableAsset, ITimelineClipAsset
{
    [Range(0f, 1f)]
    public float _fromAlpha = 0f;

    [Range(0f, 1f)]
    public float _toAlpha = 1f;

    public AnimationCurve EaseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    #region 강제 종료 또는 일반 종료시 처리
    [SerializeField]
    private bool _restoreOnPause = true;

    [SerializeField]
    [Range(0f, 1f)]
    private float _restoreAlpha = 0f;
    #endregion

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        ScriptPlayable<UIAlphaTweenBehaviour> playable =
            ScriptPlayable<UIAlphaTweenBehaviour>.Create(graph);

        UIAlphaTweenBehaviour behaviour = playable.GetBehaviour();

        behaviour.FromAlpha = _fromAlpha;
        behaviour.ToAlpha = _toAlpha;
        behaviour.EaseCurve = EaseCurve;
        behaviour.RestoreOnPause = _restoreOnPause;
        behaviour.RestoreAlpha = _restoreAlpha;

        return playable;
    }
}