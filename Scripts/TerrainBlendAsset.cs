using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
namespace TerrainBlend16
{
    [System.Serializable]
    public class TerrainLayer
    {
        // 光照模型贴图
        // public Texture2D m_AlbedoTexture;
        // public Texture2D m_NormalMap;
        // public Texture2D m_MSOMap;

        // 用于混合的贴图
        public Texture2D m_OriginAlpha;
        public Texture2D m_AutoEraseAlpha;
        public Texture2D m_EraseAlpha;
        public Texture2D m_RawIDMask;
        public float m_Coverage;
        public float m_Occupancy;

        public TerrainLayer(float coverage, float occupancy)
        {
            m_Coverage = coverage;
            m_Occupancy = occupancy;
        }
    }
    /// <summary>
    /// 可以在Asset上更新, 同时也可以用TerrainBlend脚本自动生成
    /// </summary>
    [CreateAssetMenu(fileName = "TerrainBlendAsset", menuName = "Terrain Blend/Terrain Blend Asset", order = 0)]
    public class TerrainBlendAsset : ScriptableObject
    {
        public TerrainData m_TerrainData;
        public string m_TerrainName;
        public int m_AlphamapResolution;
        public int m_ThreadGroups;
        public int m_LayersCount;
        public int[] m_CoverageIndexRank;
        public int[] m_OccupancyIndexRank;
        
        public TerrainLayer[] m_TerrainLayers;
        // public Texture2DArray m_AlbedoArray;
        // public Texture2DArray m_NormalArray;
        public Texture2DArray m_RawIDMaskArray;
        public Texture2DArray m_OriginAlphaArray;
        public Texture2DArray m_AlphaTextureArray;
        private List<string> m_RawIDMaskPaths;
        private List<string> m_OriginAlphaPaths;
        private List<string> m_AlphaTexturePaths;
        
