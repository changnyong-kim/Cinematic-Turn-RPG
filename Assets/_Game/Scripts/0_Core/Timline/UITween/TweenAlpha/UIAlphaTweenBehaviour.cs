using UnityEngine;
using UnityEngine.Playables;

public sealed class UIAlphaTweenBehaviour : PlayableBehaviour
{
    public float FromAlpha
    {
        get;
        set;
    }

    public float ToAlpha
    {
        get;
        set;
    } = 1f;

    public AnimationCurve EaseCurve
    {
        get;
        set;
    }

    public bool RestoreOnPause
    {
        get;
        set;
    }

    public float RestoreAlpha
    {
        get;
        set;
    }

    private CanvasGroup _canvasGroup;

    public override void ProcessFrame(
        Playable playable,
        FrameData info,
        object playerData)
    {
        _canvasGroup = playerData as CanvasGroup;

        if (_canvasGroup == null)
        {
            return;
        }

        double duration = playable.GetDuration();

        if (duration <= 0.0001f)
        {
            _canvasGroup.alpha = ToAlpha;
            return;
        }

        float normalizedTime = Mathf.Clamp01(
            (float)(playable.GetTime() / duration));

        float easedTime = EaseCurve != null
            ? EaseCurve.Evaluate(normalizedTime)
            : normalizedTime;

        _canvasGroup.alpha = Mathf.Lerp(
            FromAlpha,
            ToAlpha,
            easedTime);
    }

    public override void OnBehaviourPause(
    Playable playable,
    FrameData info)
    {
        if (RestoreOnPause == false)
        {
            return;
        }

        if (_canvasGroup == null)
        {
            return;
        }

        _canvasGroup.alpha = RestoreAlpha;
    }
}