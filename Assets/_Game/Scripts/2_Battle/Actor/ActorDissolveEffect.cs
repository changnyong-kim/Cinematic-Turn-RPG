using DG.Tweening;
using System.Collections.Generic;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEngine;

public sealed class ActorDissolveEffect : MonoBehaviour
{
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");

    [Header("Renderer")]
    [SerializeField]
    private Renderer[] _renderers;

    [Header("Dissolve Material")]
    [SerializeField]
    private Material _dissolveMaterialTemplate;

    [Header("Dissolve")]
    [SerializeField]
    private float _duration = 2.0f;

    [SerializeField]
    private Ease _ease = Ease.InQuad;

    private readonly Dictionary<Renderer, Material[]> _originalMaterialMap = new Dictionary<Renderer, Material[]>();
    private readonly List<Material> _runtimeMaterials = new List<Material>();

    private Tween _dissolveTween;
    private bool _isInitialized;

    private void Awake()
    {
        if (_renderers != null && _renderers.Length > 0)
        {
            return;
        }

        List<Renderer> rendererList = new List<Renderer>();
        Renderer[] childRenderers = GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < childRenderers.Length; i++)
        {
            Renderer targetRenderer = childRenderers[i];

            if (targetRenderer == null)
            {
                continue;
            }

            if (targetRenderer is SkinnedMeshRenderer || targetRenderer is MeshRenderer)
            {
                rendererList.Add(targetRenderer);
            }
        }

        _renderers = rendererList.ToArray();
    }

    public void PlayDissolve()
    {
        if (_dissolveMaterialTemplate == null)
        {
            Debug.LogError("[ActorDissolveEffect] Dissolve material template is null.");
            return;
        }

        EnsureDissolveMaterials();

        _dissolveTween?.Kill();

        SetDissolveAmount(0f);

        _dissolveTween = DOTween.To(
            (value) =>
            {
                SetDissolveAmount(value);
            }, 
            0f, 1f, _duration).SetEase(_ease);
    }

    public void ResetDissolve()
    {
        _dissolveTween?.Kill();

        RestoreOriginalMaterials();
        ReleaseRuntimeMaterials();
        _isInitialized = false;
    }

    private void EnsureDissolveMaterials()
    {
        if (_isInitialized)
        {
            return;
        }

        if (_renderers == null)
        {
            return;
        }

        for (int i = 0; i < _renderers.Length; i++)
        {
            Renderer targetRenderer = _renderers[i];

            if (targetRenderer == null)
            {
                continue;
            }

            Material[] originalMaterials = targetRenderer.sharedMaterials;

            if (originalMaterials == null || originalMaterials.Length == 0)
            {
                continue;
            }

            _originalMaterialMap[targetRenderer] = originalMaterials;

            Material[] dissolveMaterials = new Material[originalMaterials.Length];

            for (int j = 0; j < originalMaterials.Length; j++)
            {
                Material originalMaterial = originalMaterials[j];

                if (originalMaterial == null)
                {
                    continue;
                }

                Material runtimeMaterial = new Material(_dissolveMaterialTemplate);
                runtimeMaterial.name = $"{originalMaterial.name}_RuntimeDissolve";

                CopyCommonProperties(originalMaterial, runtimeMaterial);

                dissolveMaterials[j] = runtimeMaterial;
                _runtimeMaterials.Add(runtimeMaterial);
            }

            targetRenderer.sharedMaterials = dissolveMaterials;
        }

        _isInitialized = true;
    }

    private void CopyCommonProperties(Material source, Material target)
    {
        CopyTexture(source, target, BaseMapId);
    }

    private void CopyTexture(Material source, Material target, int propertyId)
    {
        if (source.HasProperty(propertyId) == false)
        {
            return;
        }

        if (target.HasProperty(propertyId) == false)
        {
            return;
        }

        target.SetTexture(propertyId, source.GetTexture(propertyId));
    }

    private void SetDissolveAmount(float amount)
    {
        for (int i = 0; i < _runtimeMaterials.Count; i++)
        {
            Material material = _runtimeMaterials[i];

            if (material == null)
            {
                continue;
            }

            if (material.HasProperty(DissolveAmountId) == false)
            {
                continue;
            }

            material.SetFloat(DissolveAmountId, amount);
        }
    }

    private void RestoreOriginalMaterials()
    {
        foreach (var pair in _originalMaterialMap)
        {
            Renderer targetRenderer = pair.Key;

            if (targetRenderer == null)
            {
                continue;
            }

            targetRenderer.sharedMaterials = pair.Value;
        }

        _originalMaterialMap.Clear();
    }

    private void ReleaseRuntimeMaterials()
    {
        for (int i = 0; i < _runtimeMaterials.Count; i++)
        {
            Material material = _runtimeMaterials[i];

            if (material == null)
            {
                continue;
            }

            Destroy(material);
        }

        _runtimeMaterials.Clear();
    }

    private void OnDestroy()
    {
        _dissolveTween?.Kill();

        RestoreOriginalMaterials();
        ReleaseRuntimeMaterials();
    }
}