        public TerrainBlendAsset(TerrainData terrainData)
        {
            m_TerrainData = terrainData;
            InitTerrainBlendAsset();
        }
        /// <summary>
        /// 初始化地形文件
        /// </summary>
        public void InitTerrainBlendAsset()
        {
            m_TerrainName = m_TerrainData.name;
            m_AlphamapResolution = m_TerrainData.alphamapResolution;
            m_ThreadGroups = Mathf.CeilToInt(m_TerrainData.alphamapResolution / 8);
            float[,,] alphamaps = m_TerrainData.GetAlphamaps(0, 0, m_AlphamapResolution, m_AlphamapResolution);
            m_LayersCount = alphamaps.GetLength(2);
            m_CoverageIndexRank = new int[m_LayersCount];
            m_OccupancyIndexRank = new int[m_LayersCount];

            m_TerrainLayers = new TerrainLayer[m_LayersCount];
            m_OriginAlphaPaths = new List<string>();
            m_OriginAlphaArray = new Texture2DArray(m_AlphamapResolution, m_AlphamapResolution, m_LayersCount, TextureFormat.R8, 0, true);
            for (int i = 0; i < m_LayersCount; i++)
            {
                float alpha = 0;
                float coverage = 0;
                float occupancy = 0;
                Texture2D tempAlphaTexture = new Texture2D(m_AlphamapResolution, m_AlphamapResolution, TextureFormat.R8, 0, true);
                for (int x = 0; x < m_AlphamapResolution; x++)
                for (int y = 0; y < m_AlphamapResolution; y++)
                {
                    alpha = alphamaps[x, y, i];
                    occupancy += alpha;
                    if (alpha > 0) coverage ++;
                    
                    tempAlphaTexture.SetPixel(y, x, new Color(alpha, alpha, alpha, 1));
                }
                coverage /= (m_AlphamapResolution * m_AlphamapResolution);
                occupancy /= (m_AlphamapResolution * m_AlphamapResolution);
                m_TerrainLayers[i] = new TerrainLayer(coverage, occupancy);
                tempAlphaTexture.Apply();
                Graphics.CopyTexture(tempAlphaTexture, 0, 0, m_OriginAlphaArray, i, 0);
                string originSavePath = GetAssetSavePath($"OriginAlpha_{i}.tga", "OriginAlpha");
                m_OriginAlphaPaths.Add(originSavePath);
                Utils.SaveTexture(tempAlphaTexture, originSavePath);
            }
            m_OriginAlphaArray.Apply(false, true);
            m_OriginAlphaArray.filterMode = FilterMode.Point;
            AssetDatabase.CreateAsset(m_OriginAlphaArray, GetAssetSavePath("OriginAlphaArray.asset", "OriginAlpha"));
            AssetDatabase.Refresh();
            for (int i = 0; i < m_LayersCount; i++)
            {
                Utils.SolveCompression(FilterMode.Point, TextureImporterFormat.R8, m_OriginAlphaPaths[i], m_AlphamapResolution);
                m_TerrainLayers[i].m_OriginAlpha = AssetDatabase.LoadAssetAtPath<Texture2D>(m_OriginAlphaPaths[i]);
            }
            m_CoverageIndexRank = SortCoverageOrOccupancy(m_TerrainLayers, true);
            m_OccupancyIndexRank = SortCoverageOrOccupancy(m_TerrainLayers, false);
            AssetDatabase.Refresh();
        }
        /// <summary>
        /// 自动擦除
        /// </summary>
        public void AutoErase()
        {
            if (m_OriginAlphaArray == null) return;
            m_AlphaTexturePaths = new List<string>();
            m_RawIDMaskPaths = new List<string>();
            Texture2DArray rawIDMaskArray = new Texture2DArray(m_AlphamapResolution, m_AlphamapResolution, m_LayersCount, TextureFormat.ARGB32, 0, true);
            // 首先需要扩展RawIDMask; R: Mask; G: Alpha;
            EraseShaderUtils.Compute.SetInts(EraseShaderUtils.s_TerrainParamsID, m_LayersCount, m_AlphamapResolution, 0);
            for (int i = 0; i < m_LayersCount; i++)
            {
                RenderTexture rawIDMask = Utils.CreateRenderTexture(m_AlphamapResolution, RenderTextureFormat.ARGB32);
                EraseShaderUtils.DispacthRawIDMask(m_ThreadGroups, m_TerrainLayers[i].m_OriginAlpha, ref rawIDMask);
                // Utils.SaveRT2Texture(rawIDMask, TextureFormat.ARGB32, GetAssetSavePath($"RawIDMask_{i}.tga", "Erase/RawIDMask"));
                Texture2D raIDMask2D = Utils.GetTexture2D(rawIDMask, TextureFormat.ARGB32);
                Graphics.CopyTexture(raIDMask2D, 0, 0, rawIDMaskArray, i, 0);
                Texture2D.DestroyImmediate(raIDMask2D);
                raIDMask2D = null;
                rawIDMask.Release();
            }
            rawIDMaskArray.Apply();
            rawIDMaskArray.filterMode = FilterMode.Point;
            // AssetDatabase.CreateAsset(rawIDMaskArray, GetAssetSavePath("RawOriginIDMaskArray.asset", "Erase"));
            // 输出需要擦除的Mask
            RenderTexture eraseMask = Utils.CreateRenderTexture(m_AlphamapResolution, RenderTextureFormat.ARGB32);
            EraseShaderUtils.DispacthEraseMask(m_ThreadGroups, rawIDMaskArray, ref eraseMask);
            Utils.SaveRT2Texture(eraseMask, TextureFormat.ARGB32, GetAssetSavePath($"EraseMask.tga", "Erase"));
            // 简单擦除
            RenderTexture rawIDMaskEraseArray = Utils.CreateRenderTexture3D(m_AlphamapResolution, m_LayersCount, RenderTextureFormat.ARGB32);
            EraseShaderUtils.DispacthEraseLayer(m_ThreadGroups, eraseMask, rawIDMaskArray, ref rawIDMaskEraseArray);
            // 清理擦除区域周围一圈像素
            RenderTexture rawIDMaskAroundArray = Utils.CreateRenderTexture3D(m_AlphamapResolution, m_LayersCount, RenderTextureFormat.ARGB32);
            RenderTexture alphaTextureArray = Utils.CreateRenderTexture3D(m_AlphamapResolution, m_LayersCount, RenderTextureFormat.R8);
            RenderTexture alphaNormalize = Utils.CreateRenderTexture(m_AlphamapResolution, RenderTextureFormat.ARGB32);
            EraseShaderUtils.DispacthBlurredEraseEdge(m_ThreadGroups, rawIDMaskEraseArray, ref rawIDMaskAroundArray, ref alphaTextureArray, ref alphaNormalize);
            Utils.SaveRT2Texture(alphaNormalize, TextureFormat.ARGB32, GetAssetSavePath($"AlphaNormalize.tga", "Erase"));
            for (int i = 0; i < m_LayersCount; i++)
            {
                RenderTexture rawIDMaskErase = Utils.CreateRenderTexture(m_AlphamapResolution, RenderTextureFormat.ARGB32);
                Graphics.CopyTexture(rawIDMaskAroundArray, i, 0, rawIDMaskErase, 0, 0);
                string idMaskSavePath = GetAssetSavePath($"RawIDMask_{i}.tga", "Erase/IDMask");
                Utils.SaveRT2Texture(rawIDMaskErase, TextureFormat.ARGB32, idMaskSavePath);
                m_RawIDMaskPaths.Add(idMaskSavePath);
                RenderTexture alphaTexture = Utils.CreateRenderTexture(m_AlphamapResolution, RenderTextureFormat.R8);
                Graphics.CopyTexture(alphaTextureArray, i, 0, alphaTexture, 0, 0);
                string alphaSavePath = GetAssetSavePath($"AutoEraseAlpha_{i}.tga", "Erase/Alpha");
                Utils.SaveRT2Texture(alphaTexture, TextureFormat.R8, alphaSavePath);
                m_AlphaTexturePaths.Add(alphaSavePath);
                rawIDMaskErase.Release();
                alphaTexture.Release();
            }
            // 输出
            AssetDatabase.Refresh();
            for (int i = 0; i < m_LayersCount; i++)
            {
                Utils.SolveCompression(FilterMode.Point, TextureImporterFormat.R8, m_AlphaTexturePaths[i], m_AlphamapResolution);
                Utils.SolveCompression(FilterMode.Point, TextureImporterFormat.RGBA32, m_RawIDMaskPaths[i], m_AlphamapResolution);
                m_TerrainLayers[i].m_AutoEraseAlpha = AssetDatabase.LoadAssetAtPath<Texture2D>(m_AlphaTexturePaths[i]);
                m_TerrainLayers[i].m_RawIDMask = AssetDatabase.LoadAssetAtPath<Texture2D>(m_RawIDMaskPaths[i]);
            }
            eraseMask.Release();
            rawIDMaskEraseArray.Release();
            rawIDMaskAroundArray.Release();
            alphaTextureArray.Release();
            alphaNormalize.Release();
            AssetDatabase.Refresh();
        }
        /// <summary>
        /// 自动擦除_V1
        /// </summary>
        public void AutoErase_V1()
        {
            if (m_OriginAlphaArray == null) return;
            m_AlphaTexturePaths = new List<string>();
            m_RawIDMaskPaths = new List<string>();
            int threadGroups = Mathf.CeilToInt(m_AlphamapResolution / 8);
            // 先向外扩展
            
            // 获得擦除的Mask
            EraseShaderUtils.Compute.SetInts(EraseShaderUtils.s_TerrainParamsID, m_LayersCount, m_AlphamapResolution, 0);
            EraseShaderUtils.Compute.SetTexture(EraseShaderUtils.EraseMask, EraseShaderUtils.s_AlphaTextureArrayID, m_OriginAlphaArray);
            RenderTexture m_EraseMaskRT = Utils.CreateRenderTexture(m_AlphamapResolution, RenderTextureFormat.ARGB32);
            EraseShaderUtils.Compute.SetTexture(EraseShaderUtils.EraseMask, EraseShaderUtils.s_EraseMaskResultID, m_EraseMaskRT);
            EraseShaderUtils.Compute.Dispatch(EraseShaderUtils.EraseMask, threadGroups, threadGroups, 1);
            // 简单擦除
            EraseShaderUtils.Compute.SetTexture(EraseShaderUtils.EraseLayer, EraseShaderUtils.s_EraseMaskID, m_EraseMaskRT);
            EraseShaderUtils.Compute.SetTexture(EraseShaderUtils.EraseLayer, EraseShaderUtils.s_AlphaTextureArrayID, m_OriginAlphaArray);
            RenderTexture m_EraseAlphaResultRT = Utils.CreateRenderTexture3D(m_AlphamapResolution, m_LayersCount, RenderTextureFormat.R8);
            EraseShaderUtils.Compute.SetTexture(EraseShaderUtils.EraseLayer, EraseShaderUtils.s_AlphaTextureArrayResultID, m_EraseAlphaResultRT);
            EraseShaderUtils.Compute.Dispatch(EraseShaderUtils.EraseLayer, threadGroups, threadGroups, 1);
            // 孤立点处理
            List<RenderTexture> m_SmoothEraseRTList = new List<RenderTexture>();
            RenderTexture m_IsolatedMaskRT = Utils.CreateRenderTexture(m_AlphamapResolution, RenderTextureFormat.ARGB32);
            Graphics.CopyTexture(m_EraseMaskRT, m_IsolatedMaskRT);
            // TODO: 如果平滑的硬边对整体影响大的话, 可以考虑循环执行; //目前只执行一次
            for (int i = 0; i < m_LayersCount; i++)
            {
                EraseShaderUtils.Compute.SetInts(EraseShaderUtils.s_TerrainParamsID, m_LayersCount, m_AlphamapResolution, i);
                // 吸附孤立点
                EraseShaderUtils.Compute.SetTexture(EraseShaderUtils.AppendIsolatedPoint, EraseShaderUtils.s_EraseMaskID, m_IsolatedMaskRT);
                EraseShaderUtils.Compute.SetTexture(EraseShaderUtils.AppendIsolatedPoint, EraseShaderUtils.s_AlphaTextureID, m_TerrainLayers[i].m_OriginAlpha);
                EraseShaderUtils.Compute.SetTexture(EraseShaderUtils.AppendIsolatedPoint, EraseShaderUtils.s_AlphaTextureArrayID, m_EraseAlphaResultRT);
                RenderTexture m_AppendEraseRT = Utils.CreateRenderTexture(m_AlphamapResolution, RenderTextureFormat.R8);
                EraseShaderUtils.Compute.SetTexture(EraseShaderUtils.AppendIsolatedPoint, EraseShaderUtils.s_EraseMaskResultID, m_EraseMaskRT);
                EraseShaderUtils.Compute.SetTexture(EraseShaderUtils.AppendIsolatedPoint, EraseShaderUtils.s_AlphaTextureResultID, m_AppendEraseRT);
                EraseShaderUtils.Compute.Dispatch(EraseShaderUtils.AppendIsolatedPoint, threadGroups, threadGroups, 1);
                // 寻找孤立点, 并将其移除
                EraseShaderUtils.Compute.SetTexture(EraseShaderUtils.FindIsolatedPoint, EraseShaderUtils.s_EraseMaskID, m_EraseMaskRT);
                EraseShaderUtils.Compute.SetTexture(EraseShaderUtils.FindIsolatedPoint, EraseShaderUtils.s_AlphaTextureID, m_AppendEraseRT);
                RenderTexture m_SmoothEraseRT = Utils.CreateRenderTexture(m_AlphamapResolution, RenderTextureFormat.R8);
                EraseShaderUtils.Compute.SetTexture(EraseShaderUtils.FindIsolatedPoint, EraseShaderUtils.s_EraseMaskResultID, m_IsolatedMaskRT);
                EraseShaderUtils.Compute.SetTexture(EraseShaderUtils.FindIsolatedPoint, EraseShaderUtils.s_AlphaTextureResultID, m_SmoothEraseRT);
                EraseShaderUtils.Compute.Dispatch(EraseShaderUtils.FindIsolatedPoint, threadGroups, threadGroups, 1);
                // 输出移除孤立点的贴图
                m_SmoothEraseRTList.Add(m_SmoothEraseRT);
                Utils.SaveRT2Texture(m_IsolatedMaskRT, TextureFormat.RGBA32, GetAssetSavePath($"Isolated_{i}.tga", "Erase/Alpha"));
                m_AppendEraseRT.Release();
            }
            List<RenderTexture> m_BlurredEraseRTList = new List<RenderTexture>();
            RenderTexture m_AlphaNormalizeInput = Utils.CreateRenderTexture(m_AlphamapResolution, RenderTextureFormat.R8);
            RenderTexture m_AlphaNormalizeResult = Utils.CreateRenderTexture(m_AlphamapResolution, RenderTextureFormat.R8);
            for (int i = 0; i < m_LayersCount; i++)
            {
                // 处理擦除产生的硬边
                EraseShaderUtils.Compute.SetTexture(EraseShaderUtils.BlurredEraseEdge, EraseShaderUtils.s_EraseMaskID, m_EraseMaskRT);
                EraseShaderUtils.Compute.SetTexture(EraseShaderUtils.BlurredEraseEdge, EraseShaderUtils.s_AlphaNormalizeID, m_AlphaNormalizeInput);
                EraseShaderUtils.Compute.SetTexture(EraseShaderUtils.BlurredEraseEdge, EraseShaderUtils.s_AlphaTextureID, m_SmoothEraseRTList[i]);
                RenderTexture m_BlurredEraseRT = Utils.CreateRenderTexture(m_AlphamapResolution, RenderTextureFormat.R8);
                EraseShaderUtils.Compute.SetTexture(EraseShaderUtils.BlurredEraseEdge, EraseShaderUtils.s_AlphaTextureResultID, m_BlurredEraseRT);
                EraseShaderUtils.Compute.SetTexture(EraseShaderUtils.BlurredEraseEdge, EraseShaderUtils.s_AlphaNormalizeResultID, m_AlphaNormalizeResult);
                EraseShaderUtils.Compute.Dispatch(EraseShaderUtils.BlurredEraseEdge, threadGroups, threadGroups, 1);
                Graphics.CopyTexture(m_AlphaNormalizeResult, m_AlphaNormalizeInput);
                m_BlurredEraseRTList.Add(m_BlurredEraseRT);
            }
            Utils.SaveRT2Texture(m_AlphaNormalizeResult, TextureFormat.R8, GetAssetSavePath($"AlphaNormalize.tga", "Erase"));
            // 输出IDMask和归一化后的Alpha
            for (int i = 0; i < m_LayersCount; i++)
            {
                // 输出RawIDMask
                EraseShaderUtils.Compute.SetTexture(EraseShaderUtils.RawIDMask, EraseShaderUtils.s_EraseMaskID, m_EraseMaskRT);
                EraseShaderUtils.Compute.SetTexture(EraseShaderUtils.RawIDMask, EraseShaderUtils.s_AlphaNormalizeID, m_AlphaNormalizeResult);
                EraseShaderUtils.Compute.SetTexture(EraseShaderUtils.RawIDMask, EraseShaderUtils.s_AlphaTextureID, m_BlurredEraseRTList[i]);
                RenderTexture m_AlphaResultRT = Utils.CreateRenderTexture(m_AlphamapResolution, RenderTextureFormat.R8);
                RenderTexture m_IDMaskResultRT = Utils.CreateRenderTexture(m_AlphamapResolution, RenderTextureFormat.ARGB32);
                EraseShaderUtils.Compute.SetTexture(EraseShaderUtils.RawIDMask, EraseShaderUtils.s_AlphaTextureResultID, m_AlphaResultRT);
                EraseShaderUtils.Compute.SetTexture(EraseShaderUtils.RawIDMask, EraseShaderUtils.s_EraseMaskResultID, m_IDMaskResultRT);
                EraseShaderUtils.Compute.Dispatch(EraseShaderUtils.RawIDMask, threadGroups, threadGroups, 1);
                
                string alphaSavePath = GetAssetSavePath($"AutoEraseAlpha_{i}.tga", "Erase/Alpha");
                Utils.SaveRT2Texture(m_AlphaResultRT, TextureFormat.RGBA32, alphaSavePath);
                m_AlphaTexturePaths.Add(alphaSavePath);

                string idMaskSavePath = GetAssetSavePath($"RawIDMask_{i}.tga", "Erase/IDMask");
                Utils.SaveRT2Texture(m_IDMaskResultRT, TextureFormat.RGBA32, idMaskSavePath);
                m_RawIDMaskPaths.Add(idMaskSavePath);
                m_AlphaResultRT.Release();
                m_IDMaskResultRT.Release();
            }
            m_EraseMaskRT.Release();
            m_EraseAlphaResultRT.Release();
            m_IsolatedMaskRT.Release();
            m_AlphaNormalizeInput.Release();
            m_AlphaNormalizeResult.Release();
            AssetDatabase.Refresh();
            for (int i = 0; i < m_LayersCount; i++)
            {
                Utils.SolveCompression(FilterMode.Point, TextureImporterFormat.R8, m_AlphaTexturePaths[i], m_AlphamapResolution);
                Utils.SolveCompression(FilterMode.Point, TextureImporterFormat.RGBA32, m_RawIDMaskPaths[i], m_AlphamapResolution);
                m_TerrainLayers[i].m_AutoEraseAlpha = AssetDatabase.LoadAssetAtPath<Texture2D>(m_AlphaTexturePaths[i]);
                m_TerrainLayers[i].m_RawIDMask = AssetDatabase.LoadAssetAtPath<Texture2D>(m_RawIDMaskPaths[i]);
            }
            AssetDatabase.Refresh();
        }

