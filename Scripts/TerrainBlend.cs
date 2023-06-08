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
#region 双层混合区域
            // 输出原始的IDTexture贴图
            ComputeBuffer indexRank = CreateAndGetBuffer(m_TerrainAsset.m_CoverageIndexRank, sizeof(uint));
            BlendShaderUtils.s_Shader.SetBuffer(BlendShaderUtils.s_RawIDTextureKernel, BlendShaderUtils.s_IndexRankID, indexRank);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_RawIDTextureKernel, BlendShaderUtils.s_RawIDMaskArrayID, m_TerrainAsset.m_RawIDMaskArray);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_RawIDTextureKernel, BlendShaderUtils.s_AlphaTextureArrayID, m_TerrainAsset.m_AlphaTextureArray);
            RenderTexture rawIDTexture = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_RawIDTextureKernel, BlendShaderUtils.s_IDResultID, rawIDTexture);
            BlendShaderUtils.s_Shader.Dispatch(BlendShaderUtils.s_RawIDTextureKernel, threadGroups, threadGroups, 1);
            Utils.SaveRT2Texture(rawIDTexture, TextureFormat.RGBA32, GetTextureSavePath("1RawIDTexture.tga", m_TerrainAsset.m_TerrainName));
            // 输出未向外扩展时IDTexture的双层混合结构中的混合层边界
            BlendShaderUtils.s_Shader.SetVector(BlendShaderUtils.s_ExtendParamsID, new Vector4(0, 1, 0, 2));
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_FindIDLayerEdgeKernel, BlendShaderUtils.s_RawIDTextureID, rawIDTexture);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_FindIDLayerEdgeKernel, BlendShaderUtils.s_AlphaTextureArrayID, m_TerrainAsset.m_AlphaTextureArray);
            RenderTexture rawIDEdgeTexture = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_FindIDLayerEdgeKernel, BlendShaderUtils.s_IDResultID, rawIDEdgeTexture);
            BlendShaderUtils.s_Shader.Dispatch(BlendShaderUtils.s_FindIDLayerEdgeKernel, threadGroups, threadGroups, 1);
            Utils.SaveRT2Texture(rawIDEdgeTexture, TextureFormat.RGBA32, GetTextureSavePath("2_1RawIDEdgeTexture.tga", m_TerrainAsset.m_TerrainName));
            // 处理相交处: 边缘相似
            BlendShaderUtils.s_Shader.SetBuffer(BlendShaderUtils.s_CheckLayerSimilarEdgeKernel, BlendShaderUtils.s_IndexRankID, indexRank);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_CheckLayerSimilarEdgeKernel, BlendShaderUtils.s_TempTextureID, rawIDEdgeTexture);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_CheckLayerSimilarEdgeKernel, BlendShaderUtils.s_AlphaTextureArrayID, m_TerrainAsset.m_AlphaTextureArray);
            RenderTexture rawIDEdgeTexture_01 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_CheckLayerSimilarEdgeKernel, BlendShaderUtils.s_IDResultID, rawIDEdgeTexture_01);
            BlendShaderUtils.s_Shader.Dispatch(BlendShaderUtils.s_CheckLayerSimilarEdgeKernel, threadGroups, threadGroups, 1);
            Utils.SaveRT2Texture(rawIDEdgeTexture_01, TextureFormat.RGBA32, GetTextureSavePath("2_2RawIDEdgeTexture_01.tga", m_TerrainAsset.m_TerrainName));
            // 处理相交处: 扩展维度
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_TransformDimensionKernel, BlendShaderUtils.s_RawIDTextureID, rawIDTexture);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_TransformDimensionKernel, BlendShaderUtils.s_TempTextureID, rawIDEdgeTexture_01);
            RenderTexture rawIDTexture_01 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            RenderTexture rawIDEdgeTexture_02 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_TransformDimensionKernel, BlendShaderUtils.s_IDResultID, rawIDTexture_01);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_TransformDimensionKernel, BlendShaderUtils.s_BlendResultID, rawIDEdgeTexture_02);
            BlendShaderUtils.s_Shader.Dispatch(BlendShaderUtils.s_TransformDimensionKernel, threadGroups, threadGroups, 1);
            Utils.SaveRT2Texture(rawIDTexture_01, TextureFormat.RGBA32, GetTextureSavePath("2_3RawIDTexture_01.tga", m_TerrainAsset.m_TerrainName));
            Utils.SaveRT2Texture(rawIDEdgeTexture_02, TextureFormat.RGBA32, GetTextureSavePath("2_3RawIDEdgeTexture_02.tga", m_TerrainAsset.m_TerrainName));
            // Layer扩展
            BlendShaderUtils.s_Shader.SetVector(BlendShaderUtils.s_ExtendParamsID, new Vector4(0, 1, 0, 2));
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_IDLayerExtendKernel, BlendShaderUtils.s_RawIDTextureID, rawIDTexture_01);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_IDLayerExtendKernel, BlendShaderUtils.s_AlphaTextureArrayID, m_TerrainAsset.m_AlphaTextureArray);
            RenderTexture secondLayerExtendDRT = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_IDLayerExtendKernel, BlendShaderUtils.s_IDResultID, secondLayerExtendDRT);
            BlendShaderUtils.s_Shader.Dispatch(BlendShaderUtils.s_IDLayerExtendKernel, threadGroups, threadGroups, 1);
            Utils.SaveRT2Texture(secondLayerExtendDRT, TextureFormat.RGBA32, GetTextureSavePath("3_1ExtendSecondLayer.tga", m_TerrainAsset.m_TerrainName));
            // 寻找扩展层的边界
            BlendShaderUtils.s_Shader.SetVector(BlendShaderUtils.s_ExtendParamsID, new Vector4(1, 0, 0, 2));
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_FindIDLayerEdgeKernel, BlendShaderUtils.s_RawIDTextureID, secondLayerExtendDRT);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_FindIDLayerEdgeKernel, BlendShaderUtils.s_AlphaTextureArrayID, m_TerrainAsset.m_AlphaTextureArray);
            RenderTexture extendEdgeDTexture = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_FindIDLayerEdgeKernel, BlendShaderUtils.s_IDResultID, extendEdgeDTexture);
            BlendShaderUtils.s_Shader.Dispatch(BlendShaderUtils.s_FindIDLayerEdgeKernel, threadGroups, threadGroups, 1);
            Utils.SaveRT2Texture(extendEdgeDTexture, TextureFormat.RGBA32, GetTextureSavePath("3_2ExtendEdgeTexture.tga", m_TerrainAsset.m_TerrainName));
            // 处理相交处: 边缘相似
            BlendShaderUtils.s_Shader.SetBuffer(BlendShaderUtils.s_CheckLayerSimilarEdgeKernel, BlendShaderUtils.s_IndexRankID, indexRank);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_CheckLayerSimilarEdgeKernel, BlendShaderUtils.s_TempTextureID, extendEdgeDTexture);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_CheckLayerSimilarEdgeKernel, BlendShaderUtils.s_AlphaTextureArrayID, m_TerrainAsset.m_AlphaTextureArray);
            RenderTexture extendEdgeDTexture_01 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_CheckLayerSimilarEdgeKernel, BlendShaderUtils.s_IDResultID, extendEdgeDTexture_01);
            BlendShaderUtils.s_Shader.Dispatch(BlendShaderUtils.s_CheckLayerSimilarEdgeKernel, threadGroups, threadGroups, 1);
            Utils.SaveRT2Texture(extendEdgeDTexture_01, TextureFormat.RGBA32, GetTextureSavePath("3_3ExtendEdgeTexture_01.tga", m_TerrainAsset.m_TerrainName));
            // 处理相交处: 处理用于扩展维度的Mask
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_CheckExtendLayerEdgeKernel, BlendShaderUtils.s_SecondLayerExtendID, secondLayerExtendDRT);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_CheckExtendLayerEdgeKernel, BlendShaderUtils.s_TempTextureID, extendEdgeDTexture_01);
            RenderTexture extendEdgeDTexture_02 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_CheckExtendLayerEdgeKernel, BlendShaderUtils.s_IDResultID, extendEdgeDTexture_02);
            BlendShaderUtils.s_Shader.Dispatch(BlendShaderUtils.s_CheckExtendLayerEdgeKernel, threadGroups, threadGroups, 1);
            Utils.SaveRT2Texture(extendEdgeDTexture_02, TextureFormat.RGBA32, GetTextureSavePath("3_4ExtendEdgeTexture_02.tga", m_TerrainAsset.m_TerrainName));
            // 处理相交处: 扩展维度
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_TransformDimensionKernel, BlendShaderUtils.s_RawIDTextureID, rawIDTexture_01);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_TransformDimensionKernel, BlendShaderUtils.s_TempTextureID, extendEdgeDTexture_02);
            RenderTexture rawIDTexture_02 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            RenderTexture extendEdgeDTexture_03 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_TransformDimensionKernel, BlendShaderUtils.s_IDResultID, rawIDTexture_02);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_TransformDimensionKernel, BlendShaderUtils.s_BlendResultID, extendEdgeDTexture_03);
            BlendShaderUtils.s_Shader.Dispatch(BlendShaderUtils.s_TransformDimensionKernel, threadGroups, threadGroups, 1);
            Utils.SaveRT2Texture(rawIDTexture_02, TextureFormat.RGBA32, GetTextureSavePath("3_5RawIDTexture_02.tga", m_TerrainAsset.m_TerrainName));
            Utils.SaveRT2Texture(extendEdgeDTexture_03, TextureFormat.RGBA32, GetTextureSavePath("3_5ExtendEdgeTexture_03.tga", m_TerrainAsset.m_TerrainName));
            // 双层混合结构输出
            // BlendShaderUtils.s_Shader.SetBuffer(BlendShaderUtils.s_DoubleLayersBlendKernel, BlendShaderUtils.s_IndexRankID, indexRank);
            // BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_DoubleLayersBlendKernel, BlendShaderUtils.s_RawIDTextureID, rawIDTexture_02);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_DoubleLayersBlendKernel, BlendShaderUtils.s_SecondLayerExtendID, extendEdgeDTexture_03);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_DoubleLayersBlendKernel, BlendShaderUtils.s_AlphaTextureArrayID, m_TerrainAsset.m_AlphaTextureArray);
            // RenderTexture m_DoubleAreaID = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            RenderTexture m_DoubleAreaBlend = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            // BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_DoubleLayersBlendKernel, BlendShaderUtils.s_IDResultID, m_DoubleAreaID);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_DoubleLayersBlendKernel, BlendShaderUtils.s_BlendResultID, m_DoubleAreaBlend);
            BlendShaderUtils.s_Shader.Dispatch(BlendShaderUtils.s_DoubleLayersBlendKernel, threadGroups, threadGroups, 1);
            // Utils.SaveRT2Texture(m_DoubleAreaID, TextureFormat.RGBA32, GetTextureSavePath("11DoubleAreaID.tga", m_TerrainAsset.m_TerrainName));
            Utils.SaveRT2Texture(m_DoubleAreaBlend, TextureFormat.RGBA32, GetTextureSavePath("11DoubleAreaBlend.tga", m_TerrainAsset.m_TerrainName));

            rawIDEdgeTexture.Release();
            rawIDEdgeTexture_01.Release();
            rawIDEdgeTexture_02.Release();
            rawIDTexture_01.Release();

            secondLayerExtendDRT.Release();
            extendEdgeDTexture.Release();
            extendEdgeDTexture_01.Release();
            extendEdgeDTexture_02.Release();
            extendEdgeDTexture_03.Release();
