using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace TerrainBlend16
{
    [ExecuteInEditMode]
    public class TerrainBlend : MonoBehaviour
    {
        public TerrainBlendAsset m_TerrainAsset;
        public int intValue;
        public void UpdateTerrainBlend()
        {
            if (m_TerrainAsset == null || BlendShaderUtils.s_Shader == null) return;
            int threadGroups = Mathf.CeilToInt(m_TerrainAsset.m_AlphamapResolution / 8);
            BlendShaderUtils.s_Shader.SetInts(BlendShaderUtils.s_TerrainParamsID, m_TerrainAsset.m_LayersCount, m_TerrainAsset.m_AlphamapResolution, 0);
            // Raw ID Texture: 输出原始的IDTexture贴图
            ComputeBuffer indexRank = CreateAndGetBuffer(m_TerrainAsset.m_CoverageIndexRank, sizeof(uint));
            BlendShaderUtils.s_Shader.SetBuffer(BlendShaderUtils.s_RawIDTextureKernel, BlendShaderUtils.s_IndexRankID, indexRank);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_RawIDTextureKernel, BlendShaderUtils.s_RawIDMaskArrayID, m_TerrainAsset.m_RawIDMaskArray);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_RawIDTextureKernel, BlendShaderUtils.s_AlphaTextureArrayID, m_TerrainAsset.m_AlphaTextureArray);
            RenderTexture m_RawIDTexture = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_RawIDTextureKernel, BlendShaderUtils.s_IDResultID, m_RawIDTexture);
            BlendShaderUtils.s_Shader.Dispatch(BlendShaderUtils.s_RawIDTextureKernel, threadGroups, threadGroups, 1);
            Utils.SaveRT2Texture(m_RawIDTexture, TextureFormat.RGBA32, GetTextureSavePath("1RawIDTexture.tga", m_TerrainAsset.m_TerrainName));
            // Check ID Layer Edge: 边界衔接处混合值过大的问题, 将其进行分离开
            // CShaderUtils.s_Shader.SetVector(CShaderUtils.s_ExtendParamsID, new Vector4(0, 1, 0, 3));
            // CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_CheckIDLayerEdgeKernel, CShaderUtils.s_RawIDTextureID, m_RawIDTexture);
            // CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_CheckIDLayerEdgeKernel, CShaderUtils.s_AlphaTextureArrayID, m_TerrainAsset.m_AlphaTextureArray);
            // RenderTexture m_RawIDEdgeTextureTD = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            // CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_CheckIDLayerEdgeKernel, CShaderUtils.s_IDResultID, m_RawIDEdgeTextureTD);
            // CShaderUtils.s_Shader.Dispatch(CShaderUtils.s_CheckIDLayerEdgeKernel, threadGroups, threadGroups, 1);
            // Utils.SaveRT2Texture(m_RawIDEdgeTextureTD, TextureFormat.RGBA32, GetTextureSavePath("2RawIDEdgeTextureTD.tga", m_TerrainAsset.m_TerrainName));

            // CShaderUtils.s_Shader.SetVector(CShaderUtils.s_ExtendParamsID, new Vector4(0, 0, 1, 3));
            // CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_CheckIDLayerEdgeKernel, CShaderUtils.s_RawIDTextureID, m_RawIDEdgeTextureTD);
            // CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_CheckIDLayerEdgeKernel, CShaderUtils.s_AlphaTextureArrayID, m_TerrainAsset.m_AlphaTextureArray);
            // RenderTexture m_RawIDEdgeTextureTT = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            // CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_CheckIDLayerEdgeKernel, CShaderUtils.s_IDResultID, m_RawIDEdgeTextureTT);
            // CShaderUtils.s_Shader.Dispatch(CShaderUtils.s_CheckIDLayerEdgeKernel, threadGroups, threadGroups, 1);
            // Utils.SaveRT2Texture(m_RawIDEdgeTextureTT, TextureFormat.RGBA32, GetTextureSavePath("2RawIDEdgeTextureTT.tga", m_TerrainAsset.m_TerrainName));

            BlendShaderUtils.s_Shader.SetVector(BlendShaderUtils.s_ExtendParamsID, new Vector4(0, 1, 0, 2));
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_CheckIDLayerEdgeKernel, BlendShaderUtils.s_RawIDTextureID, m_RawIDTexture);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_CheckIDLayerEdgeKernel, BlendShaderUtils.s_AlphaTextureArrayID, m_TerrainAsset.m_AlphaTextureArray);
            RenderTexture m_RawIDEdgeTexture = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_CheckIDLayerEdgeKernel, BlendShaderUtils.s_IDResultID, m_RawIDEdgeTexture);
            BlendShaderUtils.s_Shader.Dispatch(BlendShaderUtils.s_CheckIDLayerEdgeKernel, threadGroups, threadGroups, 1);
            Utils.SaveRT2Texture(m_RawIDEdgeTexture, TextureFormat.RGBA32, GetTextureSavePath("2RawIDEdgeTexture.tga", m_TerrainAsset.m_TerrainName));
            // ID Layer Extend: 向外扩展一格, 用来解决线性插值产生的过渡条
            BlendShaderUtils.s_Shader.SetVector(BlendShaderUtils.s_ExtendParamsID, new Vector4(0, 1, 0, 2));
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_IDLayerExtendKernel, BlendShaderUtils.s_RawIDTextureID, m_RawIDEdgeTexture);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_IDLayerExtendKernel, BlendShaderUtils.s_AlphaTextureArrayID, m_TerrainAsset.m_AlphaTextureArray);
            RenderTexture m_SecondLayerExtendDRT = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_IDLayerExtendKernel, BlendShaderUtils.s_IDResultID, m_SecondLayerExtendDRT);
            BlendShaderUtils.s_Shader.Dispatch(BlendShaderUtils.s_IDLayerExtendKernel, threadGroups, threadGroups, 1);
            Utils.SaveRT2Texture(m_SecondLayerExtendDRT, TextureFormat.RGBA32, GetTextureSavePath("3ExtendSecondLayer.tga", m_TerrainAsset.m_TerrainName));
            // Double Layers Blend: 双层区域混合
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_DoubleLayersBlendKernel, BlendShaderUtils.s_RawIDTextureID, m_RawIDEdgeTexture);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_DoubleLayersBlendKernel, BlendShaderUtils.s_SecondLayerExtendID, m_SecondLayerExtendDRT);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_DoubleLayersBlendKernel, BlendShaderUtils.s_AlphaTextureArrayID, m_TerrainAsset.m_AlphaTextureArray);
            RenderTexture m_DoubleAreaID = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            RenderTexture m_DoubleAreaBlend = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_DoubleLayersBlendKernel, BlendShaderUtils.s_IDResultID, m_DoubleAreaID);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_DoubleLayersBlendKernel, BlendShaderUtils.s_BlendResultID, m_DoubleAreaBlend);
            BlendShaderUtils.s_Shader.Dispatch(BlendShaderUtils.s_DoubleLayersBlendKernel, threadGroups, threadGroups, 1);
            Utils.SaveRT2Texture(m_DoubleAreaID, TextureFormat.RGBA32, GetTextureSavePath("4DoubleAreaID.tga", m_TerrainAsset.m_TerrainName));
            Utils.SaveRT2Texture(m_DoubleAreaBlend, TextureFormat.RGBA32, GetTextureSavePath("5DoubleAreaBlend.tga", m_TerrainAsset.m_TerrainName));
            
            // // Three Area Check ID Layer Edge
            // CShaderUtils.s_Shader.SetVector(CShaderUtils.s_ExtendParamsID, new Vector4(0, 1, 0, 3));
            // CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_CheckIDLayerEdgeKernel, CShaderUtils.s_RawIDTextureID, m_RawIDTexture);
            // CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_CheckIDLayerEdgeKernel, CShaderUtils.s_AlphaTextureArrayID, m_TerrainAsset.m_AlphaTextureArray);
            // RenderTexture m_RawIDEdgeTextureTC = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            // CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_CheckIDLayerEdgeKernel, CShaderUtils.s_IDResultID, m_RawIDEdgeTextureTC);
            // CShaderUtils.s_Shader.Dispatch(CShaderUtils.s_CheckIDLayerEdgeKernel, threadGroups, threadGroups, 1);
            // Utils.SaveRT2Texture(m_RawIDEdgeTextureTC, TextureFormat.RGBA32, GetTextureSavePath("2RawIDEdgeTextureTC.tga", m_TerrainAsset.m_TerrainName));

            // CShaderUtils.s_Shader.SetVector(CShaderUtils.s_ExtendParamsID, new Vector4(0, 0, 1, 3));
            // CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_CheckIDLayerEdgeKernel, CShaderUtils.s_RawIDTextureID, m_RawIDTexture);
            // CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_CheckIDLayerEdgeKernel, CShaderUtils.s_AlphaTextureArrayID, m_TerrainAsset.m_AlphaTextureArray);
            // RenderTexture m_RawIDEdgeTextureTT = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            // CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_CheckIDLayerEdgeKernel, CShaderUtils.s_IDResultID, m_RawIDEdgeTextureTT);
            // CShaderUtils.s_Shader.Dispatch(CShaderUtils.s_CheckIDLayerEdgeKernel, threadGroups, threadGroups, 1);
            // Utils.SaveRT2Texture(m_RawIDEdgeTextureTT, TextureFormat.RGBA32, GetTextureSavePath("2RawIDEdgeTextureTT.tga", m_TerrainAsset.m_TerrainName));
            
            // Three Area ID Layer Extend
            BlendShaderUtils.s_Shader.SetVector(BlendShaderUtils.s_ExtendParamsID, new Vector4(0, 1, 0, 3));
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_IDLayerExtendKernel, BlendShaderUtils.s_RawIDTextureID, m_RawIDEdgeTexture);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_IDLayerExtendKernel, BlendShaderUtils.s_AlphaTextureArrayID, m_TerrainAsset.m_AlphaTextureArray);
            RenderTexture m_SecondLayerExtendTRT = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_IDLayerExtendKernel, BlendShaderUtils.s_IDResultID, m_SecondLayerExtendTRT);
            BlendShaderUtils.s_Shader.Dispatch(BlendShaderUtils.s_IDLayerExtendKernel, threadGroups, threadGroups, 1);
            Utils.SaveRT2Texture(m_SecondLayerExtendTRT, TextureFormat.RGBA32, GetTextureSavePath("6ExtendSecondLayer.tga", m_TerrainAsset.m_TerrainName));

            BlendShaderUtils.s_Shader.SetVector(BlendShaderUtils.s_ExtendParamsID, new Vector4(0, 0, 1, 3));
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_IDLayerExtendKernel, BlendShaderUtils.s_RawIDTextureID, m_RawIDEdgeTexture);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_IDLayerExtendKernel, BlendShaderUtils.s_AlphaTextureArrayID, m_TerrainAsset.m_AlphaTextureArray);
            RenderTexture m_ThirdLayerExtendTRT = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_IDLayerExtendKernel, BlendShaderUtils.s_IDResultID, m_ThirdLayerExtendTRT);
            BlendShaderUtils.s_Shader.Dispatch(BlendShaderUtils.s_IDLayerExtendKernel, threadGroups, threadGroups, 1);
            Utils.SaveRT2Texture(m_ThirdLayerExtendTRT, TextureFormat.RGBA32, GetTextureSavePath("6ExtendThirdLayer.tga", m_TerrainAsset.m_TerrainName));
            // Three Layers Blend
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_ThreeLayersBlendKernel, BlendShaderUtils.s_AlphaTextureArrayID, m_TerrainAsset.m_AlphaTextureArray);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_ThreeLayersBlendKernel, BlendShaderUtils.s_RawIDTextureID, m_DoubleAreaID);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_ThreeLayersBlendKernel, BlendShaderUtils.s_TempBlendTextureID, m_DoubleAreaBlend);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_ThreeLayersBlendKernel, BlendShaderUtils.s_SecondLayerExtendID, m_SecondLayerExtendTRT);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_ThreeLayersBlendKernel, BlendShaderUtils.s_ThirdLayerExtendID, m_ThirdLayerExtendTRT);
            RenderTexture m_ThreeAreaID = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            RenderTexture m_ThreeAreaBlend = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_ThreeLayersBlendKernel, BlendShaderUtils.s_IDResultID, m_ThreeAreaID);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_ThreeLayersBlendKernel, BlendShaderUtils.s_BlendResultID, m_ThreeAreaBlend);
            BlendShaderUtils.s_Shader.Dispatch(BlendShaderUtils.s_ThreeLayersBlendKernel, threadGroups, threadGroups, 1);
            Utils.SaveRT2Texture(m_ThreeAreaID, TextureFormat.RGBA32, GetTextureSavePath("7ThreeAreaID.tga", m_TerrainAsset.m_TerrainName));
            Utils.SaveRT2Texture(m_ThreeAreaBlend, TextureFormat.RGBA32, GetTextureSavePath("8ThreeAreaBlend.tga", m_TerrainAsset.m_TerrainName));

            
            indexRank.Release();
            m_RawIDTexture.Release();
            // m_RawIDTextureMask.Release();
            // m_RawIDEdgeTextureTD.Release();
            // m_RawIDEdgeTextureTT.Release();
            m_RawIDEdgeTexture.Release();
            m_SecondLayerExtendDRT.Release();
            m_DoubleAreaID.Release();
            m_DoubleAreaBlend.Release();
            m_SecondLayerExtendTRT.Release();
            m_ThirdLayerExtendTRT.Release();
            m_ThreeAreaBlend.Release();
            AssetDatabase.Refresh();
        }
        private void DispatchRawIDMaskKernel()
        {
            
        }
        public string GetTextureSavePath(string texName, string terrainName)
        {
            return $"{Utils.s_RootPath}/TerrainAsset/{terrainName}/Debug/{texName}";
        }
        private ComputeBuffer CreateAndGetBuffer(System.Array data, int stride)
        {
            ComputeBuffer tempBuffer = new ComputeBuffer(data.Length, stride);
            tempBuffer.SetData(data);
            return tempBuffer;
        }
    }
}
