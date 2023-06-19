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
        
        public void Update2LayersBlend(bool outputDebugTexture, ref RenderTexture rawIDTexture, ref RenderTexture rawIDResult, ref RenderTexture rChannelMask)
        {
            if (m_TerrainAsset == null || BlendCS2LUtils.Compute == null) return;
            m_TerrainAsset.m_ThreadGroups = Mathf.CeilToInt(m_TerrainAsset.m_AlphamapResolution / 8);
            BlendCS2LUtils.Compute.SetInts(ShaderParams.s_TerrainParamsID, m_TerrainAsset.m_LayersCount, m_TerrainAsset.m_AlphamapResolution, 0, 0);
            ComputeBuffer indexRank = CreateAndGetBuffer(m_TerrainAsset.m_CoverageIndexRank, sizeof(uint));

            // 输出原始的IDTexture贴图
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
            BlendCS2LUtils.DispatchCheckLayerSimilarEdge(m_TerrainAsset, indexRank, rawIDEdgeTexture, rawIDTexture, ref rawIDEdgeTexture_Similar);
            if (outputDebugTexture)
                Utils.SaveRT2Texture(rawIDEdgeTexture_Similar, TextureFormat.RGBA32, GetTextureSavePath("2Layers/3RawIDEdgeTexture_Similar.tga", m_TerrainAsset.m_TerrainName));
            
            // 处理相交处: 将G转移到B通道
            RenderTexture rawIDTexture_01 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            RenderTexture rawIDEdgeTexture_G2B = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchTransformDimension(m_TerrainAsset, rawIDTexture, rawIDEdgeTexture_Similar, ref rawIDTexture_01);
            if (outputDebugTexture)
                Utils.SaveRT2Texture(rawIDTexture_01, TextureFormat.RGBA32, GetTextureSavePath("2Layers/4RawIDTexture_01.tga", m_TerrainAsset.m_TerrainName));

            // Layer扩展
            RenderTexture layerExtend = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchIDLayerExtend(m_TerrainAsset, new Vector4(0, 1, 0, 2), rawIDTexture_01, ref layerExtend);
            if (outputDebugTexture)
                Utils.SaveRT2Texture(layerExtend, TextureFormat.RGBA32, GetTextureSavePath("2Layers/5LayerExtend.tga", m_TerrainAsset.m_TerrainName));
            
            // 处理扩展后的相交处
            BlendCS2LUtils.DispatchCheckExtendLayerEdge(m_TerrainAsset, rawIDTexture_01, layerExtend, ref rawIDResult, ref rChannelMask);
            if (outputDebugTexture)
            {
                Utils.SaveRT2Texture(rawIDResult, TextureFormat.RGBA32, GetTextureSavePath("2Layers/6RawIDTexture_02.tga", m_TerrainAsset.m_TerrainName));
                Utils.SaveRT2Texture(rChannelMask, TextureFormat.RGBA32, GetTextureSavePath("2Layers/6ChannelMask.tga", m_TerrainAsset.m_TerrainName));
                // 再次扩展
                RenderTexture layerExtend_R = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
                BlendCS2LUtils.DispatchIDLayerExtend(m_TerrainAsset, new Vector4(0, 1, 0, 2), rawIDResult, ref layerExtend_R);
                Utils.SaveRT2Texture(layerExtend_R, TextureFormat.RGBA32, GetTextureSavePath("2Layers/7LayerExtend_R.tga", m_TerrainAsset.m_TerrainName));
                RenderTexture layerExtend_B = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
                BlendCS2LUtils.DispatchIDLayerExtend(m_TerrainAsset, new Vector4(0, 0, 1, 5), rawIDResult, ref layerExtend_B);
                Utils.SaveRT2Texture(layerExtend_B, TextureFormat.RGBA32, GetTextureSavePath("2Layers/7LayerExtend_B.tga", m_TerrainAsset.m_TerrainName));
                // 双层混合结构输出
                BlendCS2LUtils.Compute.SetTexture(BlendCS2LUtils.DoubleLayersBlend, ShaderParams.s_TexInput1ID, layerExtend_R);
                BlendCS2LUtils.Compute.SetTexture(BlendCS2LUtils.DoubleLayersBlend, ShaderParams.s_TexInput2ID, layerExtend_B);
                BlendCS2LUtils.Compute.SetTexture(BlendCS2LUtils.DoubleLayersBlend, ShaderParams.s_AlphaTextureArrayID, m_TerrainAsset.m_AlphaTextureArray);
                RenderTexture doubleAreaBlend = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
                BlendCS2LUtils.Compute.SetTexture(BlendCS2LUtils.DoubleLayersBlend, ShaderParams.s_Result1ID, doubleAreaBlend);
                BlendCS2LUtils.Compute.Dispatch(BlendCS2LUtils.DoubleLayersBlend, m_TerrainAsset.m_ThreadGroups, m_TerrainAsset.m_ThreadGroups, 1);
                Utils.SaveRT2Texture(doubleAreaBlend, TextureFormat.RGBA32, GetTextureSavePath("2Layers/8DoubleAreaBlend.tga", m_TerrainAsset.m_TerrainName));
                // Debug  
                BlendCS2LUtils.Compute.SetTexture(4, ShaderParams.s_TexInput1ID, rawIDTexture);
                RenderTexture rgEdge = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
                BlendCS2LUtils.Compute.SetTexture(4, ShaderParams.s_Result1ID, rgEdge);
                BlendCS2LUtils.Compute.Dispatch(4, m_TerrainAsset.m_ThreadGroups, m_TerrainAsset.m_ThreadGroups, 1);
                Utils.SaveRT2Texture(rgEdge, TextureFormat.RGBA32, GetTextureSavePath("2Layers/9Edge.tga", m_TerrainAsset.m_TerrainName));

                layerExtend_R.Release();
                layerExtend_B.Release();
                doubleAreaBlend.Release();
            }
            // 第一阶段
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
            BlendCS2LUtils.Compute.SetInts(ShaderParams.s_TerrainParamsID, m_TerrainAsset.m_LayersCount, m_TerrainAsset.m_AlphamapResolution, 1, 1);
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
            BlendCS2LUtils.DispatchCheckLayerSimilarEdge(m_TerrainAsset, indexRank, rawIDEdgeTextureS, rawIDTexture, ref rawIDEdgeTextureS_Similar);
            Utils.SaveRT2Texture(rawIDEdgeTextureS_Similar, TextureFormat.RGBA32, GetTextureSavePath("3Layers/2RawIDEdgeTextureS_Similar.tga", m_TerrainAsset.m_TerrainName));
            RenderTexture rawIDEdgeTextureT_Similar = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchCheckLayerSimilarEdge(m_TerrainAsset, indexRank, rawIDEdgeTextureT, rawIDTexture, ref rawIDEdgeTextureT_Similar);
            Utils.SaveRT2Texture(rawIDEdgeTextureT_Similar, TextureFormat.RGBA32, GetTextureSavePath("3Layers/2RawIDEdgeTextureT_Similar.tga", m_TerrainAsset.m_TerrainName));
            
            // 在余下的Mask中输出: 
            // 1、第二层、三层中都存在的层的ID
            // 2、TODO: 两边值相似的ID
            RenderTexture alphaMask = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS3LUtils.DispacthFindTransformMask(m_TerrainAsset, rawIDEdgeTextureS_Similar, rawIDEdgeTextureT_Similar, rawIDTexture, ref alphaMask);
            RenderTexture alphaMask_Fill = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS3LUtils.DispacthFindNearTransformMask(m_TerrainAsset, rawIDTexture, alphaMask, ref alphaMask_Fill);
            Utils.SaveRT2Texture(alphaMask_Fill, TextureFormat.RGBA32, GetTextureSavePath("3Layers/3AlphaMask.tga", m_TerrainAsset.m_TerrainName));
            
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

            // 获得相交处的Mask
            RenderTexture alphaMask_Extend = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS3LUtils.DispacthFindExtendTransformMask(m_TerrainAsset, layerExtendS, layerExtendT, alphaMask_01, ref alphaMask_Extend);
            Utils.SaveRT2Texture(alphaMask_Extend, TextureFormat.RGBA32, GetTextureSavePath("3Layers/6AlphaMask.tga", m_TerrainAsset.m_TerrainName));

            // 使用相交处的Mask在对RawID和Extend做一次处理
            RenderTexture layerExtendS_01 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            RenderTexture layerExtendT_01 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            RenderTexture rawIDTexture_02 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            RenderTexture alphaMask_Extend_01 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS3LUtils.DispacthTransformExtendDimension(m_TerrainAsset, layerExtendS, layerExtendT, alphaMask_Extend, rawIDTexture_01, 
                ref layerExtendS_01, ref layerExtendT_01, ref rawIDTexture_02, ref alphaMask_Extend_01);
            Utils.SaveRT2Texture(layerExtendS_01, TextureFormat.RGBA32, GetTextureSavePath("3Layers/7ExtendSecondLayer.tga", m_TerrainAsset.m_TerrainName));
            Utils.SaveRT2Texture(layerExtendT_01, TextureFormat.RGBA32, GetTextureSavePath("3Layers/7ExtendThirdLayer.tga", m_TerrainAsset.m_TerrainName));
            Utils.SaveRT2Texture(rawIDTexture_02, TextureFormat.RGBA32, GetTextureSavePath("3Layers/7RawIDTexture.tga", m_TerrainAsset.m_TerrainName));
            Utils.SaveRT2Texture(alphaMask_Extend_01, TextureFormat.RGBA32, GetTextureSavePath("3Layers/7AlphaMask.tga", m_TerrainAsset.m_TerrainName));

            // 处理AlphaMask
            RenderTexture alphaMask_03 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS3LUtils.DispacthCombineAlphaMask(m_TerrainAsset, alphaMask_01, alphaMask_Extend_01, ref alphaMask_03);
            RenderTexture alphaMaskExtend = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchIDLayerExtend(m_TerrainAsset, new Vector4(1, 0, 0, 4), alphaMask_03, ref alphaMaskExtend);
            Utils.SaveRT2Texture(alphaMaskExtend, TextureFormat.RGBA32, GetTextureSavePath("3Layers/8AlphaMaskExtend.tga", m_TerrainAsset.m_TerrainName));
            // 混合
            RenderTexture layerExtendDS = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchIDLayerExtend(m_TerrainAsset, new Vector4(0, 1, 0, 2), rawIDTexture, ref layerExtendDS);
            RenderTexture blendTexture = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);

            BlendCS3LUtils.DispacthThreeLayersBlend(m_TerrainAsset, layerExtendDS, layerExtendS_01, layerExtendT_01, alphaMaskExtend, ref blendTexture);
            Utils.SaveRT2Texture(blendTexture, TextureFormat.RGBA32, GetTextureSavePath("3Layers/9BlendTexture.tga", m_TerrainAsset.m_TerrainName));

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
            rawIDTexture_02.Release();
            alphaMask_Extend.Release();
            alphaMask_Extend_01.Release();

            alphaMask_03.Release();
            alphaMaskExtend.Release();
            layerExtendDS.Release();
            blendTexture.Release();
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
