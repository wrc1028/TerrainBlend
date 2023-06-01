﻿using System.Text.RegularExpressions;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;

namespace TerrainBlend16
{
    public class BlendShaderUtils
    {
        public static ComputeShader s_TerrainCS;
        public static ComputeShader s_Shader
        {
            get 
            {
                if (s_TerrainCS == null)
                    s_TerrainCS = AssetDatabase.LoadAssetAtPath<ComputeShader>(
                        $"{Utils.s_RootPath}/Shaders/TerrainBlend.compute");
                return s_TerrainCS;
            }
        }
        // params
        public static int s_IndexRankID = Shader.PropertyToID("_IndexRank");
        public static int s_TerrainParamsID = Shader.PropertyToID("_TerrainParams");
        public static int s_ExtendParamsID = Shader.PropertyToID("_ExtendParams");

        public static int s_AlphaTextureID = Shader.PropertyToID("_AlphaTexture");
        public static int s_RawIDTextureID = Shader.PropertyToID("_RawIDTexture");
        public static int s_TempBlendTextureID = Shader.PropertyToID("_TempBlendTexture");
        public static int s_SecondLayerExtendID = Shader.PropertyToID("_SecondLayerExtend");
        public static int s_ThirdLayerExtendID = Shader.PropertyToID("_ThirdLayerExtend");
        public static int s_RawIDMaskArrayID = Shader.PropertyToID("_RawIDMaskArray");
        public static int s_AlphaTextureArrayID = Shader.PropertyToID("_AlphaTextureArray");
        public static int s_IDResultID = Shader.PropertyToID("IDResult");
        public static int s_BlendResultID = Shader.PropertyToID("BlendResult");
        // kernels

        public static int s_RawIDTextureKernel
        {
            get { return GetKernel("RawIDTexture"); }
        }
        public static int s_CheckIDLayerEdgeKernel
        {
            get { return GetKernel("CheckIDLayerEdge"); }
        }
        public static int s_IDLayerExtendKernel
        {
            get { return GetKernel("IDLayerExtend"); }
        }
        public static int s_DoubleLayersBlendKernel
        {
            get { return GetKernel("DoubleLayersBlend"); }
        }
        public static int s_ThreeLayersBlendKernel
        {
            get { return GetKernel("ThreeLayersBlend"); }
        }
        private static int GetKernel(string kernelName)
        {
            if (s_Shader == null) return -1;
            else return s_Shader.FindKernel(kernelName);
        }
    }
    public class EraseShaderUtils
    {
        private static ComputeShader s_TerrainCS;
        public static ComputeShader s_Shader
        {
            get 
            {
                if (s_TerrainCS == null)
                    s_TerrainCS = AssetDatabase.LoadAssetAtPath<ComputeShader>(
                        $"{Utils.s_RootPath}/Shaders/TerrainErase.compute");
                return s_TerrainCS;
            }
        }
        // params
        public static int s_TerrainParamsID = Shader.PropertyToID("_TerrainParams");
        public static int s_EraseMaskID = Shader.PropertyToID("_EraseMask");
        public static int s_AlphaTextureID = Shader.PropertyToID("_AlphaTexture");
        public static int s_AlphaTextureArrayID = Shader.PropertyToID("_AlphaTextureArray");
        public static int s_EraseMaskResultID = Shader.PropertyToID("EraseMaskResult");
        public static int s_AlphaTextureResultID = Shader.PropertyToID("AlphaTextureResult");
        public static int s_AlphaTextureArrayResultID = Shader.PropertyToID("AlphaTextureArrayResult");
        // kernel
        public static int s_EraseMaskKernel
        {
            get { return GetKernel("EraseMask"); }
        }
        public static int s_EraseLayerKernel
        {
            get { return GetKernel("EraseLayer"); }
        }
        public static int s_FindIsolatedPointKernel
        {
            get { return GetKernel("FindIsolatedPoint"); }
        }
        public static int s_AppendIsolatedPointKernel
        {
            get { return GetKernel("AppendIsolatedPoint"); }
        }
        public static int s_BlurredEraseEdgeKernel
        {
            get { return GetKernel("BlurredEraseEdge"); }
        }
        public static int s_RawIDMaskKernel
        {
            get { return GetKernel("RawIDMask"); }
        }
        public static int s_AlphaNormalizeKernel
        {
            get { return GetKernel("AlphaNormalize"); }
        }
        private static int GetKernel(string kernelName)
        {
            if (s_Shader == null) return -1;
            else return s_Shader.FindKernel(kernelName);
        }
    }
    public class Utils
    {
        public static string s_RootPath = "Assets/TerrainBlend";
        public static RenderTexture CreateRenderTexture(int size, RenderTextureFormat format)
        {
            RenderTexture tempRT = new RenderTexture(size, size, 0, format, RenderTextureReadWrite.Linear);
            tempRT.filterMode = FilterMode.Point;
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
            tempRT.enableRandomWrite = true;
            tempRT.Create();
            return tempRT;
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