        public void CombineTextureArray()
        {
            m_RawIDMaskArray = new Texture2DArray(m_AlphamapResolution, m_AlphamapResolution, m_LayersCount, TextureFormat.RGBA32, 0, true);
            m_AlphaTextureArray = new Texture2DArray(m_AlphamapResolution, m_AlphamapResolution, m_LayersCount, TextureFormat.R8, 0, true);
            for (int i = 0; i < m_LayersCount; i++)
            {
                Graphics.CopyTexture(m_TerrainLayers[i].m_RawIDMask, 0, 0, m_RawIDMaskArray, i, 0);
                Graphics.CopyTexture(m_TerrainLayers[i].m_AutoEraseAlpha, 0, 0, m_AlphaTextureArray, i, 0);
            }
            m_RawIDMaskArray.Apply(false, true);
            m_AlphaTextureArray.Apply(false, true);
            m_RawIDMaskArray.filterMode = FilterMode.Point;
            m_AlphaTextureArray.filterMode = FilterMode.Point;
            AssetDatabase.CreateAsset(m_RawIDMaskArray, GetAssetSavePath("RawIDMaskArray.asset", "Erase"));
            AssetDatabase.CreateAsset(m_AlphaTextureArray, GetAssetSavePath("AlphaTextureArray.asset", "Erase"));
            AssetDatabase.Refresh();
        }
        public string GetAssetSavePath(string texName, string parentPath)
        {
            return $"{Utils.s_RootPath}/TerrainAsset/{m_TerrainName}/{parentPath}/{texName}";
        }
        /// <summary>
        /// 对覆盖率和占有率进行排序
        /// </summary>
        /// <returns>从大到小的索引排序</returns>
        private int[] SortCoverageOrOccupancy(TerrainLayer[] terrainLayers, bool isCoverage)
        {
            int[] tempIndexes = new int[terrainLayers.Length];
            float[] tempValues = new float[terrainLayers.Length];
            for (int i = 0; i < terrainLayers.Length; i++)
            {
                tempIndexes[i] = i;
                tempValues[i] = isCoverage ? terrainLayers[i].m_Coverage : terrainLayers[i].m_Occupancy;
            }
            int tempIndexe = 0;
            float tempValue = 0;
            for (int i = terrainLayers.Length - 1; i > 0; i--)
            for (int j = 0; j < i; j++)
            {
                if (tempValues[j] < tempValues[j + 1])
                {
                    tempIndexe = tempIndexes[j];
                    tempValue = tempValues[j];

                    tempIndexes[j] = tempIndexes[j + 1];
                    tempValues[j] = tempValues[j + 1];

                    tempIndexes[j + 1] = tempIndexe;
                    tempValues[j + 1] = tempValue;
                }
            }
            return tempIndexes;
        }
    }
}