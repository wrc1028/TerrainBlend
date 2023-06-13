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
        public void Update2LayersBlend(bool outputDebugTexture, ref RenderTexture rawIDResult)
        {
            if (m_TerrainAsset == null || BlendCS2LUtils.Compute == null) return;
            m_TerrainAsset.m_ThreadGroups = Mathf.CeilToInt(m_TerrainAsset.m_AlphamapResolution / 8);
            BlendCS2LUtils.Compute.SetInts(ShaderParams.s_TerrainParamsID, m_TerrainAsset.m_LayersCount, m_TerrainAsset.m_AlphamapResolution, 0);
            ComputeBuffer indexRank = CreateAndGetBuffer(m_TerrainAsset.m_CoverageIndexRank, sizeof(uint));

            // 输出原始的IDTexture贴图
            RenderTexture rawIDTexture = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchRawIDTexture(m_TerrainAsset, indexRank, ref rawIDTexture);
            if (outputDebugTexture)
                Utils.SaveRT2Texture(rawIDTexture, TextureFormat.RGBA32, GetTextureSavePath("2Layers/1RawIDTexture.tga", m_TerrainAsset.m_TerrainName));
            
            // 输出RawIDTexture的区域边界
            RenderTexture rawIDEdgeTexture = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchFindIDLayerEdge(m_TerrainAsset, new Vector4(0, 1, 0, 2), rawIDTexture, ref rawIDEdgeTexture);
            if (outputDebugTexture)    
                Utils.SaveRT2Texture(rawIDEdgeTexture, TextureFormat.RGBA32, GetTextureSavePath("2Layers/2RawIDEdgeTexture.tga", m_TerrainAsset.m_TerrainName));
            
            // 处理相交处: 消除边缘混合值相似的Mask
            RenderTexture rawIDEdgeTexture_Similar = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchCheckLayerSimilarEdge(m_TerrainAsset, indexRank, rawIDEdgeTexture, ref rawIDEdgeTexture_Similar);
            if (outputDebugTexture)
                Utils.SaveRT2Texture(rawIDEdgeTexture_Similar, TextureFormat.RGBA32, GetTextureSavePath("2Layers/3RawIDEdgeTexture_Similar.tga", m_TerrainAsset.m_TerrainName));
            
            // 处理相交处: 将RG转移到GB通道上
            RenderTexture rawIDTexture_01 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchTransformDimension(m_TerrainAsset, rawIDTexture, rawIDEdgeTexture_Similar, ref rawIDTexture_01);
            if (outputDebugTexture)
                Utils.SaveRT2Texture(rawIDTexture_01, TextureFormat.RGBA32, GetTextureSavePath("2Layers/4RawIDTexture_01.tga", m_TerrainAsset.m_TerrainName));

            // Layer扩展
            RenderTexture layerExtend = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchIDLayerExtend(m_TerrainAsset, new Vector4(0, 1, 0, 2), rawIDTexture_01, ref layerExtend);
            if (outputDebugTexture)
                Utils.SaveRT2Texture(layerExtend, TextureFormat.RGBA32, GetTextureSavePath("2Layers/5LayerExtend.tga", m_TerrainAsset.m_TerrainName));
            
            // 处理扩展后的相交处
            BlendCS2LUtils.DispatchCheckExtendLayerEdge(m_TerrainAsset, rawIDTexture_01, layerExtend, ref rawIDResult);
            if (outputDebugTexture)
            {
                Utils.SaveRT2Texture(rawIDResult, TextureFormat.RGBA32, GetTextureSavePath("2Layers/6RawIDTexture_02.tga", m_TerrainAsset.m_TerrainName));
                // 再次扩展
                RenderTexture layerExtend_01 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
                BlendCS2LUtils.DispatchIDLayerExtend(m_TerrainAsset, new Vector4(0, 1, 0, 2), rawIDResult, ref layerExtend_01);
                Utils.SaveRT2Texture(layerExtend_01, TextureFormat.RGBA32, GetTextureSavePath("2Layers/7LayerExtend_01.tga", m_TerrainAsset.m_TerrainName));
                
                // 双层混合结构输出
                BlendCS2LUtils.Compute.SetTexture(BlendCS2LUtils.DoubleLayersBlend, ShaderParams.s_TexInput1ID, layerExtend_01);
                BlendCS2LUtils.Compute.SetTexture(BlendCS2LUtils.DoubleLayersBlend, ShaderParams.s_AlphaTextureArrayID, m_TerrainAsset.m_AlphaTextureArray);
                RenderTexture doubleAreaBlend = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
                BlendCS2LUtils.Compute.SetTexture(BlendCS2LUtils.DoubleLayersBlend, ShaderParams.s_Result1ID, doubleAreaBlend);
                BlendCS2LUtils.Compute.Dispatch(BlendCS2LUtils.DoubleLayersBlend, m_TerrainAsset.m_ThreadGroups, m_TerrainAsset.m_ThreadGroups, 1);
                Utils.SaveRT2Texture(doubleAreaBlend, TextureFormat.RGBA32, GetTextureSavePath("2Layers/8DoubleAreaBlend.tga", m_TerrainAsset.m_TerrainName));
                layerExtend_01.Release();
                doubleAreaBlend.Release();
            }
            // 第一阶段
            rawIDTexture.Release();
            rawIDEdgeTexture.Release();
            rawIDEdgeTexture_Similar.Release();
            rawIDTexture_01.Release();
            // 第二阶段
            layerExtend.Release();
            indexRank.Release();
            AssetDatabase.Refresh();
        }
        public void Update3LayersBlend(RenderTexture rawIDTexture)
        {
            if (m_TerrainAsset == null || BlendCS2LUtils.Compute == null) return;
            m_TerrainAsset.m_ThreadGroups = Mathf.CeilToInt(m_TerrainAsset.m_AlphamapResolution / 8);
            BlendCS2LUtils.Compute.SetInts(ShaderParams.s_TerrainParamsID, m_TerrainAsset.m_LayersCount, m_TerrainAsset.m_AlphamapResolution, 0);
            ComputeBuffer indexRank = CreateAndGetBuffer(m_TerrainAsset.m_CoverageIndexRank, sizeof(uint));

            // 输出RawIDTexture的区域边界
            RenderTexture rawIDEdgeTextureS = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchFindIDLayerEdge(m_TerrainAsset, new Vector4(0, 1, 0, 3), rawIDTexture, ref rawIDEdgeTextureS);
            Utils.SaveRT2Texture(rawIDEdgeTextureS, TextureFormat.RGBA32, GetTextureSavePath("3Layers/1RawIDEdgeTextureS.tga", m_TerrainAsset.m_TerrainName));
            RenderTexture rawIDEdgeTextureT = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchFindIDLayerEdge(m_TerrainAsset, new Vector4(0, 0, 1, 3), rawIDTexture, ref rawIDEdgeTextureT);
            Utils.SaveRT2Texture(rawIDEdgeTextureT, TextureFormat.RGBA32, GetTextureSavePath("3Layers/1RawIDEdgeTextureT.tga", m_TerrainAsset.m_TerrainName));

            // 处理相交处: 消除边缘混合值相似的Mask
            RenderTexture rawIDEdgeTextureS_Similar = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchCheckLayerSimilarEdge(m_TerrainAsset, indexRank, rawIDEdgeTextureS, ref rawIDEdgeTextureS_Similar);
            Utils.SaveRT2Texture(rawIDEdgeTextureS_Similar, TextureFormat.RGBA32, GetTextureSavePath("3Layers/2RawIDEdgeTextureS_Similar.tga", m_TerrainAsset.m_TerrainName));
            RenderTexture rawIDEdgeTextureT_Similar = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchCheckLayerSimilarEdge(m_TerrainAsset, indexRank, rawIDEdgeTextureT, ref rawIDEdgeTextureT_Similar);
            Utils.SaveRT2Texture(rawIDEdgeTextureT_Similar, TextureFormat.RGBA32, GetTextureSavePath("3Layers/2RawIDEdgeTextureT_Similar.tga", m_TerrainAsset.m_TerrainName));
            
            // 在余下的Mask中, 输出第二层、三层中都存在的层的ID
            RenderTexture alphaMask = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS3LUtils.DispacthFindTransformMask(m_TerrainAsset, rawIDEdgeTextureS_Similar, rawIDEdgeTextureT_Similar, rawIDTexture, ref alphaMask);
            Utils.SaveRT2Texture(alphaMask, TextureFormat.RGBA32, GetTextureSavePath("3Layers/3AlphaMask.tga", m_TerrainAsset.m_TerrainName));
            
            // 根据上面的Mask, 对RawIDTexture中各自区域内的ID进行处理
            RenderTexture rawIDEdgeTextureS_01 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            RenderTexture rawIDEdgeTextureT_01 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            RenderTexture rawIDTexture_01 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            RenderTexture alphaMask_01 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS3LUtils.DispacthTransformDimension(m_TerrainAsset, rawIDEdgeTextureS_Similar, rawIDEdgeTextureT_Similar, alphaMask, rawIDTexture, 
                ref rawIDEdgeTextureS_01, ref rawIDEdgeTextureT_01, ref rawIDTexture_01, ref alphaMask_01);
            Utils.SaveRT2Texture(rawIDEdgeTextureS_01, TextureFormat.RGBA32, GetTextureSavePath("3Layers/4RawIDEdgeTextureS.tga", m_TerrainAsset.m_TerrainName));
            Utils.SaveRT2Texture(rawIDEdgeTextureT_01, TextureFormat.RGBA32, GetTextureSavePath("3Layers/4RawIDEdgeTextureT.tga", m_TerrainAsset.m_TerrainName));
            Utils.SaveRT2Texture(rawIDTexture_01, TextureFormat.RGBA32, GetTextureSavePath("3Layers/4RawIDTexture.tga", m_TerrainAsset.m_TerrainName));
            Utils.SaveRT2Texture(alphaMask_01, TextureFormat.RGBA32, GetTextureSavePath("3Layers/4AlphaMask.tga", m_TerrainAsset.m_TerrainName));

            // 扩展
            RenderTexture layerExtendS = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchIDLayerExtend(m_TerrainAsset, new Vector4(1, 0, 0, 4), rawIDEdgeTextureS_01, ref layerExtendS);
            Utils.SaveRT2Texture(layerExtendS, TextureFormat.RGBA32, GetTextureSavePath("3Layers/5ExtendSecondLayer.tga", m_TerrainAsset.m_TerrainName));
            RenderTexture layerExtendT = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchIDLayerExtend(m_TerrainAsset, new Vector4(1, 0, 0, 4), rawIDEdgeTextureT_01, ref layerExtendT);
            Utils.SaveRT2Texture(layerExtendT, TextureFormat.RGBA32, GetTextureSavePath("3Layers/5ExtendThirdLayer.tga", m_TerrainAsset.m_TerrainName));

            // 处理扩展后的相交处
            RenderTexture layerExtendS_01 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            RenderTexture layerExtendT_01 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            RenderTexture alphaMask_02 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS3LUtils.DispacthCheckExtendLayerEdge(m_TerrainAsset, layerExtendS, layerExtendT, alphaMask_01, 
                ref layerExtendS_01, ref layerExtendT_01, ref alphaMask_02);
            Utils.SaveRT2Texture(layerExtendS_01, TextureFormat.RGBA32, GetTextureSavePath("3Layers/6ExtendSecondLayer.tga", m_TerrainAsset.m_TerrainName));
            Utils.SaveRT2Texture(layerExtendT_01, TextureFormat.RGBA32, GetTextureSavePath("3Layers/6ExtendThirdLayer.tga", m_TerrainAsset.m_TerrainName));
            Utils.SaveRT2Texture(alphaMask_02, TextureFormat.RGBA32, GetTextureSavePath("3Layers/6AlphaMask.tga", m_TerrainAsset.m_TerrainName));

            // 处理AlphaMask
            RenderTexture alphaMask_03 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS3LUtils.DispacthCombineAlphaMask(m_TerrainAsset, alphaMask_02, ref alphaMask_03);
            RenderTexture alphaMaskExtend = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchIDLayerExtend(m_TerrainAsset, new Vector4(1, 0, 0, 4), alphaMask_03, ref alphaMaskExtend);
            Utils.SaveRT2Texture(alphaMaskExtend, TextureFormat.RGBA32, GetTextureSavePath("3Layers/7AlphaMaskExtend.tga", m_TerrainAsset.m_TerrainName));
            // 混合
            RenderTexture layerExtendDS = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchIDLayerExtend(m_TerrainAsset, new Vector4(0, 1, 0, 2), rawIDTexture, ref layerExtendDS);
            RenderTexture blendTexture = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);

            BlendCS3LUtils.DispacthThreeLayersBlend(m_TerrainAsset, layerExtendDS, layerExtendS_01, layerExtendT_01, alphaMaskExtend, ref blendTexture);
            Utils.SaveRT2Texture(blendTexture, TextureFormat.RGBA32, GetTextureSavePath("3Layers/8BlendTexture.tga", m_TerrainAsset.m_TerrainName));

            rawIDEdgeTextureS.Release();
            rawIDEdgeTextureT.Release();
            rawIDEdgeTextureS_Similar.Release();
            rawIDEdgeTextureT_Similar.Release();
            alphaMask.Release();

            rawIDEdgeTextureS_01.Release();
            rawIDEdgeTextureT_01.Release();
            rawIDTexture_01.Release();

            layerExtendS.Release();
            layerExtendT.Release();

            layerExtendS_01.Release();
            layerExtendT_01.Release();
            alphaMask_02.Release();
            blendTexture.Release();
            alphaMaskExtend.Release();
            layerExtendDS.Release();
            indexRank.Release();
            AssetDatabase.Refresh();
        }
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
            Utils.SaveRT2Texture(rawIDTexture, TextureFormat.RGBA32, GetTextureSavePath("2Layers/1RawIDTexture.tga", m_TerrainAsset.m_TerrainName));
            // 输出RawIDTexture的边界
            RenderTexture rawIDEdgeTexture = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchFindIDLayerEdge(m_TerrainAsset, new Vector4(0, 1, 0, 2), rawIDTexture, ref rawIDEdgeTexture);
            Utils.SaveRT2Texture(rawIDEdgeTexture, TextureFormat.RGBA32, GetTextureSavePath("2Layers/2RawIDEdgeTexture.tga", m_TerrainAsset.m_TerrainName));
            // 处理相交处: 消除边缘混合值相似的Mask
            RenderTexture rawIDEdgeTexture_Similar = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchCheckLayerSimilarEdge(m_TerrainAsset, indexRank, rawIDEdgeTexture, ref rawIDEdgeTexture_Similar);
            Utils.SaveRT2Texture(rawIDEdgeTexture_Similar, TextureFormat.RGBA32, GetTextureSavePath("2Layers/3RawIDEdgeTexture_Similar.tga", m_TerrainAsset.m_TerrainName));
            // 处理相交处: 将RG转移到GB通道上
            RenderTexture rawIDTexture_01 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchTransformDimension(m_TerrainAsset, rawIDTexture, rawIDEdgeTexture_Similar, ref rawIDTexture_01);
            Utils.SaveRT2Texture(rawIDTexture_01, TextureFormat.RGBA32, GetTextureSavePath("2Layers/4RawIDTexture_01.tga", m_TerrainAsset.m_TerrainName));
            
            // Layer扩展
            RenderTexture layerExtendDS = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchIDLayerExtend(m_TerrainAsset, new Vector4(0, 1, 0, 2), rawIDTexture_01, ref layerExtendDS);
            Utils.SaveRT2Texture(layerExtendDS, TextureFormat.RGBA32, GetTextureSavePath("2Layers/5LayerExtend.tga", m_TerrainAsset.m_TerrainName));
            // 处理相交处: 处理用于扩展维度的Mask, 因为有些是正常向外扩展没有相交情况, 或者扩充的位置不位于双层结构内
            RenderTexture rawIDTexture_02 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchCheckExtendLayerEdge(m_TerrainAsset, rawIDTexture_01, layerExtendDS, ref rawIDTexture_02);
            Utils.SaveRT2Texture(rawIDTexture_02, TextureFormat.RGBA32, GetTextureSavePath("2Layers/6RawIDTexture_02.tga", m_TerrainAsset.m_TerrainName));
            // 再次扩展
            RenderTexture layerExtendDS_01 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchIDLayerExtend(m_TerrainAsset, new Vector4(0, 1, 0, 2), rawIDTexture_02, ref layerExtendDS_01);
            Utils.SaveRT2Texture(layerExtendDS_01, TextureFormat.RGBA32, GetTextureSavePath("2Layers/7LayerExtend_01.tga", m_TerrainAsset.m_TerrainName));
            // 双层混合结构输出
            // BlendCS2LUtils.Compute.SetTexture(BlendCS2LUtils.DoubleLayersBlend, ShaderParams.s_TexInput1ID, layerExtendDS_01);
            // BlendCS2LUtils.Compute.SetTexture(BlendCS2LUtils.DoubleLayersBlend, ShaderParams.s_AlphaTextureArrayID, m_TerrainAsset.m_AlphaTextureArray);
            // RenderTexture doubleAreaBlend = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            // BlendCS2LUtils.Compute.SetTexture(BlendCS2LUtils.DoubleLayersBlend, ShaderParams.s_Result1ID, doubleAreaBlend);
            // BlendCS2LUtils.Compute.Dispatch(BlendCS2LUtils.DoubleLayersBlend, m_TerrainAsset.m_ThreadGroups, m_TerrainAsset.m_ThreadGroups, 1);
            // Utils.SaveRT2Texture(doubleAreaBlend, TextureFormat.RGBA32, GetTextureSavePath("2Layers/8DoubleAreaBlend.tga", m_TerrainAsset.m_TerrainName));

            // 第一阶段
            rawIDTexture.Release();
            rawIDEdgeTexture.Release();
            rawIDEdgeTexture_Similar.Release();
            rawIDTexture_01.Release();
            // 第二阶段
            layerExtendDS.Release();
            rawIDTexture_02.Release();
            layerExtendDS_01.Release();
            
            indexRank.Release();
            AssetDatabase.Refresh();
            return;
