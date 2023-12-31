﻿using System.Text.RegularExpressions;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;

namespace TerrainBlend16
{
    public class ShaderParams
    {
        // TODO: 设置成通用: 以后擦除、双层混合和三层混合都用这个
        public static int s_IndexRankID = Shader.PropertyToID("_IndexRank");
        public static int s_ExtendParamsID = Shader.PropertyToID("_ExtendParams");
        public static int s_TerrainParamsID = Shader.PropertyToID("_TerrainParams");
        public static int s_AlphaTextureArrayID = Shader.PropertyToID("_AlphaTextureArray");
        public static int s_RawIDMaskArrayID = Shader.PropertyToID("_RawIDMaskArray");
        public static int s_RawIDMaskArrayResultID = Shader.PropertyToID("RawIDMaskArrayResult");
        public static int s_UndisposedCountID = Shader.PropertyToID("UndisposedCount");

        public static int s_TexInput1ID = Shader.PropertyToID("_TextureInput01");
        public static int s_TexInput2ID = Shader.PropertyToID("_TextureInput02");
        public static int s_TexInput3ID = Shader.PropertyToID("_TextureInput03");
        public static int s_TexInput4ID = Shader.PropertyToID("_TextureInput04");
        
        public static int s_Result1ID = Shader.PropertyToID("Result01");
        public static int s_Result2ID = Shader.PropertyToID("Result02");
        public static int s_Result3ID = Shader.PropertyToID("Result03");
        public static int s_Result4ID = Shader.PropertyToID("Result04");
    }
    public class BlendCS2LUtils
    {
        public static ComputeShader s_Compute;
        public static ComputeShader Compute
        {
            get 
            {
                if (s_Compute == null)
                    s_Compute = AssetDatabase.LoadAssetAtPath<ComputeShader>(
                        $"{Utils.s_RootPath}/Shaders/Terrain2LayersBlend.compute");
                return s_Compute;
            }
        }
        // kernels  
        public static int RawIDTexture
        {
            get { return GetKernel("RawIDTexture"); }
        }
        public static int IDMaskErase
        {
            get { return GetKernel("IDMaskErase"); }
        }
        public static int IDMaskExtend
        {
            get { return GetKernel("IDMaskExtend"); }
        }
        public static int CheckIDLayerEdge
        {
            get { return GetKernel("CheckIDLayerEdge"); }
        }
        public static int TransformG2B
        {
            get { return GetKernel("TransformG2B"); }
        }
        public static int DoubleLayersBlend
        {
            get { return GetKernel("DoubleLayersBlend"); }
        }
        private static int GetKernel(string kernelName)
        {
            if (Compute == null) return -1;
            else return Compute.FindKernel(kernelName);
        }
        // Dispatch
        public static void DispatchRawIDTexture(TerrainBlendAsset asset, ComputeBuffer indexRankBuffer, ref RenderTexture rawIDTexture)
        {
            Compute.SetBuffer (RawIDTexture, ShaderParams.s_IndexRankID, indexRankBuffer);
            Compute.SetTexture(RawIDTexture, ShaderParams.s_RawIDMaskArrayID, asset.m_RawIDMaskArray);
            Compute.SetTexture(RawIDTexture, ShaderParams.s_AlphaTextureArrayID, asset.m_AlphaTextureArray);
            Compute.SetTexture(RawIDTexture, ShaderParams.s_Result1ID, rawIDTexture);
            Compute.Dispatch(RawIDTexture, asset.m_ThreadGroups, asset.m_ThreadGroups, 1);
        }

        public static void DispatchIDMaskErase(TerrainBlendAsset asset, Vector4 extendParams, ComputeBuffer indexRankBuffer, RenderTexture rawIDTexture, 
            ref RenderTexture rawIDMaskArray)
        {
            Compute.SetVector(ShaderParams.s_ExtendParamsID, extendParams);
            Compute.SetBuffer (IDMaskErase, ShaderParams.s_IndexRankID, indexRankBuffer);
            Compute.SetTexture(IDMaskErase, ShaderParams.s_TexInput1ID, rawIDTexture);
            Compute.SetTexture(IDMaskErase, ShaderParams.s_RawIDMaskArrayID, asset.m_RawIDMaskArray);
            Compute.SetTexture(IDMaskErase, ShaderParams.s_RawIDMaskArrayResultID, rawIDMaskArray);
            Compute.Dispatch(IDMaskErase, asset.m_ThreadGroups, asset.m_ThreadGroups, 1);
        }

