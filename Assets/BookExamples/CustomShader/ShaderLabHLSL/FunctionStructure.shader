// 이것은 주석입니다. (Comment)
Shader "Custom/ShaderLab HLSL/FunctionStructure"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _Test("Test", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            half _Test;

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
            CBUFFER_END

            half4 FunctionTest(half4 color, half test)
            {
                half4 retCol;
                retCol = color * test;
                return retCol;
            }

            float2 TransformUV(float2 inUV, float4 tilingOffset)
            {
                float2 retUV;
                retUV = inUV * tilingOffset.xy;
                retUV += tilingOffset.zw;
                return retUV;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                //OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.uv = TransformUV(IN.uv, _BaseMap_ST);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                color = FunctionTest(color, _Test);
                return color;
            }
            ENDHLSL
        }
    }
}