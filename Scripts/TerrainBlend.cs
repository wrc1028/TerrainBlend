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
            if (m_TerrainAsset == null || BlendCS2LUtils.Compute == null) return;
            m_TerrainAsset.m_ThreadGroups = Mathf.CeilToInt(m_TerrainAsset.m_AlphamapResolution / 8);
            BlendCS2LUtils.Compute.SetInts(ShaderParams.s_TerrainParamsID, m_TerrainAsset.m_LayersCount, m_TerrainAsset.m_AlphamapResolution, 0);
#region 双层混合区域
            // 输出原始的IDTexture贴图
            ComputeBuffer indexRank = CreateAndGetBuffer(m_TerrainAsset.m_CoverageIndexRank, sizeof(uint));
            RenderTexture rawIDTexture = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchRawIDTexture(m_TerrainAsset, indexRank, ref rawIDTexture);
            Utils.SaveRT2Texture(rawIDTexture, TextureFormat.RGBA32, GetTextureSavePath("1RawIDTexture.tga", m_TerrainAsset.m_TerrainName));
            // 输出未向外扩展时IDTexture的双层混合结构中的混合层边界
            RenderTexture rawIDEdgeTexture = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchFindIDLayerEdge(m_TerrainAsset, new Vector4(0, 1, 0, 2), rawIDTexture, ref rawIDEdgeTexture);
            Utils.SaveRT2Texture(rawIDEdgeTexture, TextureFormat.RGBA32, GetTextureSavePath("2_1RawIDEdgeTexture.tga", m_TerrainAsset.m_TerrainName));
            // 处理相交处: 边缘相似
            RenderTexture rawIDEdgeTexture_01 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchCheckLayerSimilarEdge(m_TerrainAsset, indexRank, rawIDEdgeTexture, ref rawIDEdgeTexture_01);
            Utils.SaveRT2Texture(rawIDEdgeTexture_01, TextureFormat.RGBA32, GetTextureSavePath("2_2RawIDEdgeTexture_01.tga", m_TerrainAsset.m_TerrainName));
            // 处理相交处: 扩展维度
            RenderTexture rawIDTexture_01 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            RenderTexture rawIDEdgeTexture_02 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchTransformDimension(m_TerrainAsset, rawIDTexture, rawIDEdgeTexture_01, ref rawIDTexture_01, ref rawIDEdgeTexture_02);
            Utils.SaveRT2Texture(rawIDTexture_01, TextureFormat.RGBA32, GetTextureSavePath("2_3RawIDTexture_01.tga", m_TerrainAsset.m_TerrainName));
            Utils.SaveRT2Texture(rawIDEdgeTexture_02, TextureFormat.RGBA32, GetTextureSavePath("2_3RawIDEdgeTexture_02.tga", m_TerrainAsset.m_TerrainName));
            // Layer扩展
            RenderTexture secondLayerExtendD = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchIDLayerExtend(m_TerrainAsset, new Vector4(0, 1, 0, 2), rawIDTexture_01, ref secondLayerExtendD);
            Utils.SaveRT2Texture(secondLayerExtendD, TextureFormat.RGBA32, GetTextureSavePath("3_1ExtendSecondLayer.tga", m_TerrainAsset.m_TerrainName));
            // 寻找扩展层的边界
            RenderTexture extendEdgeTextureD = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchFindIDLayerEdge(m_TerrainAsset, new Vector4(1, 0, 0, 2), secondLayerExtendD, ref extendEdgeTextureD);
            Utils.SaveRT2Texture(extendEdgeTextureD, TextureFormat.RGBA32, GetTextureSavePath("3_2ExtendEdgeTexture.tga", m_TerrainAsset.m_TerrainName));
            // 处理相交处: 边缘相似
            RenderTexture extendEdgeTextureD_01 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchCheckLayerSimilarEdge(m_TerrainAsset, indexRank, extendEdgeTextureD, ref extendEdgeTextureD_01);
            Utils.SaveRT2Texture(extendEdgeTextureD_01, TextureFormat.RGBA32, GetTextureSavePath("3_3ExtendEdgeTexture_01.tga", m_TerrainAsset.m_TerrainName));
            // 处理相交处: 处理用于扩展维度的Mask, 因为有些是正常向外扩展没有相交情况, 或者扩充的位置不位于双层结构内
            RenderTexture extendEdgeTextureD_02 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchCheckExtendLayerEdge(m_TerrainAsset, extendEdgeTextureD_01, secondLayerExtendD, ref extendEdgeTextureD_02);
            Utils.SaveRT2Texture(extendEdgeTextureD_02, TextureFormat.RGBA32, GetTextureSavePath("3_4ExtendEdgeTexture_02.tga", m_TerrainAsset.m_TerrainName));
            // 处理相交处: 扩展维度
            RenderTexture rawIDTexture_02 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            RenderTexture extendEdgeTextureD_03 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchTransformDimension(m_TerrainAsset, rawIDTexture_01, extendEdgeTextureD_02, ref rawIDTexture_02, ref extendEdgeTextureD_03);
            Utils.SaveRT2Texture(rawIDTexture_02, TextureFormat.RGBA32, GetTextureSavePath("3_5RawIDTexture_02.tga", m_TerrainAsset.m_TerrainName));
            Utils.SaveRT2Texture(extendEdgeTextureD_03, TextureFormat.RGBA32, GetTextureSavePath("3_5ExtendEdgeTexture_03.tga", m_TerrainAsset.m_TerrainName));
            // 双层混合结构输出
            BlendCS2LUtils.Compute.SetTexture(BlendCS2LUtils.DoubleLayersBlend, ShaderParams.s_TexInput1ID, extendEdgeTextureD_03);
            BlendCS2LUtils.Compute.SetTexture(BlendCS2LUtils.DoubleLayersBlend, ShaderParams.s_AlphaTextureArrayID, m_TerrainAsset.m_AlphaTextureArray);
            RenderTexture m_DoubleAreaBlend = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.Compute.SetTexture(BlendCS2LUtils.DoubleLayersBlend, ShaderParams.s_Result1ID, m_DoubleAreaBlend);
            BlendCS2LUtils.Compute.Dispatch(BlendCS2LUtils.DoubleLayersBlend, m_TerrainAsset.m_ThreadGroups, m_TerrainAsset.m_ThreadGroups, 1);
            Utils.SaveRT2Texture(m_DoubleAreaBlend, TextureFormat.RGBA32, GetTextureSavePath("11DoubleAreaBlend.tga", m_TerrainAsset.m_TerrainName));

            rawIDEdgeTexture.Release();
            rawIDEdgeTexture_01.Release();
            rawIDEdgeTexture_02.Release();
            rawIDTexture_01.Release();

            secondLayerExtendD.Release();
            extendEdgeTextureD.Release();
            extendEdgeTextureD_01.Release();
            extendEdgeTextureD_02.Release();
            extendEdgeTextureD_03.Release();
