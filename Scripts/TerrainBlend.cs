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

        public void UpdateTerrainBlend()
        {
            if (m_TerrainAsset == null || CShaderUtils.s_Shader == null) return;
            int threadGroups = Mathf.CeilToInt(m_TerrainAsset.m_AlphamapResolution / 8);
            CShaderUtils.s_Shader.SetInts(CShaderUtils.s_TerrainParamsID, 0, m_TerrainAsset.m_AlphamapResolution, m_TerrainAsset.m_LayersCount);
            ComputeBuffer indexRank = CreateAndGetBuffer(m_TerrainAsset.m_CoverageIndexRank, sizeof(uint));
            // Raw ID Texture
            CShaderUtils.s_Shader.SetBuffer(CShaderUtils.s_RawIDTextureKernel, CShaderUtils.s_IndexRankID, indexRank);
            CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_RawIDTextureKernel, CShaderUtils.s_RawIDMaskArrayID, m_TerrainAsset.m_RawIDMaskArray);
            CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_RawIDTextureKernel, CShaderUtils.s_AlphaTextureArrayID, m_TerrainAsset.m_AlphaTextureArray);
            RenderTexture m_RawIDTexture = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_RawIDTextureKernel, CShaderUtils.s_IDResultID, m_RawIDTexture);
            CShaderUtils.s_Shader.Dispatch(CShaderUtils.s_RawIDTextureKernel, threadGroups, threadGroups, 1);
            Utils.SaveRT2Texture(m_RawIDTexture, TextureFormat.RGBA32, GetTextureSavePath("1RawIDTexture.tga", m_TerrainAsset.m_TerrainName));
            // Check ID Layer Edge
            CShaderUtils.s_Shader.SetVector(CShaderUtils.s_ExtendParamsID, new Vector4(0, 1, 0, 2));
            CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_CheckIDLayerEdgeKernel, CShaderUtils.s_RawIDTextureID, m_RawIDTexture);
            CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_CheckIDLayerEdgeKernel, CShaderUtils.s_AlphaTextureArrayID, m_TerrainAsset.m_AlphaTextureArray);
            RenderTexture m_RawIDEdgeTexture = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_CheckIDLayerEdgeKernel, CShaderUtils.s_IDResultID, m_RawIDEdgeTexture);
            CShaderUtils.s_Shader.Dispatch(CShaderUtils.s_CheckIDLayerEdgeKernel, threadGroups, threadGroups, 1);
            Utils.SaveRT2Texture(m_RawIDEdgeTexture, TextureFormat.RGBA32, GetTextureSavePath("2RawIDEdgeTexture.tga", m_TerrainAsset.m_TerrainName));
            // Double Area ID Layer Extend
            CShaderUtils.s_Shader.SetVector(CShaderUtils.s_ExtendParamsID, new Vector4(0, 1, 0, 2));
            CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_IDLayerExtendKernel, CShaderUtils.s_RawIDTextureID, m_RawIDEdgeTexture);
            CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_IDLayerExtendKernel, CShaderUtils.s_AlphaTextureArrayID, m_TerrainAsset.m_AlphaTextureArray);
            RenderTexture m_SecondLayerExtendDRT = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_IDLayerExtendKernel, CShaderUtils.s_IDResultID, m_SecondLayerExtendDRT);
            CShaderUtils.s_Shader.Dispatch(CShaderUtils.s_IDLayerExtendKernel, threadGroups, threadGroups, 1);
            Utils.SaveRT2Texture(m_SecondLayerExtendDRT, TextureFormat.RGBA32, GetTextureSavePath("3ExtendSecondLayer.tga", m_TerrainAsset.m_TerrainName));
            // Double Layers Blend
            CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_DoubleLayersBlendKernel, CShaderUtils.s_RawIDTextureID, m_RawIDEdgeTexture);
            CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_DoubleLayersBlendKernel, CShaderUtils.s_SecondLayerExtendID, m_SecondLayerExtendDRT);
            CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_DoubleLayersBlendKernel, CShaderUtils.s_AlphaTextureArrayID, m_TerrainAsset.m_AlphaTextureArray);
            RenderTexture m_DoubleAreaID = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            RenderTexture m_DoubleAreaBlend = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_DoubleLayersBlendKernel, CShaderUtils.s_IDResultID, m_DoubleAreaID);
            CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_DoubleLayersBlendKernel, CShaderUtils.s_BlendResultID, m_DoubleAreaBlend);
            CShaderUtils.s_Shader.Dispatch(CShaderUtils.s_DoubleLayersBlendKernel, threadGroups, threadGroups, 1);
            Utils.SaveRT2Texture(m_DoubleAreaID, TextureFormat.RGBA32, GetTextureSavePath("4DoubleAreaID.tga", m_TerrainAsset.m_TerrainName));
            Utils.SaveRT2Texture(m_DoubleAreaBlend, TextureFormat.RGBA32, GetTextureSavePath("5DoubleAreaBlend.tga", m_TerrainAsset.m_TerrainName));
            // Three Area ID Layer Extend
            CShaderUtils.s_Shader.SetVector(CShaderUtils.s_ExtendParamsID, new Vector4(0, 1, 0, 3));
            CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_IDLayerExtendKernel, CShaderUtils.s_RawIDTextureID, m_RawIDEdgeTexture);
            CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_IDLayerExtendKernel, CShaderUtils.s_AlphaTextureArrayID, m_TerrainAsset.m_AlphaTextureArray);
            RenderTexture m_SecondLayerExtendTRT = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_IDLayerExtendKernel, CShaderUtils.s_IDResultID, m_SecondLayerExtendTRT);
            CShaderUtils.s_Shader.Dispatch(CShaderUtils.s_IDLayerExtendKernel, threadGroups, threadGroups, 1);
            Utils.SaveRT2Texture(m_SecondLayerExtendTRT, TextureFormat.RGBA32, GetTextureSavePath("6ExtendSecondLayer.tga", m_TerrainAsset.m_TerrainName));

            CShaderUtils.s_Shader.SetVector(CShaderUtils.s_ExtendParamsID, new Vector4(0, 0, 1, 3));
            CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_IDLayerExtendKernel, CShaderUtils.s_RawIDTextureID, m_RawIDEdgeTexture);
            CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_IDLayerExtendKernel, CShaderUtils.s_AlphaTextureArrayID, m_TerrainAsset.m_AlphaTextureArray);
            RenderTexture m_ThirdLayerExtendTRT = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_IDLayerExtendKernel, CShaderUtils.s_IDResultID, m_ThirdLayerExtendTRT);
            CShaderUtils.s_Shader.Dispatch(CShaderUtils.s_IDLayerExtendKernel, threadGroups, threadGroups, 1);
            Utils.SaveRT2Texture(m_ThirdLayerExtendTRT, TextureFormat.RGBA32, GetTextureSavePath("7ExtendThirdLayer.tga", m_TerrainAsset.m_TerrainName));
            // Three Layers Blend
            CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_ThreeLayersBlendKernel, CShaderUtils.s_AlphaTextureArrayID, m_TerrainAsset.m_AlphaTextureArray);
            CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_ThreeLayersBlendKernel, CShaderUtils.s_RawIDTextureID, m_DoubleAreaBlend);
            CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_ThreeLayersBlendKernel, CShaderUtils.s_SecondLayerExtendID, m_SecondLayerExtendTRT);
            CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_ThreeLayersBlendKernel, CShaderUtils.s_ThirdLayerExtendID, m_ThirdLayerExtendTRT);
            RenderTexture m_ThreeAreaBlend = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_ThreeLayersBlendKernel, CShaderUtils.s_BlendResultID, m_ThreeAreaBlend);
            CShaderUtils.s_Shader.Dispatch(CShaderUtils.s_ThreeLayersBlendKernel, threadGroups, threadGroups, 1);
            Utils.SaveRT2Texture(m_ThreeAreaBlend, TextureFormat.RGBA32, GetTextureSavePath("8ThreeAreaBlend.tga", m_TerrainAsset.m_TerrainName));

            m_RawIDTexture.Release();
            m_RawIDEdgeTexture.Release();
            m_SecondLayerExtendDRT.Release();
            m_DoubleAreaID.Release();
            m_DoubleAreaBlend.Release();
            m_SecondLayerExtendTRT.Release();
            m_ThirdLayerExtendTRT.Release();
            m_ThreeAreaBlend.Release();
            AssetDatabase.Refresh();
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
