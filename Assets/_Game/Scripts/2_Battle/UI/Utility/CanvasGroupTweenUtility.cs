using DG.Tweening;
using UnityEngine;

public static class CanvasGroupTweenUtility
{
    public static Tween FadeIn(
        CanvasGroup canvasGroup,
        float duration,
        Ease ease = Ease.OutQuad,
        bool enableInputOnComplete = true)
    {
        if (canvasGroup == null)
        {
            return null;
        }

        canvasGroup.gameObject.SetActive(true);
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        Tween tween = canvasGroup
            .DOFade(1f, duration)
            .SetEase(ease);

        tween.OnComplete(() =>
        {
            if (enableInputOnComplete)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        });

        return tween;
    }

    public static Tween FadeOut(
        CanvasGroup canvasGroup,
        float duration,
        Ease ease = Ease.InQuad,
        bool disableInputImmediately = true,
        bool deactivateOnComplete = false)
    {
        if (canvasGroup == null)
        {
            return null;
        }

        if (disableInputImmediately)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        Tween tween = canvasGroup
            .DOFade(0f, duration)
            .SetEase(ease);

        tween.OnComplete(() =>
        {
            canvasGroup.alpha = 0f;

            if (deactivateOnComplete)
            {
                canvasGroup.gameObject.SetActive(false);
            }
        });

        return tween;
    }

    public static Tween FadeTo(
        CanvasGroup canvasGroup,
        float targetAlpha,
        float duration,
        Ease ease = Ease.OutQuad)
    {
        if (canvasGroup == null)
        {
            return null;
        }

        return canvasGroup
            .DOFade(Mathf.Clamp01(targetAlpha), duration)
            .SetEase(ease);
    }

    public static void SetVisibleImmediately(
        CanvasGroup canvasGroup,
        bool visible,
        bool controlInput = true)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = visible ? 1f : 0f;

        if (controlInput)
        {
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
    }
}
