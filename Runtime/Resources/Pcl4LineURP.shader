Shader "Hidden/Pcl4LineURP"
{
	HLSLINCLUDE
	ENDHLSL

	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags{ "RenderPipeline" = "UniversalPipeline" }

		Pass
		{
			ZWrite Off ZTest Always Cull Off
			Blend SrcAlpha OneMinusSrcAlpha, OneMinusDstAlpha One

			HLSLPROGRAM

			#pragma vertex Vert
			#pragma fragment Frag

			#pragma target 4.5
			#pragma editor_sync_compilation
			#pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"

			TEXTURE2D_X(_MainTex);
			float4 _MainTex_TexelSize;

			float _Alpha;

			float4 Load(int2 icoords, int idx, int idy)
			{
#if SHADER_API_GLES
				float2 uv = (icoords + int2(idx, idy)) * _MainTex_TexelSize.xy;
				return SAMPLE_TEXTURE2D_X(_MainTex, sampler_LinearClamp, uv);
#else
				return LOAD_TEXTURE2D_X(_MainTex, clamp(icoords + int2(idx, idy), 0, _MainTex_TexelSize.zw - 1.0));
#endif
			}

			float4 Frag(Varyings input) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				float2 uv = UnityStereoTransformScreenSpaceTex(input.uv);
				int2   positionSS = uv * _MainTex_TexelSize.zw;

				float4 lineColor = Load(positionSS, 0, 0);
				lineColor.rgb /= max(lineColor.a, 1e-20);
				lineColor.a *= _Alpha;
				return lineColor;
			}

			ENDHLSL
		}
	}
	Fallback Off

}