        public static void DispatchIDMaskExtend(TerrainBlendAsset asset, RenderTexture rawIDMaskArray, ref RenderTexture rawIDMaskArrayResult)
        {
            Compute.SetTexture(IDMaskExtend, ShaderParams.s_RawIDMaskArrayID, rawIDMaskArray);
            Compute.SetTexture(IDMaskExtend, ShaderParams.s_RawIDMaskArrayResultID, rawIDMaskArrayResult);
            Compute.Dispatch(IDMaskExtend, asset.m_ThreadGroups, asset.m_ThreadGroups, 1);
        }

        public static void DispatchCheckIDLayerEdge(TerrainBlendAsset asset, RenderTexture rawIDMaskArray, ref RenderTexture rawIDLayerEdge)
        {
            Compute.SetTexture(CheckIDLayerEdge, ShaderParams.s_RawIDMaskArrayID, rawIDMaskArray);
            Compute.SetTexture(CheckIDLayerEdge, ShaderParams.s_Result1ID, rawIDLayerEdge);
            Compute.Dispatch(CheckIDLayerEdge, asset.m_ThreadGroups, asset.m_ThreadGroups, 1);
        }
        public static void DispatchTransformG2B(TerrainBlendAsset asset, RenderTexture rawIDTexture, RenderTexture rawIDLayerEdge, ref RenderTexture rawIDResult)
        {
            Compute.SetTexture(TransformG2B, ShaderParams.s_TexInput1ID, rawIDTexture);
            Compute.SetTexture(TransformG2B, ShaderParams.s_TexInput2ID, rawIDLayerEdge);
            Compute.SetTexture(TransformG2B, ShaderParams.s_Result1ID, rawIDResult);
            Compute.Dispatch(TransformG2B, asset.m_ThreadGroups, asset.m_ThreadGroups, 1);
        }
        public static void DispatchDoubleLayersBlend(TerrainBlendAsset asset, RenderTexture rawIDTexture, RenderTexture rawIDLayerEdge, ref RenderTexture blendMap)
        {
            Compute.SetTexture(DoubleLayersBlend, ShaderParams.s_TexInput1ID, rawIDTexture);
            Compute.SetTexture(DoubleLayersBlend, ShaderParams.s_TexInput2ID, rawIDLayerEdge);
            Compute.SetTexture(DoubleLayersBlend, ShaderParams.s_AlphaTextureArrayID, asset.m_AlphaTextureArray);
            Compute.SetTexture(DoubleLayersBlend, ShaderParams.s_Result1ID, blendMap);
            Compute.Dispatch(DoubleLayersBlend, asset.m_ThreadGroups, asset.m_ThreadGroups, 1);
        }
    }
    public class BlendCS3LUtils
    {
        private static ComputeShader s_Compute;
        public static ComputeShader Compute
        {
            get 
            {
                if (s_Compute == null)
                {
                    s_Compute = AssetDatabase.LoadAssetAtPath<ComputeShader>(
                        $"{Utils.s_RootPath}/Shaders/Terrain3LayersBlend.compute");
                    if (s_Compute == null)
                        Debug.LogError("Not found Compute Shader!");
                }
                return s_Compute;
            }
        }
        // kernels
        public static int CheckSameLayerID
        {
            get { return GetKernel("CheckSameLayerID"); }
        }
        public static int CheckNearSameLayerID
        {
            get { return GetKernel("CheckNearSameLayerID"); }
        }
        public static int CheckIsolateLayerID
        {
            get { return GetKernel("CheckIsolateLayerID"); }
        }
        public static int CombineLayerID
        {
            get { return GetKernel("CombineLayerID"); }
        }
        public static int TransformGB2A
        {
            get { return GetKernel("TransformGB2A"); }
        }
        public static int CombineAlphaMask
        {
            get { return GetKernel("CombineAlphaMask"); }
        }
        public static int ThreeLayersBlend
        {
            get { return GetKernel("ThreeLayersBlend"); }
        }
        private static int GetKernel(string kernelName)
        {
            if (Compute == null)
            {
                Debug.LogError("Not found Compute Shader!");
                return -1;
            }
            else return Compute.FindKernel(kernelName);
        }
        // Dispacth
        private static int ThreadGroups(TerrainBlendAsset asset)
        {
            return Mathf.CeilToInt(asset.m_AlphamapResolution / 8);
        }
        public static void DispacthCheckSameLayerID(TerrainBlendAsset asset, 
            RenderTexture layerEdge, RenderTexture refLayerEdge, ref RenderTexture sameLayerID)
        {
            // Compute.SetVector(ShaderParams.s_ExtendParamsID, extendParams);
            Compute.SetTexture(CheckSameLayerID, ShaderParams.s_TexInput1ID, layerEdge);
            Compute.SetTexture(CheckSameLayerID, ShaderParams.s_TexInput2ID, refLayerEdge);
            Compute.SetTexture(CheckSameLayerID, ShaderParams.s_Result1ID, sameLayerID);
            Compute.Dispatch(CheckSameLayerID, asset.m_ThreadGroups, asset.m_ThreadGroups, 1);
        }
        public static void DispacthCheckNearSameLayerID(TerrainBlendAsset asset, ComputeBuffer undisposedCount, 
            RenderTexture sameLayerID, ref RenderTexture sameLayerIDResult)
        {
            Compute.SetBuffer(CheckNearSameLayerID, ShaderParams.s_UndisposedCountID, undisposedCount);
            Compute.SetTexture(CheckNearSameLayerID, ShaderParams.s_TexInput1ID, sameLayerID);
            Compute.SetTexture(CheckNearSameLayerID, ShaderParams.s_Result1ID, sameLayerIDResult);
            Compute.Dispatch(CheckNearSameLayerID, asset.m_ThreadGroups, asset.m_ThreadGroups, 1);
        }
        public static void DispacthCheckIsolateLayerID(TerrainBlendAsset asset, RenderTexture sameLayerID, ref RenderTexture sameLayerIDResult)
        {
            Compute.SetTexture(CheckIsolateLayerID, ShaderParams.s_TexInput1ID, sameLayerID);
            Compute.SetTexture(CheckIsolateLayerID, ShaderParams.s_Result1ID, sameLayerIDResult);
            Compute.Dispatch(CheckIsolateLayerID, asset.m_ThreadGroups, asset.m_ThreadGroups, 1);
        }
        public static void DispacthCombineLayerID(TerrainBlendAsset asset, RenderTexture sameLayerIDG, RenderTexture sameLayerIDB, RenderTexture layerEdgeG, RenderTexture layerEdgeB, 
            ref RenderTexture sameLayerIDResult, ref RenderTexture layerEdgeGResult, ref RenderTexture layerEdgeBResult)
        {
            Compute.SetTexture(CombineLayerID, ShaderParams.s_TexInput1ID, sameLayerIDG);
            Compute.SetTexture(CombineLayerID, ShaderParams.s_TexInput2ID, sameLayerIDB);
            Compute.SetTexture(CombineLayerID, ShaderParams.s_TexInput3ID, layerEdgeG);
            Compute.SetTexture(CombineLayerID, ShaderParams.s_TexInput4ID, layerEdgeB);
            Compute.SetTexture(CombineLayerID, ShaderParams.s_Result1ID, sameLayerIDResult);
            Compute.SetTexture(CombineLayerID, ShaderParams.s_Result2ID, layerEdgeGResult);
            Compute.SetTexture(CombineLayerID, ShaderParams.s_Result3ID, layerEdgeBResult);
            Compute.Dispatch(CombineLayerID, asset.m_ThreadGroups, asset.m_ThreadGroups, 1);
        }
        public static void DispacthTransformGB2A(TerrainBlendAsset asset, RenderTexture rawIDTexture, RenderTexture sameLayerID, 
            ref RenderTexture rawIDResult, ref RenderTexture alphaIDResult)
        {
            Compute.SetTexture(TransformGB2A, ShaderParams.s_TexInput1ID, rawIDTexture);
            Compute.SetTexture(TransformGB2A, ShaderParams.s_TexInput2ID, sameLayerID);
            Compute.SetTexture(TransformGB2A, ShaderParams.s_Result1ID, rawIDResult);
            Compute.SetTexture(TransformGB2A, ShaderParams.s_Result2ID, alphaIDResult);
            Compute.Dispatch(TransformGB2A, asset.m_ThreadGroups, asset.m_ThreadGroups, 1);
        }
        
