using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using KSPTextureLoader;

namespace Parallax
{
    public static class BiomeLayerBuilder
    {
        const int ALB_SIZE = 2048; const TextureFormat ALB_FMT = TextureFormat.DXT5;
        const int BMP_SIZE = 2048; const TextureFormat BMP_FMT = TextureFormat.DXT5;
        const int SCALAR_SIZE = 1024;

        public static void BuildAndBind(CelestialBody cb, ParallaxTerrainBody body)
        {
            BiomeLayer[] byCh = new BiomeLayer[4];
            foreach (BiomeLayer l in body.biomeLayers) byCh[l.channel] = l;
            int count = Array.FindLastIndex(byCh, l => l != null) + 1;
            if (count <= 0) return;

            Dictionary<string, TextureHandle> H = Preload(byCh);

            Texture2DArray albedoArr = BuildArray(byCh, count, H, l => l == null ? null : l.albedoPath, ALB_SIZE, ALB_FMT, true, false);
            Texture2DArray bumpArr = BuildArray(byCh, count, H, l => l == null ? null : l.bumpPath, BMP_SIZE, BMP_FMT, true, true);
            Texture2D dispPacked = BuildPacked(byCh, H, l => l == null ? null : l.displacementPath, SCALAR_SIZE);
            Texture2D inflPacked = BuildPacked(byCh, H, l => l == null ? null : l.influencePath, SCALAR_SIZE);
            Texture2D aoPacked = BuildPacked(byCh, H, l => l == null ? null : l.occlusionPath, SCALAR_SIZE);

            body.biomeAlbedoArr = albedoArr;
            body.biomeBumpArr = bumpArr;
            body.biomeDispPacked = dispPacked;
            body.biomeInflPacked = inflPacked;
            body.biomeAoPacked = aoPacked;

            body.biomeTilingLive = Param(byCh, l => l.tiling, 0.03f);
            body.biomeDispScaleLive = Param(byCh, l => l.displacementScale, 0f);
            body.biomeInflStrengthLive = Param(byCh, l => l.influenceStrength, 0f);
            body.biomeBumpScaleLive = Param(byCh, l => l.bumpScale, 1f);
            body.biomeAoStrengthLive = Param(byCh, l => l.occlusionStrength, 0f);
            body.biomeEdgeNoiseLive = 0f;

            foreach (Material m in Variants(body.parallaxMaterials))
            {
                if (m == null) continue;
                m.SetTexture("_BiomeAlbedoArray", albedoArr);
                m.SetTexture("_BiomeBumpArray", bumpArr);
                m.SetTexture("_BiomeDisplacementPacked", dispPacked);
                m.SetTexture("_BiomeInfluencePacked", inflPacked);
                m.SetTexture("_BiomeOcclusionPacked", aoPacked);
                m.SetFloatArray("_BiomeTiling", body.biomeTilingLive);
                m.SetFloatArray("_BiomeDisplacementScale", body.biomeDispScaleLive);
                m.SetFloatArray("_BiomeInfluenceStrength", body.biomeInflStrengthLive);
                m.SetFloatArray("_BiomeBumpScale", body.biomeBumpScaleLive);
                m.SetFloatArray("_BiomeOcclusionStrength", body.biomeAoStrengthLive);
                m.SetFloat("_BiomeEdgeNoise", body.biomeEdgeNoiseLive);
                m.EnableKeyword("BIOME_LAYER");
                ParallaxDebug.Log("         [BiomeLayer] varient bound: ");
            }
            ParallaxDebug.Log("[BiomeLayer] " + body.planetName + ": " + count + " layer(s) bound");
        }
        static Dictionary<string, TextureHandle> Preload(BiomeLayer[] byCh)
        {
            var h = new Dictionary<string, TextureHandle>();
            void kick(string path, bool linear, bool unreadable)
            {
                if (string.IsNullOrEmpty(path) || h.ContainsKey(path)) return;
                if (!TextureLoader.TextureExists(path)) { ParallaxDebug.LogError("[BiomeLayer] missing texture " + path); return; }
                h[path] = TextureLoader.LoadTexture<Texture2D>(path, new TextureLoadOptions { Linear = linear, Unreadable = unreadable });
            }
            foreach (BiomeLayer l in byCh)
            {
                if (l == null) continue;
                kick(l.albedoPath, false, true);
                kick(l.bumpPath, true, true);
                kick(l.displacementPath, true, false);
                kick(l.influencePath, true, false);
                kick(l.occlusionPath, true, false);
            }
            return h;
        }

