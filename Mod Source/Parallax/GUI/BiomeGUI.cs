using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Parallax
{
    public partial class ParallaxGUI
    {
        static void BiomeMenu()
        {
            if (!ConfigLoader.parallaxTerrainBodies.ContainsKey(FlightGlobals.currentMainBody.name))
            {
                GUILayout.Label("Current planet is not configured for Parallax", HighLogic.Skin.label);
                return;
            }

            ParallaxTerrainBody body = ConfigLoader.parallaxTerrainBodies[FlightGlobals.currentMainBody.name];

            GUILayout.Label("Biome Layer Properties: ", HighLogic.Skin.label);
            GUILayout.Label("Live edits; not saved to config", HighLogic.Skin.label);
            GUILayout.Space(10);

            if (!body.HasBiomeLayers)
            {
                GUILayout.Label(FlightGlobals.currentMainBody.name + " has no biome layers configured", HighLogic.Skin.label);
                return;
            }

            if (body.biomeChannelByName == null) { GUILayout.Label("biomeChannelByName NULL"); return; }
            if (body.biomeTilingLive == null) { GUILayout.Label("biomeTilingLive NULL"); return; }
            if (body.biomeDispScaleLive == null) { GUILayout.Label("biomeDispScaleLive NULL"); return; }
            if (body.biomeInflStrengthLive == null) { GUILayout.Label("biomeInflStrengthLive NULL"); return; }
            if (body.biomeBumpScaleLive == null) { GUILayout.Label("biomeBumpScaleLive NULL"); return; }
            if (body.biomeAoStrengthLive == null) { GUILayout.Label("biomeAoStrengthLive NULL"); return; }

            // one channel per configured biome, in channel order
            foreach (var kvp in body.biomeChannelByName.OrderBy(k => k.Value))
            {
                int ch = kvp.Value;
                if (ch < 0 || ch > 3) continue;
                GUILayout.Space(8);
                GUILayout.Label(kvp.Key + "  (Channel " + ch + ")", HighLogic.Skin.label);

                float tiling = body.biomeTilingLive[ch];
                if (ParamCreator.CreateParam("Tiling:", ref tiling, GUIHelperFunctions.FloatField))
                { body.biomeTilingLive[ch] = tiling; body.SetBiomeMaterialValues(); }

                float disp = body.biomeDispScaleLive[ch];
                if (ParamCreator.CreateParam("Displacement Scale:", ref disp, GUIHelperFunctions.FloatField))
                { body.biomeDispScaleLive[ch] = disp; body.SetBiomeMaterialValues(); }

                float infl = body.biomeInflStrengthLive[ch];
                if (ParamCreator.CreateParam("Influence Strength:", ref infl, GUIHelperFunctions.FloatField))
                { body.biomeInflStrengthLive[ch] = infl; body.SetBiomeMaterialValues(); }

                float bump = body.biomeBumpScaleLive[ch];
                if (ParamCreator.CreateParam("Bump Scale:", ref bump, GUIHelperFunctions.FloatField))
                { body.biomeBumpScaleLive[ch] = bump; body.SetBiomeMaterialValues(); }

                float ao = body.biomeAoStrengthLive[ch];
                if (ParamCreator.CreateParam("Occlusion Strength:", ref ao, GUIHelperFunctions.FloatField))
                { body.biomeAoStrengthLive[ch] = ao; body.SetBiomeMaterialValues(); }
            }
            // global edge softening
            float edgeNoise = body.biomeEdgeNoiseLive;
            if (ParamCreator.CreateParam("Edge Noise:", ref edgeNoise, GUIHelperFunctions.FloatField))
            { body.biomeEdgeNoiseLive = edgeNoise; body.SetBiomeMaterialValues(); }
        }
    }
}