        public static void DispacthCombineAlphaMask(TerrainBlendAsset asset, RenderTexture alphaMaskEdge, RenderTexture alphaMaskExtend, ref RenderTexture alphaMaskResult)
        {
            Compute.SetTexture(CombineAlphaMask, ShaderParams.s_TexInput1ID, alphaMaskEdge);
            Compute.SetTexture(CombineAlphaMask, ShaderParams.s_TexInput2ID, alphaMaskExtend);
            Compute.SetTexture(CombineAlphaMask, ShaderParams.s_Result1ID, alphaMaskResult);
            Compute.Dispatch(CombineAlphaMask, asset.m_ThreadGroups, asset.m_ThreadGroups, 1);
        }
        public static void DispacthThreeLayersBlend(TerrainBlendAsset asset, RenderTexture rChannelBlend, RenderTexture layerEdgeG, 
            RenderTexture layerEdgeB, RenderTexture alphaID, ref RenderTexture blendTexture)
        {
            Compute.SetTexture(ThreeLayersBlend, ShaderParams.s_TexInput1ID, rChannelBlend);
            Compute.SetTexture(ThreeLayersBlend, ShaderParams.s_TexInput2ID, layerEdgeG);
            Compute.SetTexture(ThreeLayersBlend, ShaderParams.s_TexInput3ID, layerEdgeB);
            Compute.SetTexture(ThreeLayersBlend, ShaderParams.s_TexInput4ID, alphaID);
            Compute.SetTexture(ThreeLayersBlend, ShaderParams.s_AlphaTextureArrayID, asset.m_AlphaTextureArray);
            Compute.SetTexture(ThreeLayersBlend, ShaderParams.s_Result1ID, blendTexture);
            Compute.Dispatch(ThreeLayersBlend, asset.m_ThreadGroups, asset.m_ThreadGroups, 1);
        }
    }
    public class EraseShaderUtils
    {
        private static ComputeShader s_Compute;
        public static ComputeShader Compute
        {
            get 
            {
                if (s_Compute == null)
                    s_Compute = AssetDatabase.LoadAssetAtPath<ComputeShader>(
                        $"{Utils.s_RootPath}/Shaders/TerrainErase.compute");
                return s_Compute;
            }
        }
        // params
        public static int s_TerrainParamsID = Shader.PropertyToID("_TerrainParams");
        public static int s_EraseMaskID = Shader.PropertyToID("_EraseMask");
        public static int s_AlphaTextureID = Shader.PropertyToID("_AlphaTexture");
        public static int s_AlphaNormalizeID = Shader.PropertyToID("_AlphaNormalize");
        public static int s_AlphaTextureArrayID = Shader.PropertyToID("_AlphaTextureArray");
        public static int s_RawIDMaskArrayID = Shader.PropertyToID("_RawIDMaskArray");


