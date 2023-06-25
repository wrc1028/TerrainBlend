Shader "Unlit/TerrainBlendPT"
{
    Properties
    {
        // BaseMap
        _FxFIDTex("4x4 ID Map", 2D) = "white" {}
        _FxFBlendTex("4x4 Blend Map", 2D) = "white" {}
        // Blend
        _FxFFillAreaColor ("Fill Area Color", Color) = (0.1, 0.5, 0.1, 0.3)
        _TerrainID ("地形ID", int) = 2
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

            float4 _FxFFillAreaColor;
            float4 _Channel;
            int _TerrainID;

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

            float4 frag (Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 idcol = SAMPLE_TEXTURE2D(_FxFIDTex, sampler_FxFIDTex, input.uv);
                half4 blendWeight = SAMPLE_TEXTURE2D(_FxFBlendTex, sampler_FxFBlendTex, input.uv);
                // r 存储 前两层ID g 为第三层
                half2 IDColorReal = floor(idcol.rg * 255 + half2(0.5, 0.5));
                half2 fron = floor(IDColorReal.rg * 0.0625);
                half2 last = IDColorReal.rg - 16 * fron;
                int3 index = int3(fron.x, last.x, fron.y);
                // Color channel filter
                float rChannel = (_TerrainID & (1 << index.x)) == 0 ? 0 : 1;
                float gChannel = (_TerrainID & (1 << index.y)) == 0 ? 0 : 1;
                float bChannel = (_TerrainID & (1 << index.z)) == 0 ? 0 : 1;
                // Decode Blend Map last.y 是否为第一种混合模式  idcol.b 是否为第二种混合模式
                half3 mixState = half3(last.y, idcol.b, 1 - last.y - idcol.b); // 只有一个通道为 1
                half3 weight_one = half3(1, 0, 0);
                half3 weight_two = half3(0, blendWeight.r, 1 - blendWeight.r);
                half3 weight_three = half3(blendWeight.g, blendWeight.b, 1 - blendWeight.g - blendWeight.b);
                half3 weight = mixState.x * weight_one * _Channel.r + mixState.y * weight_two * _Channel.g + mixState.z * weight_three * _Channel.b;
                half3 finalColor = weight.x * pow(_TerrainColor[index.x], 2.2) + 
                    weight.y * pow(_TerrainColor[index.y], 2.2) + 
                    weight.z * pow(_TerrainColor[index.z], 2.2);
                // Fill Area
                float blendValue = dot(float3(rChannel, gChannel, bChannel), weight);
                return float4(finalColor, 1);
            }

            ENDHLSL
        }
        Pass 
        {
            Tags{"LightMode" = "Forward"}

            HLSLPROGRAM

            #pragma multi_compile_instancing

            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_FxFIDTex);            SAMPLER(sampler_FxFIDTex);
            TEXTURE2D(_FxFBlendTex);         SAMPLER(sampler_FxFBlendTex);

            float4 _FxFFillAreaColor;
            float4 _Channel;

            static const float4 _TerrainColor[8] = 
            {
                float4(1, 0, 0, 1), float4(1, 1, 0, 1), float4(1, 0, 1, 1), float4(1, 1, 1, 1),
                float4(0, 1, 0, 1), float4(0, 1, 1, 1), float4(0, 0, 1, 1), float4(0.25, 0.25, 0.25, 1),
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
                return layerValue * 16 - 1;
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
                half4 idcol = SAMPLE_TEXTURE2D(_FxFIDTex, sampler_FxFIDTex, input.uv);
                half4 blendWeight = SAMPLE_TEXTURE2D(_FxFBlendTex, sampler_FxFBlendTex, input.uv);
                // r 存储 前两层ID g 为第三层
                int3 index = int3(Decode(idcol.x), Decode(idcol.y), Decode(idcol.z));
                // Decode Blend Map last.y 是否为第一种混合模式  idcol.b 是否为第二种混合模式
                uint4 layerMask = LayerMask(idcol.w);
                // half3 mixState = half3(idcol.w == 0 ? 1 : 0, idcol.w == 0.5 ? 1 : 0, idcol.w == 1 ? 1 : 0); // 只有一个通道为 1
                half3 weight_one = half3(1, 0, 0);
                half3 weight_two = half3(1 - blendWeight.r, blendWeight.r, 0);
                half3 weight_three = half3(1 - blendWeight.g - blendWeight.b, blendWeight.g, blendWeight.b);
                half3 weight = layerMask.x * weight_one * _Channel.r + layerMask.y * weight_two * _Channel.g + layerMask.z * weight_three * _Channel.b;
                half3 finalColor = weight.x * _TerrainColor[index.x % 8] + 
                    weight.y * _TerrainColor[index.y % 8] + 
                    weight.z * _TerrainColor[index.z % 8];

                return float4(finalColor / 2, 1);
            }

            ENDHLSL
        }
    }
}
