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
        public Texture2D m_RawIDMask;
        public Texture2D m_AlphaTexture;
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
        public int m_LayersCount;
        public int[] m_CoverageIndexRank;
        public int[] m_OccupancyIndexRank;
        
        public TerrainLayer[] m_TerrainLayers;
        // public Texture2DArray m_AlbedoArray;
        // public Texture2DArray m_NormalArray;
        public Texture2DArray m_RawIDMaskArray;
        public Texture2DArray m_AlphaTextureArray;
        private List<string> m_RawIDMaskPaths;
        private List<string> m_AlphaTexturePaths;
        
        public TerrainBlendAsset(TerrainData terrainData)
        {
            m_TerrainData = terrainData;
            InitTerrainBlendAsset();
        }

        public void InitTerrainBlendAsset()
        {
            m_TerrainName = m_TerrainData.name;
            m_AlphamapResolution = m_TerrainData.alphamapResolution;
            float[,,] alphamaps = m_TerrainData.GetAlphamaps(0, 0, m_AlphamapResolution, m_AlphamapResolution);
            m_LayersCount = alphamaps.GetLength(2);
            m_CoverageIndexRank = new int[m_LayersCount];
            m_OccupancyIndexRank = new int[m_LayersCount];

            m_TerrainLayers = new TerrainLayer[m_LayersCount];
            m_RawIDMaskPaths = new List<string>();
            m_AlphaTexturePaths = new List<string>();
            
            for (int i = 0; i < m_LayersCount; i++)
            {
                float alpha = 0;
                float coverage = 0;
                float occupancy = 0;
                Texture2D tempAlphaTexture = new Texture2D(m_AlphamapResolution, m_AlphamapResolution, TextureFormat.R16, 0, true);
                
                for (int x = 0; x < m_AlphamapResolution; x++)
                for (int y = 0; y < m_AlphamapResolution; y++)
                {
                    alpha = alphamaps[x, y, i];
                    occupancy += alpha;
                    if (alpha > 0) coverage ++;
                    
                    tempAlphaTexture.SetPixel(y, x, new Color(alpha, alpha, alpha, alpha));
                }
                coverage /= (m_AlphamapResolution * m_AlphamapResolution);
                occupancy /= (m_AlphamapResolution * m_AlphamapResolution);
                m_TerrainLayers[i] = new TerrainLayer(coverage, occupancy);
                tempAlphaTexture.Apply();

                // 输出RawIDMask和AlphaTexture
                OutputRawTexture(tempAlphaTexture, i);
            }
            AssetDatabase.Refresh();
            // 加载生成的贴图, 并修改其格式
            int loadTextureCount = Mathf.Min(m_RawIDMaskPaths.Count, m_AlphaTexturePaths.Count);
            for (int i = 0; i < loadTextureCount; i++)
            {
                Utils.SolveCompression(FilterMode.Point, TextureImporterFormat.RGBA32, m_RawIDMaskPaths[i], m_AlphamapResolution);
                Utils.SolveCompression(FilterMode.Point, TextureImporterFormat.R16, m_AlphaTexturePaths[i], m_AlphamapResolution);
                m_TerrainLayers[i].m_RawIDMask = AssetDatabase.LoadAssetAtPath<Texture2D>(m_RawIDMaskPaths[i]);
                m_TerrainLayers[i].m_AlphaTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(m_AlphaTexturePaths[i]);
            }
            // 生成RawIDMask和AlphaTexture的Array贴图
            m_RawIDMaskArray = new Texture2DArray(m_AlphamapResolution, m_AlphamapResolution, loadTextureCount, 
                TextureFormat.RGBA32, 0, true);
            m_AlphaTextureArray = new Texture2DArray(m_AlphamapResolution, m_AlphamapResolution, loadTextureCount, 
                TextureFormat.R16, 0, true);
            for (int i = 0; i < loadTextureCount; i++)
            {
                Graphics.CopyTexture(m_TerrainLayers[i].m_RawIDMask, 0, 0, m_RawIDMaskArray, i, 0);
                Graphics.CopyTexture(m_TerrainLayers[i].m_AlphaTexture, 0, 0, m_AlphaTextureArray, i, 0);
            }
            m_RawIDMaskArray.Apply(false, true);
            m_AlphaTextureArray.Apply(false, true);
            m_RawIDMaskArray.filterMode = FilterMode.Point;
            m_AlphaTextureArray.filterMode = FilterMode.Point;
            AssetDatabase.CreateAsset(m_RawIDMaskArray, GetAssetSavePath("RawIDMaskArray.asset", true));
            AssetDatabase.CreateAsset(m_AlphaTextureArray, GetAssetSavePath("AlphaTextureArray.asset", false));
            AssetDatabase.Refresh();
            // 更新覆盖率和占有率
            m_CoverageIndexRank = SortCoverageOrOccupancy(m_TerrainLayers, true);
            m_OccupancyIndexRank = SortCoverageOrOccupancy(m_TerrainLayers, false);
        }
        
        public string GetAssetSavePath(string texName, bool isRawIDMask)
        {
            string parentPath = isRawIDMask ? "RawIDMasks" : "AlphaTextures";
            return $"{Utils.s_RootPath}/TerrainAsset/{m_TerrainName}/{parentPath}/{texName}";
        }
        private void OutputRawTexture(Texture2D alphaTexture, int layerIndex)
        {
            // 根据AlphaTexture处理RawIDMask
            if (CShaderUtils.s_RawIDMaskKernel >= 0)
            {
                int threadGroups = Mathf.CeilToInt(m_AlphamapResolution / 8);
                RenderTexture tempRawIDMask = Utils.CreateRenderTexture(m_AlphamapResolution, RenderTextureFormat.ARGB32);
                CShaderUtils.s_Shader.SetInts(CShaderUtils.s_TerrainParamsID, layerIndex, m_AlphamapResolution);
                CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_RawIDMaskKernel, CShaderUtils.s_AlphaTextureID, alphaTexture);
                CShaderUtils.s_Shader.SetTexture(CShaderUtils.s_RawIDMaskKernel, CShaderUtils.s_IDResultID, tempRawIDMask);
                CShaderUtils.s_Shader.Dispatch(CShaderUtils.s_RawIDMaskKernel, threadGroups, threadGroups, 1);

                string rawIDMaskPath = GetAssetSavePath($"RawIDMask_{layerIndex}.tga", true);
                Utils.SaveRT2Texture(tempRawIDMask, TextureFormat.RGBA32, rawIDMaskPath);
                m_RawIDMaskPaths.Add(rawIDMaskPath);
                tempRawIDMask.Release();
            }
            else
            {
                Debug.LogError("未找到RawIDMask内核!");
                return;
            }
            string alphaTexturePath = GetAssetSavePath($"AlphaTexture_{layerIndex}.tga", false);
            m_AlphaTexturePaths.Add(alphaTexturePath);
            Utils.SaveTexture(alphaTexture, alphaTexturePath);
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