using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Pencil_4.URP
{
    public class PencilLineRendererFeature : ScriptableRendererFeature
    {
        public RenderPassEvent Event = RenderPassEvent.BeforeRenderingTransparents;

        private RenderPass _pass;

        public override void Create()
        {
            _pass = new RenderPass(Event);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.isSceneViewCamera)
            {
                return;
            }

#if UNITY_EDITOR
            if (!EditorApplication.isPlaying && RenderMode.GameViewRenderMode == RenderMode.Mode.Off)
            {
                return;
            }
#endif
            renderer.EnqueuePass(_pass);
        }

        class RenderPass : ScriptableRenderPass
        {
            static readonly string k_renderTag = "Render Pencil+ Line (URP)";
            Material _material;
            Material material => _material ?? (_material = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/Pcl4LineURP")));

            public RenderPass(RenderPassEvent evt)
            {
                renderPassEvent = evt;
            }

            Type GetLineComponentType()
            {
                switch(renderPassEvent)
                {
                    case RenderPassEvent.BeforeRenderingTransparents:
                        return typeof(PencilLine);
                    case RenderPassEvent.BeforeRenderingPostProcessing:
                        return typeof(PencilLine_BeforePostProcess);
                    case RenderPassEvent.AfterRenderingPostProcessing:
                        return typeof(PencilLine_AfterPostProcess);
                }

                return null;
            }

#if UNITY_6000_0_OR_NEWER
            private class PassData
            {
                internal TextureHandle activeColorBuffer;
                internal Type lineComponentType;
                internal Material material;
                internal Camera camera;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
            {
                using (var builder = renderGraph.AddUnsafePass<PassData>(k_renderTag, out var passData))
                {
                    UniversalCameraData cameraData = frameContext.Get<UniversalCameraData>();
                    passData.camera = cameraData.camera;
                    passData.lineComponentType = GetLineComponentType();
                    passData.material = material;

                    UniversalResourceData resourceData = frameContext.Get<UniversalResourceData>();
                    passData.activeColorBuffer = resourceData.activeColorTexture;
                    builder.UseTexture(passData.activeColorBuffer, AccessFlags.Write);

                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc((PassData data, UnsafeGraphContext context) => ExecutePass(data, context));
                }
            }

            static void ExecutePass(PassData passData, UnsafeGraphContext context)
            {
                var lineComponent = VolumeManager.instance.stack.GetComponent(passData.lineComponentType) as PencilLineBase;
                if (!lineComponent || !lineComponent.IsActive())
                {
                    return;
                }

                CommandBuffer unsafeCommandBuffer = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
                lineComponent.Render(unsafeCommandBuffer, passData.camera, passData.activeColorBuffer, passData.material);
            }
        }
#else
            RenderTargetIdentifier _renderTarget;

            public void Setup(RenderTargetIdentifier renderTarget)
            {
                _renderTarget = renderTarget;
                if (!_material)
                {
                    _material = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/Pcl4LineURP"));
                }
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (!_material || !renderingData.cameraData.postProcessEnabled)
                {
                    return;
                }

                var type = GetLineComponentType();
                if (type == null)
                {
                    return;
                }
                var lineComponent = VolumeManager.instance.stack.GetComponent(type) as PencilLineBase;
                if (!lineComponent || !lineComponent.IsActive())
                {
                    return;
                }

                var cmd = CommandBufferPool.Get(k_renderTag);

                var cameraTarget = _renderTarget;
                if (renderPassEvent == RenderPassEvent.AfterRenderingPostProcessing)
                {
                    ref CameraData cameraData = ref renderingData.cameraData;
                    cameraTarget = (cameraData.targetTexture != null) ? new RenderTargetIdentifier(cameraData.targetTexture) : BuiltinRenderTextureType.CameraTarget;
                }

                lineComponent.Render(cmd, renderingData.cameraData.camera, cameraTarget, _material);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
#if UNITY_2018_3_OR_NEWER
            _pass.Setup(renderer.cameraColorTargetHandle);
#else
            _pass.Setup(renderer.cameraColorTarget);
#endif
        }
#endif
    }
}