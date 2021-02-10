using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
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
            _pass.Setup(renderer.cameraColorTarget);
            renderer.EnqueuePass(_pass);
        }

        class RenderPass : ScriptableRenderPass
        {
            static readonly string k_renderTag = "Render Pencil+ Line (URP)";
            Material _material;
            RenderTargetIdentifier _renderTarget;

            public RenderPass(RenderPassEvent evt)
            {
                renderPassEvent = evt;
            }

            public void Setup(RenderTargetIdentifier renderTarget)
            {
                _renderTarget = renderTarget;
                if (!_material)
                {
                    _material = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/Pcl4LineURP"));
                }
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
    }
}