#endregion
#region 三层混合区域
            // 输出未向外扩展时IDTexture的三层混合结构中的混合层边界
            BlendShaderUtils.s_Shader.SetVector(BlendShaderUtils.s_ExtendParamsID, new Vector4(0, 1, 0, 3));
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_FindIDLayerEdgeKernel, BlendShaderUtils.s_RawIDTextureID, rawIDTexture);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_FindIDLayerEdgeKernel, BlendShaderUtils.s_AlphaTextureArrayID, m_TerrainAsset.m_AlphaTextureArray);
            RenderTexture rawIDEdgeTextureS = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_FindIDLayerEdgeKernel, BlendShaderUtils.s_IDResultID, rawIDEdgeTextureS);
            BlendShaderUtils.s_Shader.Dispatch(BlendShaderUtils.s_FindIDLayerEdgeKernel, threadGroups, threadGroups, 1);
            Utils.SaveRT2Texture(rawIDEdgeTextureS, TextureFormat.RGBA32, GetTextureSavePath("4_1RawIDEdgeTextureS.tga", m_TerrainAsset.m_TerrainName));
            BlendShaderUtils.s_Shader.SetVector(BlendShaderUtils.s_ExtendParamsID, new Vector4(0, 0, 1, 3));
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_FindIDLayerEdgeKernel, BlendShaderUtils.s_RawIDTextureID, rawIDTexture_02);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_FindIDLayerEdgeKernel, BlendShaderUtils.s_AlphaTextureArrayID, m_TerrainAsset.m_AlphaTextureArray);
            RenderTexture rawIDEdgeTextureT = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_FindIDLayerEdgeKernel, BlendShaderUtils.s_IDResultID, rawIDEdgeTextureT);
            BlendShaderUtils.s_Shader.Dispatch(BlendShaderUtils.s_FindIDLayerEdgeKernel, threadGroups, threadGroups, 1);
            Utils.SaveRT2Texture(rawIDEdgeTextureT, TextureFormat.RGBA32, GetTextureSavePath("4_1RawIDEdgeTextureT.tga", m_TerrainAsset.m_TerrainName));
            // 处理相交处: 边缘相似
            BlendShaderUtils.s_Shader.SetBuffer(BlendShaderUtils.s_CheckLayerSimilarEdgeKernel, BlendShaderUtils.s_IndexRankID, indexRank);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_CheckLayerSimilarEdgeKernel, BlendShaderUtils.s_TempTextureID, rawIDEdgeTextureS);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_CheckLayerSimilarEdgeKernel, BlendShaderUtils.s_AlphaTextureArrayID, m_TerrainAsset.m_AlphaTextureArray);
            RenderTexture rawIDEdgeTextureS_01 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_CheckLayerSimilarEdgeKernel, BlendShaderUtils.s_IDResultID, rawIDEdgeTextureS_01);
            BlendShaderUtils.s_Shader.Dispatch(BlendShaderUtils.s_CheckLayerSimilarEdgeKernel, threadGroups, threadGroups, 1);
            Utils.SaveRT2Texture(rawIDEdgeTextureS_01, TextureFormat.RGBA32, GetTextureSavePath("4_2RawIDEdgeTextureS_01.tga", m_TerrainAsset.m_TerrainName));
            BlendShaderUtils.s_Shader.SetBuffer(BlendShaderUtils.s_CheckLayerSimilarEdgeKernel, BlendShaderUtils.s_IndexRankID, indexRank);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_CheckLayerSimilarEdgeKernel, BlendShaderUtils.s_TempTextureID, rawIDEdgeTextureT);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_CheckLayerSimilarEdgeKernel, BlendShaderUtils.s_AlphaTextureArrayID, m_TerrainAsset.m_AlphaTextureArray);
            RenderTexture rawIDEdgeTextureT_01 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_CheckLayerSimilarEdgeKernel, BlendShaderUtils.s_IDResultID, rawIDEdgeTextureT_01);
            BlendShaderUtils.s_Shader.Dispatch(BlendShaderUtils.s_CheckLayerSimilarEdgeKernel, threadGroups, threadGroups, 1);
            Utils.SaveRT2Texture(rawIDEdgeTextureT_01, TextureFormat.RGBA32, GetTextureSavePath("4_2RawIDEdgeTextureT_01.tga", m_TerrainAsset.m_TerrainName));
            // 扩展
            BlendShaderUtils.s_Shader.SetVector(BlendShaderUtils.s_ExtendParamsID, new Vector4(0, 1, 0, 3));
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_IDLayerExtendKernel, BlendShaderUtils.s_RawIDTextureID, rawIDTexture_02);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_IDLayerExtendKernel, BlendShaderUtils.s_AlphaTextureArrayID, m_TerrainAsset.m_AlphaTextureArray);
            RenderTexture secondLayerExtendTRT = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_IDLayerExtendKernel, BlendShaderUtils.s_IDResultID, secondLayerExtendTRT);
            BlendShaderUtils.s_Shader.Dispatch(BlendShaderUtils.s_IDLayerExtendKernel, threadGroups, threadGroups, 1);
            Utils.SaveRT2Texture(secondLayerExtendTRT, TextureFormat.RGBA32, GetTextureSavePath("4_3ExtendSecondLayer.tga", m_TerrainAsset.m_TerrainName));
            BlendShaderUtils.s_Shader.SetVector(BlendShaderUtils.s_ExtendParamsID, new Vector4(0, 0, 1, 3));
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_IDLayerExtendKernel, BlendShaderUtils.s_RawIDTextureID, rawIDTexture_02);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_IDLayerExtendKernel, BlendShaderUtils.s_AlphaTextureArrayID, m_TerrainAsset.m_AlphaTextureArray);
            RenderTexture thirdLayerExtendTRT = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_IDLayerExtendKernel, BlendShaderUtils.s_IDResultID, thirdLayerExtendTRT);
            BlendShaderUtils.s_Shader.Dispatch(BlendShaderUtils.s_IDLayerExtendKernel, threadGroups, threadGroups, 1);
            Utils.SaveRT2Texture(thirdLayerExtendTRT, TextureFormat.RGBA32, GetTextureSavePath("4_3ExtendThirdLayer.tga", m_TerrainAsset.m_TerrainName));
            // 混合
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_ThreeLayersBlendKernel, BlendShaderUtils.s_AlphaTextureArrayID, m_TerrainAsset.m_AlphaTextureArray);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_ThreeLayersBlendKernel, BlendShaderUtils.s_RawIDTextureID, rawIDTexture_02);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_ThreeLayersBlendKernel, BlendShaderUtils.s_TempTextureID, m_DoubleAreaBlend);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_ThreeLayersBlendKernel, BlendShaderUtils.s_SecondLayerExtendID, secondLayerExtendTRT);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_ThreeLayersBlendKernel, BlendShaderUtils.s_ThirdLayerExtendID, thirdLayerExtendTRT);
            RenderTexture m_ThreeAreaID = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            RenderTexture m_ThreeAreaBlend = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_ThreeLayersBlendKernel, BlendShaderUtils.s_IDResultID, m_ThreeAreaID);
            BlendShaderUtils.s_Shader.SetTexture(BlendShaderUtils.s_ThreeLayersBlendKernel, BlendShaderUtils.s_BlendResultID, m_ThreeAreaBlend);
            BlendShaderUtils.s_Shader.Dispatch(BlendShaderUtils.s_ThreeLayersBlendKernel, threadGroups, threadGroups, 1);
            Utils.SaveRT2Texture(m_ThreeAreaID, TextureFormat.RGBA32, GetTextureSavePath("7ThreeAreaID.tga", m_TerrainAsset.m_TerrainName));
            Utils.SaveRT2Texture(m_ThreeAreaBlend, TextureFormat.RGBA32, GetTextureSavePath("8ThreeAreaBlend.tga", m_TerrainAsset.m_TerrainName));
#endregion
            rawIDEdgeTextureS.Release();
            rawIDEdgeTextureT.Release();
            rawIDEdgeTextureS_01.Release();
            rawIDEdgeTextureT_01.Release();
            secondLayerExtendTRT.Release();
            thirdLayerExtendTRT.Release();
            m_ThreeAreaID.Release();
            m_ThreeAreaBlend.Release();

            rawIDTexture.Release();
            rawIDTexture_02.Release();
            m_DoubleAreaBlend.Release();
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