#endregion
#region 三层混合区域
//             // AlphaMask
//             RenderTexture alphaInput = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
//             RenderTexture alphaMask = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
//             BlendCS3LUtils.DispacthFindTransformMask(m_TerrainAsset, rawIDEdgeTextureS_01, rawIDEdgeTextureT_01, alphaInput, ref alphaMask);
//             Utils.SaveRT2Texture(alphaMask, TextureFormat.RGBA32, GetTextureSavePath("4_3AlphaMask.tga", m_TerrainAsset.m_TerrainName));
//             // 消除原始边界
//             RenderTexture rawIDEdgeTextureS_02 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
//             RenderTexture rawIDEdgeTextureT_02 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
//             RenderTexture rawIDTexture_03 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
//             BlendCS3LUtils.DispacthTransformDimension(m_TerrainAsset, rawIDEdgeTextureS_01, rawIDEdgeTextureT_01, alphaMask, rawIDTexture_02, 
//                 ref rawIDEdgeTextureS_02, ref rawIDEdgeTextureT_02, ref rawIDTexture_03);
//             Utils.SaveRT2Texture(rawIDEdgeTextureS_02, TextureFormat.RGBA32, GetTextureSavePath("4_4RawIDEdgeTextureS_02.tga", m_TerrainAsset.m_TerrainName));
//             Utils.SaveRT2Texture(rawIDEdgeTextureT_02, TextureFormat.RGBA32, GetTextureSavePath("4_4RawIDEdgeTextureT_02.tga", m_TerrainAsset.m_TerrainName));
//             Utils.SaveRT2Texture(rawIDTexture_03, TextureFormat.RGBA32, GetTextureSavePath("4_4RawIDTexture_03.tga", m_TerrainAsset.m_TerrainName));
//             // 扩展
//             RenderTexture secondLayerExtendT = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
//             BlendCS2LUtils.DispatchIDLayerExtend(m_TerrainAsset, new Vector4(0, 1, 0, 3), rawIDTexture_03, ref secondLayerExtendT);
//             Utils.SaveRT2Texture(secondLayerExtendT, TextureFormat.RGBA32, GetTextureSavePath("4_5ExtendSecondLayer.tga", m_TerrainAsset.m_TerrainName));
//             RenderTexture  thirdLayerExtendT = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
//             BlendCS2LUtils.DispatchIDLayerExtend(m_TerrainAsset, new Vector4(0, 0, 1, 3), rawIDTexture_03, ref thirdLayerExtendT);
//             Utils.SaveRT2Texture(thirdLayerExtendT, TextureFormat.RGBA32, GetTextureSavePath("4_5ExtendThirdLayer.tga", m_TerrainAsset.m_TerrainName));
//             // 寻找边界
//             RenderTexture extendEdgeTextureST = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
//             BlendCS2LUtils.DispatchFindIDLayerEdge(m_TerrainAsset, new Vector4(1, 0, 0, 2), secondLayerExtendT, ref extendEdgeTextureST);
//             Utils.SaveRT2Texture(extendEdgeTextureST, TextureFormat.RGBA32, GetTextureSavePath("4_6ExtendSecondEdgeLayer.tga", m_TerrainAsset.m_TerrainName));
//             RenderTexture extendEdgeTextureTT = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
//             BlendCS2LUtils.DispatchFindIDLayerEdge(m_TerrainAsset, new Vector4(1, 0, 0, 2), thirdLayerExtendT, ref extendEdgeTextureTT);
//             Utils.SaveRT2Texture(extendEdgeTextureTT, TextureFormat.RGBA32, GetTextureSavePath("4_6ExtendThirdEdgeLayer.tga", m_TerrainAsset.m_TerrainName));
//             // AlphaMask
//             RenderTexture alphaMask_01 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
//             BlendCS3LUtils.DispacthFindTransformMask(m_TerrainAsset, extendEdgeTextureST, extendEdgeTextureTT, alphaMask, ref alphaMask_01);
//             Utils.SaveRT2Texture(alphaMask_01, TextureFormat.RGBA32, GetTextureSavePath("4_7AlphaMask.tga", m_TerrainAsset.m_TerrainName));
//             // 消除扩展边
//             RenderTexture rawIDEdgeTextureS_03 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
//             RenderTexture rawIDEdgeTextureT_03 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
//             RenderTexture rawIDTexture_04 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
//             BlendCS3LUtils.DispacthTransformDimension(m_TerrainAsset, extendEdgeTextureST, extendEdgeTextureTT, alphaMask_01, rawIDTexture_03, 
//                 ref rawIDEdgeTextureS_03, ref rawIDEdgeTextureT_03, ref rawIDTexture_04);
//             Utils.SaveRT2Texture(rawIDEdgeTextureS_03, TextureFormat.RGBA32, GetTextureSavePath("4_8RawIDEdgeTextureS.tga", m_TerrainAsset.m_TerrainName));
//             Utils.SaveRT2Texture(rawIDEdgeTextureT_03, TextureFormat.RGBA32, GetTextureSavePath("4_8RawIDEdgeTextureT.tga", m_TerrainAsset.m_TerrainName));
//             Utils.SaveRT2Texture(rawIDTexture_04, TextureFormat.RGBA32, GetTextureSavePath("4_8RawIDTexture.tga", m_TerrainAsset.m_TerrainName));
//             // 混合 R: extendEdgeTextureD_03 G: rawIDEdgeTextureS_03 B: rawIDEdgeTextureT_03 A: alphaMask_01
//             RenderTexture  alphaLayerExtendT = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
//             BlendCS2LUtils.DispatchIDLayerExtend(m_TerrainAsset, new Vector4(1, 0, 0, 2), alphaMask_01, ref alphaLayerExtendT);
//             RenderTexture  alphaLayerExtendT_2 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
//             BlendCS2LUtils.DispatchIDLayerExtend(m_TerrainAsset, new Vector4(1, 0, 0, 2), alphaLayerExtendT, ref alphaLayerExtendT_2);
//             RenderTexture  rawIDEdgeTextureS_04 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
//             BlendCS2LUtils.DispatchIDLayerExtend(m_TerrainAsset, new Vector4(0, 1, 0, 3), rawIDTexture_04, ref rawIDEdgeTextureS_04);
//             RenderTexture  rawIDEdgeTextureT_04 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
//             BlendCS2LUtils.DispatchIDLayerExtend(m_TerrainAsset, new Vector4(0, 0, 1, 3), rawIDTexture_04, ref rawIDEdgeTextureT_04);
//             RenderTexture blendTexture = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
//             BlendCS3LUtils.DispacthThreeLayersBlend(m_TerrainAsset, extendEdgeTextureD_Dimension, rawIDEdgeTextureS_04, rawIDEdgeTextureT_04, alphaLayerExtendT_2, ref blendTexture);
//             Utils.SaveRT2Texture(blendTexture, TextureFormat.RGBA32, GetTextureSavePath("4_9BlendTexture.tga", m_TerrainAsset.m_TerrainName));
            
#endregion
//             rawIDEdgeTextureS.Release();
//             rawIDEdgeTextureT.Release();
//             rawIDEdgeTextureS_01.Release();
//             rawIDEdgeTextureT_01.Release();
//             alphaInput.Release();
//             alphaMask.Release();
//             rawIDEdgeTextureS_02.Release();
//             rawIDEdgeTextureT_02.Release();
//             rawIDTexture_03.Release();
//             secondLayerExtendT.Release();
//             thirdLayerExtendT.Release();
//             extendEdgeTextureST.Release();
//             extendEdgeTextureTT.Release();
//             alphaMask_01.Release();
//             rawIDEdgeTextureS_03.Release();
//             rawIDEdgeTextureT_03.Release();
//             extendEdgeTextureD_Dimension.Release();
//             rawIDTexture_04.Release();
//             blendTexture.Release();

//             rawIDTexture.Release();
//             rawIDTexture_02.Release();
//             indexRank.Release();
//             AssetDatabase.Refresh();
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