        static Texture2D Resolve(Dictionary<string, TextureHandle> H, string path)
        {
            if (string.IsNullOrEmpty(path) || !H.TryGetValue(path, out TextureHandle handle)) return null;
            try { return handle.GetTexture() as Texture2D; }
            catch (Exception e) { ParallaxDebug.LogError("[BiomeLayer] load failed " + path); Debug.LogException(e); return null; }
        }

        static Texture2D Normalize(Texture2D src, int size, bool mips, bool linear)
        {
            RenderTexture rt = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32,
    linear ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.sRGB);
            Graphics.Blit(src, rt);
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D outT = new Texture2D(size, size, TextureFormat.RGBA32, mips, linear);
            outT.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            outT.Apply(mips);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return outT;
        }

        static Texture2DArray BuildArray(BiomeLayer[] byCh, int count, Dictionary<string, TextureHandle> H,
            Func<BiomeLayer, string> path, int size, TextureFormat fmt, bool mips, bool linear)
        {
            Texture2DArray arr = new Texture2DArray(size, size, count, fmt, mips, linear);
            arr.wrapMode = TextureWrapMode.Repeat;
            arr.filterMode = FilterMode.Trilinear;
            for (int i = 0; i < count; i++)
            {
                Texture2D src = Resolve(H, path(byCh[i]));
                if (src == null) continue;
                if (src.width != size || src.height != size || src.format != fmt)
                {
                    Texture2D n = Normalize(src, size, mips, linear);
                    if (fmt != TextureFormat.RGBA32) n.Compress(true);
                    src = n;
                }
                Graphics.CopyTexture(src, 0, arr, i);
            }
            arr.Apply(false, true);
            return arr;
        }

        static Texture2D BuildPacked(BiomeLayer[] byCh, Dictionary<string, TextureHandle> H,
            Func<BiomeLayer, string> path, int size)
        {
            Texture2D packed = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            packed.wrapMode = TextureWrapMode.Repeat;
            Color[] dst = new Color[size * size];
            for (int ch = 0; ch < 4; ch++)
            {
                if (byCh[ch] == null) continue;
                Texture2D src = Resolve(H, path(byCh[ch]));
                if (src == null) continue;
                if (src.width != size || src.height != size) src = Normalize(src, size, false, true);
                Color[] s = src.GetPixels();
                for (int i = 0; i < dst.Length; i++)
                {
                    float v = s[i].r;
                    if (ch == 0) dst[i].r = v;
                    else if (ch == 1) dst[i].g = v;
                    else if (ch == 2) dst[i].b = v;
                    else dst[i].a = v;
                }
            }
            packed.SetPixels(dst);
            packed.Apply(true, true);
            return packed;
        }

        static IEnumerable<Material> Variants(ParallaxMaterials pm)
        {
            yield return pm.parallaxLow;
            yield return pm.parallaxMid;
            yield return pm.parallaxHigh;
            yield return pm.parallaxLowMid;
            yield return pm.parallaxMidHigh;
            yield return pm.parallaxFull;
        }

        static float[] Param(BiomeLayer[] byCh, Func<BiomeLayer, float> pick, float dflt)
        {
            float[] a = new float[4];
            for (int i = 0; i < 4; i++) a[i] = byCh[i] != null ? pick(byCh[i]) : dflt;
            return a;
        }
    }
}
