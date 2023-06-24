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
        
        public void Update2LayersBlend(bool outputDebugTexture, ref RenderTexture rawIDResult, ref RenderTexture rawBlendResult)
        {
            if (m_TerrainAsset == null || BlendCS2LUtils.Compute == null) return;
            m_TerrainAsset.m_ThreadGroups = Mathf.CeilToInt(m_TerrainAsset.m_AlphamapResolution / 8);
            // R: 地形混合层数; G: 处理贴图分辨率; B: 混合模式; A: 地形索引
            BlendCS2LUtils.Compute.SetInts(ShaderParams.s_TerrainParamsID, m_TerrainAsset.m_LayersCount, m_TerrainAsset.m_AlphamapResolution, 0, 0);
            ComputeBuffer indexRank = CreateAndGetBuffer(m_TerrainAsset.m_CoverageIndexRank, sizeof(uint));

            // 输出原始的IDTexture贴图
            RenderTexture rawIDTexture = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchRawIDTexture(m_TerrainAsset, indexRank, ref rawIDTexture);
            if (outputDebugTexture)
                Utils.SaveRT2Texture(rawIDTexture, TextureFormat.RGBA32, GetTextureSavePath("2Layers/1RawIDTexture.tga", m_TerrainAsset.m_TerrainName));

            // 擦除双层结构层次以外的RawIDMaskArray
            RenderTexture rawIDMaskArrayErase = Utils.CreateRenderTexture3D(m_TerrainAsset.m_AlphamapResolution, m_TerrainAsset.m_LayersCount, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchIDMaskErase(m_TerrainAsset, new Vector4(0, 1, 0, 2), indexRank, rawIDTexture, ref rawIDMaskArrayErase);
            // 扩展擦除后的RawIDMask
            RenderTexture rawIDMaskArrayExtend = Utils.CreateRenderTexture3D(m_TerrainAsset.m_AlphamapResolution, m_TerrainAsset.m_LayersCount, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchIDMaskExtend(m_TerrainAsset, rawIDMaskArrayErase, ref rawIDMaskArrayExtend);
            // 在有重叠的位置进行判断
            RenderTexture rawIDLayerEdge = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchCheckIDLayerEdge(m_TerrainAsset, rawIDMaskArrayExtend, ref rawIDLayerEdge);
            if (outputDebugTexture)
                Utils.SaveRT2Texture(rawIDLayerEdge, TextureFormat.RGBA32, GetTextureSavePath("2Layers/2RawIDLayerEdge.tga", m_TerrainAsset.m_TerrainName));
            // 输出 RawIDResult
            // RenderTexture rawIDResult = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchTransformG2B(m_TerrainAsset, rawIDTexture, rawIDLayerEdge, ref rawIDResult);
            if (outputDebugTexture)
                Utils.SaveRT2Texture(rawIDResult, TextureFormat.RGBA32, GetTextureSavePath("2Layers/3RawIDResult.tga", m_TerrainAsset.m_TerrainName));
            // 输出双层混合结构的 RawBlendResult
            // RenderTexture rawBlendResult = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchDoubleLayersBlend(m_TerrainAsset, rawIDResult, rawIDLayerEdge, ref rawBlendResult);
            if (outputDebugTexture)
                Utils.SaveRT2Texture(rawBlendResult, TextureFormat.RGBA32, GetTextureSavePath("2Layers/4RawBlendResult.tga", m_TerrainAsset.m_TerrainName));
            
            // Debug RenderTextureArray
            // for (int i = 0; i < m_TerrainAsset.m_LayersCount; i++)
            // {
            //     RenderTexture rawIDMaskErase = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            //     Graphics.CopyTexture(rawIDMaskArrayExtend, i, 0, rawIDMaskErase, 0, 0);
            //     Utils.SaveRT2Texture(rawIDMaskErase, TextureFormat.ARGB32, GetTextureSavePath($"2Layers/Debug/rawIDMaskErase{i}.tga", m_TerrainAsset.m_TerrainName));
            //     rawIDMaskErase.Release();
            // }
            indexRank.Release();
            rawIDTexture.Release();
            rawIDMaskArrayErase.Release();
            rawIDMaskArrayExtend.Release();
            rawIDLayerEdge.Release();
            // rawIDResult.Release();
            // rawBlendResult.Release();
            AssetDatabase.Refresh();
        }
        public void Update3LayersBlend(RenderTexture rawIDTexture, RenderTexture rawBlendTexture)
        {
            if (m_TerrainAsset == null || BlendCS2LUtils.Compute == null) return;
            m_TerrainAsset.m_ThreadGroups = Mathf.CeilToInt(m_TerrainAsset.m_AlphamapResolution / 8);
            BlendCS2LUtils.Compute.SetInts(ShaderParams.s_TerrainParamsID, m_TerrainAsset.m_LayersCount, m_TerrainAsset.m_AlphamapResolution, 1, 0);
            ComputeBuffer indexRank = CreateAndGetBuffer(m_TerrainAsset.m_CoverageIndexRank, sizeof(uint));
            // 擦除三层结构层次以外的RawIDMaskArray, 注意区分G2B的区域
            RenderTexture rawIDMaskArrayEraseG = Utils.CreateRenderTexture3D(m_TerrainAsset.m_AlphamapResolution, m_TerrainAsset.m_LayersCount, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchIDMaskErase(m_TerrainAsset, new Vector4(0, 1, 0, 3), indexRank, rawIDTexture, ref rawIDMaskArrayEraseG);
            RenderTexture rawIDMaskArrayEraseB = Utils.CreateRenderTexture3D(m_TerrainAsset.m_AlphamapResolution, m_TerrainAsset.m_LayersCount, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchIDMaskErase(m_TerrainAsset, new Vector4(0, 0, 1, 3), indexRank, rawIDTexture, ref rawIDMaskArrayEraseB);
            // 扩展擦除后的RawIDMask
            RenderTexture rawIDMaskArrayExtendG = Utils.CreateRenderTexture3D(m_TerrainAsset.m_AlphamapResolution, m_TerrainAsset.m_LayersCount, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchIDMaskExtend(m_TerrainAsset, rawIDMaskArrayEraseG, ref rawIDMaskArrayExtendG);
            RenderTexture rawIDMaskArrayExtendB = Utils.CreateRenderTexture3D(m_TerrainAsset.m_AlphamapResolution, m_TerrainAsset.m_LayersCount, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchIDMaskExtend(m_TerrainAsset, rawIDMaskArrayEraseB, ref rawIDMaskArrayExtendB);
            // 在有重叠的位置进行判断
            RenderTexture rawIDLayerEdgeG = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchCheckIDLayerEdge(m_TerrainAsset, rawIDMaskArrayExtendG, ref rawIDLayerEdgeG);
            RenderTexture rawIDLayerEdgeB = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchCheckIDLayerEdge(m_TerrainAsset, rawIDMaskArrayExtendB, ref rawIDLayerEdgeB);
            Utils.SaveRT2Texture(rawIDLayerEdgeG, TextureFormat.RGBA32, GetTextureSavePath("3Layers/1_1RawIDLayerEdgeG.tga", m_TerrainAsset.m_TerrainName));
            Utils.SaveRT2Texture(rawIDLayerEdgeB, TextureFormat.RGBA32, GetTextureSavePath("3Layers/1_2RawIDLayerEdgeB.tga", m_TerrainAsset.m_TerrainName));
            // 查找可以转移到A通道的ID, 比如: 
            // 1、在相同区域内GB都存在的ID;
            // 2、混合值相似的情况
            RenderTexture alphaMaskG = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS3LUtils.DispacthCheckSameLayerID(m_TerrainAsset, new Vector4(0, 1, 0, 3), rawIDTexture, rawIDLayerEdgeG, ref alphaMaskG);
            Utils.SaveRT2Texture(alphaMaskG, TextureFormat.RGBA32, GetTextureSavePath("3Layers/2_1AlphaMaskG.tga", m_TerrainAsset.m_TerrainName));

            
            rawIDMaskArrayEraseG.Release();
            rawIDMaskArrayEraseB.Release();
            rawIDMaskArrayExtendG.Release();
            rawIDMaskArrayExtendB.Release();
            rawIDLayerEdgeG.Release();
            rawIDLayerEdgeB.Release();
            alphaMaskG.Release();

            indexRank.Release();
            AssetDatabase.Refresh();
        }
        // public void Update3LayersBlend(RenderTexture rawIDTexture)
        // {
        //     if (m_TerrainAsset == null || BlendCS2LUtils.Compute == null) return;
        //     m_TerrainAsset.m_ThreadGroups = Mathf.CeilToInt(m_TerrainAsset.m_AlphamapResolution / 8);
        //     BlendCS2LUtils.Compute.SetInts(ShaderParams.s_TerrainParamsID, m_TerrainAsset.m_LayersCount, m_TerrainAsset.m_AlphamapResolution, 1, 1);
        //     ComputeBuffer indexRank = CreateAndGetBuffer(m_TerrainAsset.m_CoverageIndexRank, sizeof(uint));

        //     // 输出RawIDTexture的区域边界
        //     RenderTexture rawIDEdgeTextureS = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
        //     BlendCS2LUtils.DispatchFindIDLayerEdge(m_TerrainAsset, new Vector4(0, 1, 0, 3), rawIDTexture, ref rawIDEdgeTextureS);
        //     Utils.SaveRT2Texture(rawIDEdgeTextureS, TextureFormat.RGBA32, GetTextureSavePath("3Layers/1RawIDEdgeTextureS.tga", m_TerrainAsset.m_TerrainName));
        //     RenderTexture rawIDEdgeTextureT = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
        //     BlendCS2LUtils.DispatchFindIDLayerEdge(m_TerrainAsset, new Vector4(0, 0, 1, 3), rawIDTexture, ref rawIDEdgeTextureT);
        //     Utils.SaveRT2Texture(rawIDEdgeTextureT, TextureFormat.RGBA32, GetTextureSavePath("3Layers/1RawIDEdgeTextureT.tga", m_TerrainAsset.m_TerrainName));

        //     // 处理相交处: 消除边缘混合值相似的Mask
        //     RenderTexture rawIDEdgeTextureS_Similar = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
        //     BlendCS2LUtils.DispatchCheckLayerSimilarEdge(m_TerrainAsset, indexRank, rawIDEdgeTextureS, rawIDTexture, ref rawIDEdgeTextureS_Similar);
        //     Utils.SaveRT2Texture(rawIDEdgeTextureS_Similar, TextureFormat.RGBA32, GetTextureSavePath("3Layers/2RawIDEdgeTextureS_Similar.tga", m_TerrainAsset.m_TerrainName));
        //     RenderTexture rawIDEdgeTextureT_Similar = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
        //     BlendCS2LUtils.DispatchCheckLayerSimilarEdge(m_TerrainAsset, indexRank, rawIDEdgeTextureT, rawIDTexture, ref rawIDEdgeTextureT_Similar);
        //     Utils.SaveRT2Texture(rawIDEdgeTextureT_Similar, TextureFormat.RGBA32, GetTextureSavePath("3Layers/2RawIDEdgeTextureT_Similar.tga", m_TerrainAsset.m_TerrainName));
            
        //     // 在余下的Mask中输出: 
        //     // 1、第二层、三层中都存在的层的ID
        //     // 2、TODO: 两边值相似的ID
        //     RenderTexture alphaMask = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
        //     BlendCS3LUtils.DispacthFindTransformMask(m_TerrainAsset, rawIDEdgeTextureS_Similar, rawIDEdgeTextureT_Similar, rawIDTexture, ref alphaMask);
        //     Utils.SaveRT2Texture(alphaMask, TextureFormat.RGBA32, GetTextureSavePath("3Layers/3AlphaMask.tga", m_TerrainAsset.m_TerrainName));
        //     RenderTexture alphaMask_Fix = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
        //     BlendCS3LUtils.DispacthFixTransformMask(m_TerrainAsset, rawIDTexture, alphaMask, ref alphaMask_Fix);
        //     Utils.SaveRT2Texture(alphaMask_Fix, TextureFormat.RGBA32, GetTextureSavePath("3Layers/3AlphaMask_Fix.tga", m_TerrainAsset.m_TerrainName));
            
        //     // 根据上面的Mask, 对RawIDTexture中各自区域内的ID进行处理
        //     RenderTexture rawIDEdgeTextureS_01 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
        //     RenderTexture rawIDEdgeTextureT_01 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
        //     RenderTexture rawIDTexture_01 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
        //     RenderTexture alphaMask_01 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
        //     BlendCS3LUtils.DispacthTransformDimension(m_TerrainAsset, rawIDEdgeTextureS_Similar, rawIDEdgeTextureT_Similar, alphaMask_Fix, rawIDTexture, 
        //         ref rawIDEdgeTextureS_01, ref rawIDEdgeTextureT_01, ref rawIDTexture_01, ref alphaMask_01);
        //     Utils.SaveRT2Texture(rawIDEdgeTextureS_01, TextureFormat.RGBA32, GetTextureSavePath("3Layers/4RawIDEdgeTextureS.tga", m_TerrainAsset.m_TerrainName));
        //     Utils.SaveRT2Texture(rawIDEdgeTextureT_01, TextureFormat.RGBA32, GetTextureSavePath("3Layers/4RawIDEdgeTextureT.tga", m_TerrainAsset.m_TerrainName));
        //     Utils.SaveRT2Texture(rawIDTexture_01, TextureFormat.RGBA32, GetTextureSavePath("3Layers/4RawIDTexture.tga", m_TerrainAsset.m_TerrainName));
        //     Utils.SaveRT2Texture(alphaMask_01, TextureFormat.RGBA32, GetTextureSavePath("3Layers/4AlphaMask.tga", m_TerrainAsset.m_TerrainName));

        //     // 扩展
        //     RenderTexture layerExtendS = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
        //     BlendCS2LUtils.DispatchIDLayerExtend(m_TerrainAsset, new Vector4(1, 0, 0, 4), rawIDEdgeTextureS_01, ref layerExtendS);
        //     Utils.SaveRT2Texture(layerExtendS, TextureFormat.RGBA32, GetTextureSavePath("3Layers/5ExtendSecondLayer.tga", m_TerrainAsset.m_TerrainName));
        //     RenderTexture layerExtendT = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
        //     BlendCS2LUtils.DispatchIDLayerExtend(m_TerrainAsset, new Vector4(1, 0, 0, 4), rawIDEdgeTextureT_01, ref layerExtendT);
        //     Utils.SaveRT2Texture(layerExtendT, TextureFormat.RGBA32, GetTextureSavePath("3Layers/5ExtendThirdLayer.tga", m_TerrainAsset.m_TerrainName));

        //     // 获得相交处的Mask
        //     RenderTexture alphaMask_Extend = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
        //     BlendCS3LUtils.DispacthFindExtendTransformMask(m_TerrainAsset, layerExtendS, layerExtendT, alphaMask_01, ref alphaMask_Extend);
        //     Utils.SaveRT2Texture(alphaMask_Extend, TextureFormat.RGBA32, GetTextureSavePath("3Layers/6AlphaMask.tga", m_TerrainAsset.m_TerrainName));

        //     // 使用相交处的Mask在对RawID和Extend做一次处理
        //     RenderTexture layerExtendS_01 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
        //     RenderTexture layerExtendT_01 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
        //     RenderTexture rawIDTexture_02 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
        //     RenderTexture alphaMask_Extend_01 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
        //     BlendCS3LUtils.DispacthTransformExtendDimension(m_TerrainAsset, layerExtendS, layerExtendT, alphaMask_Extend, rawIDTexture_01, 
        //         ref layerExtendS_01, ref layerExtendT_01, ref rawIDTexture_02, ref alphaMask_Extend_01);
        //     Utils.SaveRT2Texture(layerExtendS_01, TextureFormat.RGBA32, GetTextureSavePath("3Layers/7ExtendSecondLayer.tga", m_TerrainAsset.m_TerrainName));
        //     Utils.SaveRT2Texture(layerExtendT_01, TextureFormat.RGBA32, GetTextureSavePath("3Layers/7ExtendThirdLayer.tga", m_TerrainAsset.m_TerrainName));
        //     Utils.SaveRT2Texture(rawIDTexture_02, TextureFormat.RGBA32, GetTextureSavePath("3Layers/7RawIDTexture.tga", m_TerrainAsset.m_TerrainName));
        //     Utils.SaveRT2Texture(alphaMask_Extend_01, TextureFormat.RGBA32, GetTextureSavePath("3Layers/7AlphaMask.tga", m_TerrainAsset.m_TerrainName));

        //     // 处理AlphaMask
        //     RenderTexture alphaMask_03 = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
        //     BlendCS3LUtils.DispacthCombineAlphaMask(m_TerrainAsset, alphaMask_01, alphaMask_Extend_01, ref alphaMask_03);
        //     RenderTexture alphaMaskExtend = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
        //     BlendCS2LUtils.DispatchIDLayerExtend(m_TerrainAsset, new Vector4(1, 0, 0, 4), alphaMask_03, ref alphaMaskExtend);
        //     Utils.SaveRT2Texture(alphaMaskExtend, TextureFormat.RGBA32, GetTextureSavePath("3Layers/8AlphaMaskExtend.tga", m_TerrainAsset.m_TerrainName));
        //     // 混合
        //     RenderTexture layerExtendDS = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
        //     BlendCS2LUtils.DispatchIDLayerExtend(m_TerrainAsset, new Vector4(0, 1, 0, 2), rawIDTexture, ref layerExtendDS);
        //     RenderTexture blendTexture = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);

        //     BlendCS3LUtils.DispacthThreeLayersBlend(m_TerrainAsset, layerExtendDS, layerExtendS_01, layerExtendT_01, alphaMaskExtend, ref blendTexture);
        //     Utils.SaveRT2Texture(blendTexture, TextureFormat.RGBA32, GetTextureSavePath("3Layers/9BlendTexture.tga", m_TerrainAsset.m_TerrainName));

        //     rawIDEdgeTextureS.Release();
        //     rawIDEdgeTextureT.Release();
        //     rawIDEdgeTextureS_Similar.Release();
        //     rawIDEdgeTextureT_Similar.Release();
        //     alphaMask.Release();

        //     rawIDEdgeTextureS_01.Release();
        //     rawIDEdgeTextureT_01.Release();
        //     rawIDTexture_01.Release();

        //     layerExtendS.Release();
        //     layerExtendT.Release();

        //     layerExtendS_01.Release();
        //     layerExtendT_01.Release();
        //     rawIDTexture_02.Release();
        //     alphaMask_Extend.Release();
        //     alphaMask_Extend_01.Release();

        //     alphaMask_03.Release();
        //     alphaMaskExtend.Release();
        //     layerExtendDS.Release();
        //     blendTexture.Release();
        //     indexRank.Release();
        //     AssetDatabase.Refresh();
        // }
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
