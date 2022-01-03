// https://forum.unity.com/threads/anisotropic-highlight-surface-shader.94270/
// 미완성, 검토중
Shader "Custom/BRDF/AnisotropicHighlight"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [HDR]_SpecColor("Specular", Color) = (0.2, 0.2, 0.2)
        _SpecPower("Specular Power", Float) = 10
        _AnisoMap ("Anisotropic Direction (RGB)", 2D) = "bump" {}
        _AnisoOffset ("Anisotropic Offset", Range(-1,1)) = -0.2
        _AnisoPower ("Anisotropic Power", Float) = 1
        _AnisoMultiplier ("Anisotropic Multiplier", Float) = 1
        _AnisoValue ("Anisotropic Value", Range(0, 1)) = 1
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
                float2 uv2          : TEXCOORD1;
                float3 normal       : TEXCOORD2;
                float3 viewDir      : TEXCOORD3;
                float3 lightDir     : TEXCOORD4;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_AnisoMap);
            SAMPLER(sampler_AnisoMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _AnisoMap_ST;
                half4 _SpecColor;
                half _SpecPower;
                half _AnisoOffset;
                half _AnisoPower;
                half _AnisoMultiplier;
                half _AnisoValue;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.uv2 = TRANSFORM_TEX(IN.uv, _AnisoMap);
                OUT.normal = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDir = normalize(_WorldSpaceCameraPos.xyz - TransformObjectToWorld(IN.positionOS.xyz));
                OUT.lightDir = normalize(_MainLightPosition.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 이걸 안 하면 버텍스 사이 픽셀 노멀의 길이가 1이 아닌 것들이 발생함.
                IN.normal = normalize(IN.normal);

                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                half4 anisoDir = SAMPLE_TEXTURE2D(_AnisoMap, sampler_AnisoMap, IN.uv2);
                anisoDir.rgb = UnpackNormal(anisoDir); // _AnisoMap 에 대해서 노멀맵 적용하고 Unpack을 해야하는지 검토 필요.
                float NdotL = saturate(dot(IN.normal, IN.lightDir));
                
                // Anisotropic
                float3 H = normalize(IN.lightDir + IN.viewDir);
                half HdotA = dot(normalize(IN.normal + anisoDir.rgb), H);
                float aniso = max(0, sin(radians((HdotA + _AnisoOffset) * 100)));
                aniso = pow(aniso, _AnisoPower) * _AnisoMultiplier;
                
                // BlinnPhong Specular
                half spec = saturate(dot(H, IN.normal));
                spec = pow(spec, _SpecPower);

                spec = lerp(spec, aniso, _AnisoValue); // 비등방성 스페큘러와 일반 스페큘러를 Lerp
                half3 specColor = spec * _SpecColor.rgb;

                half3 ambient = SampleSH(IN.normal);
                half3 lighting = NdotL * _MainLightColor.rgb + ambient;
                color.rgb *= lighting;
                color.rgb += specColor;

                return color;
            }
            ENDHLSL
        }
    }
}