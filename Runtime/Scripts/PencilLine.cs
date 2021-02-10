using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Pencil_4.URP
{
    public sealed class PencilLine : PencilLineBase
    { }

    public abstract class PencilLineBase : VolumeComponent, IPostProcessComponent
    {
        public ClampedFloatParameter alpha = new ClampedFloatParameter(0f, 0f, 1f);

        public bool IsActive() => alpha.value > 0;

        public bool IsTileCompatible() => false;

        internal void Render(CommandBuffer cmd, Camera camera, RenderTargetIdentifier renderTarget, Material material)
        {
            //cmd.ClearRenderTarget(false, true, Color.green);

            foreach (var lineEffect in camera.GetComponents<PencilLineEffect>())
            {
                if (lineEffect.PencilRenderer != null && lineEffect.PencilRenderer.Texture != null && lineEffect.isPostProsessingEnabled)
                {
                    // テクスチャ更新設定
                    if (lineEffect.isRendering == true)
                    {
#if UNITY_2018_3_OR_NEWER
                        var callback = NativeFunctions.GetTextureUpdateCallbackV2();
#else
                        var callback = NativeFunctions.GetTextureUpdateCallback();
#endif
                        if (callback == IntPtr.Zero)
                        {
                            continue;
                        }

                        // ハンドルを取得し、ネイティブで確保したバッファが意図せず解放されないようにする
                        // ハンドルはTextureUpdateCallback()のEndで自動的に解除される
                        var textureUpdateHandle = lineEffect.PencilRenderer.RequestTextureUpdate(0);
                        if (textureUpdateHandle == 0xFFFFFFFF)
                        {
                            // PencilLinePostProcessRenderer.Render()の呼び出しがlineEffect.OnPreRender()よりも早いケースが稀にあり、
                            // PostProcessing_RenderingEventモードのときに適切なライン描画が行われない場合がある
                            continue;
                        }
#if UNITY_2018_3_OR_NEWER
                        cmd.IssuePluginCustomTextureUpdateV2(callback, lineEffect.PencilRenderer.Texture, textureUpdateHandle);
#else
                        cmd.IssuePluginCustomTextureUpdate(callback, lineEffect.PencilRenderer.Texture, textureUpdateHandle);
#endif
                        // レンダーエレメント画像出力用のテクスチャ更新
                        for (int renderElementIndex = 0; true; renderElementIndex++)
                        {
                            var renderElementTexture = lineEffect.PencilRenderer.GetRenderElementTexture(renderElementIndex);
                            var renderElementTargetTexture = lineEffect.PencilRenderer.GetRenderElementTargetTexture(renderElementIndex);
                            if (renderElementTexture == null || renderElementTargetTexture == null)
                            {
                                break;
                            }

                            textureUpdateHandle = lineEffect.PencilRenderer.RequestTextureUpdate(1 + renderElementIndex);
                            if (textureUpdateHandle == 0xFFFFFFFF)
                            {
                                break;
                            }

#if UNITY_2018_3_OR_NEWER
                            cmd.IssuePluginCustomTextureUpdateV2(callback, renderElementTexture, textureUpdateHandle);
#else
                            cmd.IssuePluginCustomTextureUpdate(callback, renderElementTexture, textureUpdateHandle);
#endif
                            cmd.Blit(renderElementTexture, renderElementTargetTexture);
                        }
                    }

                    // 描画設定
                    cmd.SetGlobalFloat("_Alpha", alpha.value);
                    cmd.Blit(lineEffect.PencilRenderer.Texture, renderTarget, material);
                }
            }
        }
    }
}