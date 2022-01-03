Shader "Custom/BRDF/BlinnPhongVertexShader"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [HDR]_SpecColor("Specular", Color) = (0.2, 0.2, 0.2)
        _SpecPower("Specular Power", Float) = 10
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                half3 lighting      : COLOR0;
                half3 specColor     : COLOR1;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _SpecColor;
                half _SpecPower;
            CBUFFER_END

            // Lambert 라이팅 연산을 대부분 버택스 셰이더에서 수행
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float3 lightDir = normalize(_MainLightPosition.xyz);
                half ndotl = saturate(dot(normalWS, lightDir));
                half3 ambient = SampleSH(normalWS);
                OUT.lighting = ndotl * _MainLightColor.rgb + ambient;

                // Vertex Specular
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - positionWS);
                float3 H = normalize(lightDir + viewDir);
                half spec = saturate(dot(H, normalWS));
                spec = pow(spec, _SpecPower);
                OUT.specColor = spec * _SpecColor.rgb;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                color.rgb *= IN.lighting;
                color.rgb += IN.specColor;

                return color;
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}