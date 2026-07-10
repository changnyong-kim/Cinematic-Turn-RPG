using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public sealed class ParryRimRendererFeature : ScriptableRendererFeature
{
    [Serializable]
    private sealed class Settings
    {
        [SerializeField]
        private Material _overrideMaterial;

        [SerializeField]
        private RenderPassEvent _renderPassEvent =
            RenderPassEvent.AfterRenderingOpaques;

        [SerializeField]
        private uint _renderingLayerMask = 1u << 1;

        public Material OverrideMaterial => _overrideMaterial;
        public RenderPassEvent RenderPassEvent => _renderPassEvent;
        public uint RenderingLayerMask => _renderingLayerMask;
    }

    private sealed class ParryRimRenderPass : ScriptableRenderPass
    {
        private static readonly ShaderTagId UniversalForwardTag =
            new ShaderTagId("UniversalForward");

        private static readonly ShaderTagId UniversalForwardOnlyTag =
            new ShaderTagId("UniversalForwardOnly");

        private static readonly ShaderTagId SrpDefaultUnlitTag =
            new ShaderTagId("SRPDefaultUnlit");

        private sealed class PassData
        {
            public RendererListHandle RendererListHandle;
        }

        private readonly Settings _settings;

        public ParryRimRenderPass(Settings settings)
        {
            _settings = settings;
            renderPassEvent = settings.RenderPassEvent;
        }

        public override void RecordRenderGraph(
            RenderGraph renderGraph,
            ContextContainer frameContext)
        {
            Material overrideMaterial = _settings.OverrideMaterial;

            if (overrideMaterial == null)
            {
                return;
            }

            UniversalRenderingData renderingData =
                frameContext.Get<UniversalRenderingData>();

            UniversalCameraData cameraData =
                frameContext.Get<UniversalCameraData>();

            UniversalLightData lightData =
                frameContext.Get<UniversalLightData>();

            UniversalResourceData resourceData =
                frameContext.Get<UniversalResourceData>();

            SortingCriteria sortingCriteria =
                cameraData.defaultOpaqueSortFlags;

            DrawingSettings drawingSettings =
                RenderingUtils.CreateDrawingSettings(
                    UniversalForwardTag,
                    renderingData,
                    cameraData,
                    lightData,
                    sortingCriteria);

            drawingSettings.SetShaderPassName(
                1,
                UniversalForwardOnlyTag);

            drawingSettings.SetShaderPassName(
                2,
                SrpDefaultUnlitTag);

            drawingSettings.overrideMaterial = overrideMaterial;
            drawingSettings.overrideMaterialPassIndex = 0;

            FilteringSettings filteringSettings =
                new FilteringSettings(
                    RenderQueueRange.opaque,
                    layerMask: -1,
                    renderingLayerMask: _settings.RenderingLayerMask);

            RendererListParams rendererListParams =
                new RendererListParams(
                    renderingData.cullResults,
                    drawingSettings,
                    filteringSettings);

            using (var builder =
                renderGraph.AddRasterRenderPass<PassData>(
                    "Parry Rim Render Pass",
                    out PassData passData))
            {
                passData.RendererListHandle =
                    renderGraph.CreateRendererList(
                        rendererListParams);

                builder.UseRendererList(
                    passData.RendererListHandle);

                builder.SetRenderAttachment(
                    resourceData.activeColorTexture,
                    0,
                    AccessFlags.Write);

                builder.SetRenderAttachmentDepth(
                    resourceData.activeDepthTexture,
                    AccessFlags.Read);

                builder.SetRenderFunc(
                    static (
                        PassData data,
                        RasterGraphContext context) =>
                    {
                        context.cmd.DrawRendererList(
                            data.RendererListHandle);
                    });
            }
        }
    }

    [SerializeField]
    private Settings _settings = new Settings();

    private ParryRimRenderPass _renderPass;

    public override void Create()
    {
        _renderPass = new ParryRimRenderPass(_settings);
    }

    public override void AddRenderPasses(
        ScriptableRenderer renderer,
        ref RenderingData renderingData)
    {
        if (_settings.OverrideMaterial == null)
        {
            return;
        }

        renderer.EnqueuePass(_renderPass);
    }
}
