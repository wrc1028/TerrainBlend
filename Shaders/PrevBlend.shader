Shader "Unlit/PrevBlend"
{
    Properties
    {
        // BaseMap
        _FxFIDTex("4x4 ID Map", 2D) = "white" {}
        _FxFBlendTex("4x4 Blend Map", 2D) = "white" {}
        _Mask ("Mask", 2D) = "white" {}
        // Blend
        _Channel ("通道", vector) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass 
        {
            Tags{"LightMode" = "UniversalForward"}

            HLSLPROGRAM

            #pragma multi_compile_instancing

            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_FxFIDTex);            SAMPLER(sampler_FxFIDTex);
            TEXTURE2D(_FxFBlendTex);         SAMPLER(sampler_FxFBlendTex);
            TEXTURE2D(_Mask);                SAMPLER(sampler_Mask);

            float4 _Channel;

            static const float4 _TerrainColor[16] = 
            {
                float4(0.5, 0.5, 0.5, 1), float4(1, 0, 0, 1), float4(1, 1, 0, 1), float4(1, 0, 1, 1), 
                float4(1, 1, 1, 1),       float4(0, 1, 0, 1), float4(0, 1, 1, 1), float4(0, 0, 1, 1), 
                float4(1, 0.5, 0.5, 1),   float4(1, 0, 0.5, 1), float4(1, 1, 0.5, 1), float4(1, 0.5, 1, 1), 
                float4(0.5, 0.5, 1, 1),   float4(0.5, 1, 0.5, 1), float4(0.5, 1, 1, 1), float4(0.5, 0, 1, 1), 
            };
            

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 texcoord   : TEXCOORD0;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            
            Varyings vert (Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.texcoord;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }
            int Decode(float layerValue)
            {
                return floor(layerValue * 32) - 1;
            }

            float4 frag (Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                // input.uv.x = 1 - input.uv.x;
                half4 blendWeight = SAMPLE_TEXTURE2D(_FxFBlendTex, sampler_FxFBlendTex, input.uv);
                half4 idcol = SAMPLE_TEXTURE2D(_FxFIDTex, sampler_FxFIDTex, input.uv);
                half4 maskA = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, input.uv);
                int4  idValue = floor(idcol * 255);
                int4  layerIndex = idValue >> 4;
                int4  layerMask = idValue - layerIndex * 16;
                half3 blendStruct = half3(layerMask.w, layerIndex.w, 1 - layerMask.w - layerIndex.w);
                blendStruct.z = (1 - layerMask.x) * _Channel.w * blendStruct.z;
                half3 weight_one = half3(1, 0, 0);
                half3 weight_two = half3(1 - blendWeight.r, blendWeight.r, 0);
                blendWeight.g = blendWeight.g * layerMask.r;
                // blendWeight.g = lerp(blendWeight.a, blendWeight.g, layerMask.g);
                // blendWeight.b = lerp(blendWeight.a, blendWeight.b, layerMask.b);
                half3 weight_three = half3(1 - blendWeight.g - blendWeight.b, blendWeight.g, blendWeight.b);
                half3 weight = blendStruct.x * weight_one * _Channel.r + blendStruct.y * weight_two * _Channel.g + blendStruct.z * weight_three * _Channel.b;
                half3 finalColor = weight.x * pow(_TerrainColor[layerIndex.x], 2.2) + 
                    weight.y * pow(_TerrainColor[layerIndex.y], 2.2) + 
                    weight.z * pow(_TerrainColor[layerIndex.z], 2.2);
                return float4(finalColor, 1) + (1 - maskA.r);
            }

            ENDHLSL
        }
    }
}
