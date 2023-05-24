Shader "Unlit/PrevBlend"
{
    Properties
    {
        // BaseMap
        _FxFIDTex("4x4 ID Map", 2D) = "white" {}
        _FxFBlendTex("4x4 Blend Map", 2D) = "white" {}
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

            float4 _Channel;

            static const float4 _TerrainColor[16] = 
            {
                float4(0.5, 0.5, 0.5, 1), float4(1, 0, 0, 1), float4(1, 1, 0, 1), float4(1, 0, 1, 1), 
                float4(1, 1, 1, 1),       float4(0, 1, 0, 1), float4(0, 1, 1, 1), float4(0, 0, 1, 1), 
                float4(1, 0.5, 0.5, 1), float4(1, 0, 0.5, 1), float4(1, 1, 0.5, 1), float4(1, 0.5, 1, 1), 
                float4(0.5, 0.5, 1, 1),       float4(0.5, 1, 0.5, 1), float4(0.5, 1, 1, 1), float4(0.5, 0, 1, 1), 
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
            int4 LayerMask(float mask)
            {
                uint4 tempLayersCount = 0;
                if (mask == 0)       tempLayersCount = uint4(1, 0, 0, 0);
                else if (mask == 1)  tempLayersCount = uint4(0, 0, 0, 1);
                else if (mask < 0.5) tempLayersCount = uint4(0, 1, 0, 0);
                else if (mask > 0.5) tempLayersCount = uint4(0, 0, 1, 0);
                return tempLayersCount;
            }
            float4 frag (Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                // input.uv.x = 1 - input.uv.x;
                half4 idcol = SAMPLE_TEXTURE2D(_FxFIDTex, sampler_FxFIDTex, input.uv);
                half4 blendWeight = pow(SAMPLE_TEXTURE2D(_FxFBlendTex, sampler_FxFBlendTex, input.uv), 1);
                // r 存储 前两层ID g 为第三层
                int3 index = int3(Decode(idcol.x), Decode(idcol.y), Decode(idcol.z));
                // Decode Blend Map last.y 是否为第一种混合模式  idcol.b 是否为第二种混合模式
                uint4 layerMask = LayerMask(idcol.w);
                // half3 mixState = half3(idcol.w == 0 ? 1 : 0, idcol.w == 0.5 ? 1 : 0, idcol.w == 1 ? 1 : 0); // 只有一个通道为 1
                half3 weight_one = half3(1, 0, 0);
                half3 weight_two = half3(1 - blendWeight.r, blendWeight.r, 0);
                half3 weight_three = half3(1 - blendWeight.g - blendWeight.b, blendWeight.g, blendWeight.b);
                half3 weight = layerMask.x * weight_one * _Channel.r + layerMask.y * weight_two * _Channel.g + layerMask.z * weight_three * _Channel.b;
                half3 finalColor = weight.x * pow(_TerrainColor[index.x], 2.2) + 
                    weight.y * pow(_TerrainColor[index.y], 2.2) + 
                    weight.z * pow(_TerrainColor[index.z], 2.2);

                return float4(finalColor, 1);
            }

            ENDHLSL
        }
    }
}
