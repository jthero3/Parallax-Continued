using UnityEngine;

namespace Parallax
{
    [KSPAddon(KSPAddon.Startup.PSystemSpawn, false)]
    public class BiomeLayerLoader : MonoBehaviour
    {
        void Start()
        {
            ParallaxDebug.Log("[BiomeLayer] loader Start, bodies=" + ConfigLoader.parallaxTerrainBodies.Count);
            foreach (var kvp in ConfigLoader.parallaxTerrainBodies)
            {
                ParallaxTerrainBody body = kvp.Value;
                if (!body.HasBiomeLayers) continue;

                CelestialBody cb = FlightGlobals.GetBodyByName(body.planetName);
                if (cb == null || cb.BiomeMap == null)
                {
                    ParallaxDebug.LogError("[BiomeLayer] no biome map for " + body.planetName + ", skipping");
                    continue;
                }
                BiomeLayerBuilder.BuildAndBind(cb, body);
            }
        }
    }
}