#endregion
#region 三层混合区域
            // 输出未向外扩展时IDTexture的三层混合结构中的混合层边界
            RenderTexture rawIDEdgeTextureS = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchFindIDLayerEdge(m_TerrainAsset, new Vector4(0, 1, 0, 3), rawIDTexture_02, ref rawIDEdgeTextureS);
            Utils.SaveRT2Texture(rawIDEdgeTextureS, TextureFormat.RGBA32, GetTextureSavePath("4_1RawIDEdgeTextureS.tga", m_TerrainAsset.m_TerrainName));
            RenderTexture rawIDEdgeTextureT = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchFindIDLayerEdge(m_TerrainAsset, new Vector4(0, 0, 1, 3), rawIDTexture_02, ref rawIDEdgeTextureT);
            Utils.SaveRT2Texture(rawIDEdgeTextureT, TextureFormat.RGBA32, GetTextureSavePath("4_1RawIDEdgeTextureT.tga", m_TerrainAsset.m_TerrainName));
            // 处理相交处: 边缘相似
            RenderTexture rawIDEdgeTextureS_01 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchCheckLayerSimilarEdge(m_TerrainAsset, indexRank, rawIDEdgeTextureS, ref rawIDEdgeTextureS_01);
            Utils.SaveRT2Texture(rawIDEdgeTextureS_01, TextureFormat.RGBA32, GetTextureSavePath("4_2RawIDEdgeTextureS_01.tga", m_TerrainAsset.m_TerrainName));
            RenderTexture rawIDEdgeTextureT_01 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchCheckLayerSimilarEdge(m_TerrainAsset, indexRank, rawIDEdgeTextureT, ref rawIDEdgeTextureT_01);
            Utils.SaveRT2Texture(rawIDEdgeTextureT_01, TextureFormat.RGBA32, GetTextureSavePath("4_2RawIDEdgeTextureT_01.tga", m_TerrainAsset.m_TerrainName));
            // AlphaMask
            RenderTexture alphaInput = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            RenderTexture alphaMask = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS3LUtils.DispacthFindTransformMask(m_TerrainAsset, rawIDEdgeTextureS_01, rawIDEdgeTextureT_01, alphaInput, ref alphaMask);
            Utils.SaveRT2Texture(alphaMask, TextureFormat.RGBA32, GetTextureSavePath("4_3AlphaMask.tga", m_TerrainAsset.m_TerrainName));
            // 消除原始边界
            RenderTexture rawIDEdgeTextureS_02 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            RenderTexture rawIDEdgeTextureT_02 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            RenderTexture rawIDTexture_03 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS3LUtils.DispacthTransformDimension(m_TerrainAsset, rawIDEdgeTextureS_01, rawIDEdgeTextureT_01, alphaMask, rawIDTexture_02, 
                ref rawIDEdgeTextureS_02, ref rawIDEdgeTextureT_02, ref rawIDTexture_03);
            Utils.SaveRT2Texture(rawIDEdgeTextureS_02, TextureFormat.RGBA32, GetTextureSavePath("4_4RawIDEdgeTextureS_02.tga", m_TerrainAsset.m_TerrainName));
            Utils.SaveRT2Texture(rawIDEdgeTextureT_02, TextureFormat.RGBA32, GetTextureSavePath("4_4RawIDEdgeTextureT_02.tga", m_TerrainAsset.m_TerrainName));
            Utils.SaveRT2Texture(rawIDTexture_03, TextureFormat.RGBA32, GetTextureSavePath("4_4RawIDTexture_03.tga", m_TerrainAsset.m_TerrainName));
            // 扩展
            RenderTexture secondLayerExtendT = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchIDLayerExtend(m_TerrainAsset, new Vector4(0, 1, 0, 3), rawIDTexture_03, ref secondLayerExtendT);
            Utils.SaveRT2Texture(secondLayerExtendT, TextureFormat.RGBA32, GetTextureSavePath("4_5ExtendSecondLayer.tga", m_TerrainAsset.m_TerrainName));
            RenderTexture  thirdLayerExtendT = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchIDLayerExtend(m_TerrainAsset, new Vector4(0, 0, 1, 3), rawIDTexture_03, ref thirdLayerExtendT);
            Utils.SaveRT2Texture(thirdLayerExtendT, TextureFormat.RGBA32, GetTextureSavePath("4_5ExtendThirdLayer.tga", m_TerrainAsset.m_TerrainName));
            // 寻找边界
            RenderTexture extendEdgeTextureST = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchFindIDLayerEdge(m_TerrainAsset, new Vector4(1, 0, 0, 2), secondLayerExtendT, ref extendEdgeTextureST);
            Utils.SaveRT2Texture(extendEdgeTextureST, TextureFormat.RGBA32, GetTextureSavePath("4_6ExtendSecondEdgeLayer.tga", m_TerrainAsset.m_TerrainName));
            RenderTexture extendEdgeTextureTT = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchFindIDLayerEdge(m_TerrainAsset, new Vector4(1, 0, 0, 2), thirdLayerExtendT, ref extendEdgeTextureTT);
            Utils.SaveRT2Texture(extendEdgeTextureTT, TextureFormat.RGBA32, GetTextureSavePath("4_6ExtendThirdEdgeLayer.tga", m_TerrainAsset.m_TerrainName));
            // AlphaMask
            RenderTexture alphaMask_01 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS3LUtils.DispacthFindTransformMask(m_TerrainAsset, extendEdgeTextureST, extendEdgeTextureTT, alphaMask, ref alphaMask_01);
            Utils.SaveRT2Texture(alphaMask_01, TextureFormat.RGBA32, GetTextureSavePath("4_7AlphaMask.tga", m_TerrainAsset.m_TerrainName));
            // 消除扩展边
            RenderTexture rawIDEdgeTextureS_03 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            RenderTexture rawIDEdgeTextureT_03 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            RenderTexture rawIDTexture_04 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS3LUtils.DispacthTransformDimension(m_TerrainAsset, extendEdgeTextureST, extendEdgeTextureTT, alphaMask_01, rawIDTexture_03, 
                ref rawIDEdgeTextureS_03, ref rawIDEdgeTextureT_03, ref rawIDTexture_04);
            Utils.SaveRT2Texture(rawIDEdgeTextureS_03, TextureFormat.RGBA32, GetTextureSavePath("4_8RawIDEdgeTextureS.tga", m_TerrainAsset.m_TerrainName));
            Utils.SaveRT2Texture(rawIDEdgeTextureT_03, TextureFormat.RGBA32, GetTextureSavePath("4_8RawIDEdgeTextureT.tga", m_TerrainAsset.m_TerrainName));
            Utils.SaveRT2Texture(rawIDTexture_04, TextureFormat.RGBA32, GetTextureSavePath("4_8RawIDTexture.tga", m_TerrainAsset.m_TerrainName));
            // 混合 R: extendEdgeTextureD_03 G: rawIDEdgeTextureS_03 B: rawIDEdgeTextureT_03 A: alphaMask_01
            
#endregion
            rawIDEdgeTextureS.Release();
            rawIDEdgeTextureT.Release();
            rawIDEdgeTextureS_01.Release();
            rawIDEdgeTextureT_01.Release();
            alphaInput.Release();
            alphaMask.Release();
            rawIDEdgeTextureS_02.Release();
            rawIDEdgeTextureT_02.Release();
            rawIDTexture_03.Release();
            secondLayerExtendT.Release();
            thirdLayerExtendT.Release();
            extendEdgeTextureST.Release();
            extendEdgeTextureTT.Release();
            alphaMask_01.Release();
            rawIDEdgeTextureS_03.Release();
            rawIDEdgeTextureT_03.Release();
            rawIDTexture_04.Release();

            rawIDTexture.Release();
            rawIDTexture_02.Release();
            m_DoubleAreaBlend.Release();
            indexRank.Release();
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