        public static int s_EraseMaskResultID = Shader.PropertyToID("EraseMaskResult");
        public static int s_AlphaTextureResultID = Shader.PropertyToID("AlphaTextureResult");
        public static int s_AlphaNormalizeResultID = Shader.PropertyToID("AlphaNormalizeResult");
        public static int s_AlphaTextureArrayResultID = Shader.PropertyToID("AlphaTextureArrayResult");
        public static int s_RawIDMaskArrayResultID = Shader.PropertyToID("RawIDMaskArrayResult");
        // kernel
        public static int EraseMask
        {
            get { return GetKernel("EraseMask"); }
        }
        public static int EraseLayer
        {
            get { return GetKernel("EraseLayer"); }
        }
        public static int FindIsolatedPoint
        {
            get { return GetKernel("FindIsolatedPoint"); }
        }
        public static int AppendIsolatedPoint
        {
            get { return GetKernel("AppendIsolatedPoint"); }
        }
        public static int BlurredEraseEdge
        {
            get { return GetKernel("BlurredEraseEdge"); }
        }
        public static int RawIDMask
        {
            get { return GetKernel("RawIDMask"); }
        }
        public static void DispacthEraseMask(int threadGroups, Texture2DArray rawIDMaskArray, ref RenderTexture eraseMask)
        {
            Compute.SetTexture(EraseMask, s_RawIDMaskArrayID, rawIDMaskArray);
            Compute.SetTexture(EraseMask, s_EraseMaskResultID, eraseMask);
            Compute.Dispatch(EraseMask, threadGroups, threadGroups, 1);
        }
        public static void DispacthEraseLayer(int threadGroups, RenderTexture eraseMask, Texture2DArray rawIDMaskArray, ref RenderTexture rawIDMaskArrayResult)
        {
            Compute.SetTexture(EraseLayer, s_EraseMaskID, eraseMask);
            Compute.SetTexture(EraseLayer, s_RawIDMaskArrayID, rawIDMaskArray);
            Compute.SetTexture(EraseLayer, s_RawIDMaskArrayResultID, rawIDMaskArrayResult);
            Compute.Dispatch(EraseLayer, threadGroups, threadGroups, 1);
        }
        public static void DispacthBlurredEraseEdge(int threadGroups, RenderTexture rawIDMaskArray, ref RenderTexture rawIDMaskArrayResult, ref RenderTexture alphaTextureArray, ref RenderTexture alphaNormalize)
        {
            Compute.SetTexture(BlurredEraseEdge, s_RawIDMaskArrayID, rawIDMaskArray);
            Compute.SetTexture(BlurredEraseEdge, s_RawIDMaskArrayResultID, rawIDMaskArrayResult);
            Compute.SetTexture(BlurredEraseEdge, s_AlphaTextureArrayResultID, alphaTextureArray);
            Compute.SetTexture(BlurredEraseEdge, s_AlphaNormalizeResultID, alphaNormalize);
            Compute.Dispatch(BlurredEraseEdge, threadGroups, threadGroups, 1);
        }
        public static void DispacthRawIDMask(int threadGroups, Texture2D originAlpha, ref RenderTexture rawIDMask)
        {
            Compute.SetTexture(RawIDMask, s_AlphaTextureID, originAlpha);
            Compute.SetTexture(RawIDMask, s_EraseMaskResultID, rawIDMask);
            Compute.Dispatch(RawIDMask, threadGroups, threadGroups, 1);
        }
        private static int GetKernel(string kernelName)
        {
            if (Compute == null) return -1;
            else return Compute.FindKernel(kernelName);
        }
    }
    public class Utils
    {
        public static string s_RootPath = "Assets/TerrainBlend";
        public static RenderTexture CreateRenderTexture(int size, RenderTextureFormat format)
        {
            RenderTexture tempRT = new RenderTexture(size, size, 0, format, RenderTextureReadWrite.Linear);
            tempRT.filterMode = FilterMode.Point;
            tempRT.wrapMode = TextureWrapMode.Clamp;
            tempRT.enableRandomWrite = true;
            tempRT.Create();
            return tempRT;
        }
        public static RenderTexture CreateRenderTexture3D(int size, int slicesNum, RenderTextureFormat format)
        {
            RenderTexture tempRT = new RenderTexture(size, size, 0, format, RenderTextureReadWrite.Linear);
            tempRT.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
            tempRT.volumeDepth = slicesNum;
            tempRT.filterMode = FilterMode.Point;
            tempRT.wrapMode = TextureWrapMode.Clamp;
            tempRT.enableRandomWrite = true;
            tempRT.Create();
            return tempRT;
        }public static Texture2D GetTexture2D(RenderTexture rt, TextureFormat format)
        {
            Texture2D tex2D = new Texture2D(rt.width, rt.height, format, false, true);
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            tex2D.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex2D.Apply();
            RenderTexture.active = prev;
            return tex2D;
        }
        public static void SaveRT2Texture(RenderTexture rt, TextureFormat format, string savePath, bool isHDR = false)
        {
            Texture2D tex2D = new Texture2D(rt.width, rt.height, format, false, true);
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            tex2D.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex2D.Apply();
            RenderTexture.active = prev;

            SaveTexture(tex2D, savePath, isHDR);
        }
        public static void SaveTexture(Texture2D tex2D, string savePath, bool isHDR = false)
        {
            string directory = Path.GetDirectoryName(savePath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            
            Byte[] texBytes = isHDR ? tex2D.EncodeToEXR() : tex2D.EncodeToTGA();
            FileStream texFile = File.Open(savePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            BinaryWriter texWriter = new BinaryWriter(texFile);
            texWriter.Write(texBytes);
            texFile.Close();
            Texture2D.DestroyImmediate(tex2D);
            tex2D = null;
        }
        public static void SolveCompression(FilterMode filterMode, TextureImporterFormat format, string path, int maxTextureSize)
        {
            TextureImporter im = (TextureImporter)AssetImporter.GetAtPath(path);
            im.sRGBTexture = false;
            im.mipmapEnabled = false;
            im.filterMode = filterMode;
            im.textureCompression = TextureImporterCompression.Uncompressed;
            im.maxTextureSize = maxTextureSize;

            TextureImporterPlatformSettings androidSetting = im.GetPlatformTextureSettings("Android");
            androidSetting.overridden = true;
            androidSetting.format = format;
            im.SetPlatformTextureSettings(androidSetting);

            TextureImporterPlatformSettings iphoneSetting = im.GetPlatformTextureSettings("iPhone");
            iphoneSetting.overridden = true;
            iphoneSetting.format = format;
            im.SetPlatformTextureSettings(iphoneSetting);

            TextureImporterPlatformSettings standaloneSetting = im.GetPlatformTextureSettings("Standalone");
            standaloneSetting.overridden = true;
            standaloneSetting.format = format;
            im.SetPlatformTextureSettings(standaloneSetting);
            im.SaveAndReimport();
            AssetDatabase.Refresh();
        }
    }
}