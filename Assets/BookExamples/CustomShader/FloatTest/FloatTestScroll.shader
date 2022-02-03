// Float 자료형 정밀도 테스트용 셰이더

Shader "Custom/Float Test Scroll"
{
    Properties
    { 
        [MainTexture] _BaseMap("Base Map", 2D) = "white"
        _BigNumber("BigNumber", Float) = 0
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

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float _BigNumber;
            CBUFFER_END
            
            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                uv.x += frac(_Time.x + _BigNumber);
                uv.x = frac(uv.x);

                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                return color;
            }
            ENDHLSL
        }
    }
}