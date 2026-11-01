using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MyUmrah
{
    /// <summary>Definition of a material. Shared by the editor generator (creates .mat assets) and the runtime fallback.</summary>
    [System.Serializable]
    public struct MatDef
    {
        public string name;
        public Color color;
        public float smooth;
        public float metal;
        public bool transparent;
        public string shader; // null = URP/Lit

        public MatDef(string n, Color c, float s = 0.12f, float m = 0f, bool t = false, string sh = null)
        {
            name = n; color = c; smooth = s; metal = m; transparent = t; shader = sh;
        }
    }

    public static class MatLibrary
    {
        static Color C(float r, float g, float b, float a = 1f) { return new Color(r, g, b, a); }

        public static readonly MatDef[] Defs =
        {
            new MatDef("Asphalt", C(0.16f, 0.16f, 0.17f), 0.08f),
            new MatDef("RoadLine", C(0.92f, 0.92f, 0.88f), 0.1f),
            new MatDef("Concrete", C(0.62f, 0.62f, 0.6f), 0.1f),
            new MatDef("Marble", C(0.93f, 0.92f, 0.88f), 0.5f),
            new MatDef("WallWhite", C(0.93f, 0.92f, 0.88f)),
            new MatDef("WallCream", C(0.9f, 0.84f, 0.66f)),
            new MatDef("WallBlue", C(0.55f, 0.72f, 0.85f)),
            new MatDef("WallPink", C(0.9f, 0.68f, 0.66f)),
            new MatDef("WallGreen", C(0.6f, 0.78f, 0.6f)),
            new MatDef("RoofRed", C(0.62f, 0.2f, 0.16f)),
            new MatDef("RoofTile", C(0.45f, 0.3f, 0.22f)),
            new MatDef("MasjidGreen", C(0.08f, 0.45f, 0.28f), 0.4f),
            new MatDef("Gold", C(0.85f, 0.68f, 0.22f), 0.7f, 0.6f),
            new MatDef("Glass", C(0.55f, 0.78f, 0.9f, 0.45f), 0.9f, 0f, true),
            new MatDef("WoodDark", C(0.25f, 0.16f, 0.1f)),
            new MatDef("Grass", C(0.3f, 0.55f, 0.2f)),
            new MatDef("Sand", C(0.85f, 0.78f, 0.55f)),
            new MatDef("Water", C(0.1f, 0.45f, 0.6f, 0.62f), 0.95f, 0f, true),
            new MatDef("Leaf", C(0.12f, 0.4f, 0.15f)),
            new MatDef("Trunk", C(0.4f, 0.3f, 0.2f)),
            new MatDef("Flamboyant", C(0.9f, 0.25f, 0.12f)),
            new MatDef("Skin", C(0.8f, 0.6f, 0.45f), 0.2f),
            new MatDef("Cloth", C(1f, 1f, 1f), 0.1f),
            new MatDef("Hair", C(0.08f, 0.06f, 0.05f)),
            new MatDef("Dark", C(0.08f, 0.08f, 0.09f)),
            new MatDef("CarRed", C(0.75f, 0.12f, 0.12f), 0.6f, 0.2f),
            new MatDef("CarWhite", C(0.92f, 0.92f, 0.92f), 0.6f, 0.2f),
            new MatDef("CarBlue", C(0.15f, 0.25f, 0.6f), 0.6f, 0.2f),
            new MatDef("CarSilver", C(0.7f, 0.72f, 0.74f), 0.6f, 0.4f),
            new MatDef("TaxiYellow", C(0.95f, 0.78f, 0.1f), 0.5f, 0.1f),
            new MatDef("BusBlue", C(0.15f, 0.35f, 0.7f), 0.5f, 0.1f),
            new MatDef("Tyre", C(0.05f, 0.05f, 0.05f), 0.2f),
            new MatDef("Chrome", C(0.8f, 0.8f, 0.82f), 0.9f, 0.9f),
            new MatDef("AwningRed", C(0.8f, 0.2f, 0.2f)),
            new MatDef("PrayerMat", C(0.1f, 0.5f, 0.35f)),
            new MatDef("TerrainLit", C(1f, 1f, 1f), 0f, 0f, false, "Universal Render Pipeline/Terrain/Lit"),
            new MatDef("Skybox", C(1f, 1f, 1f), 0f, 0f, false, "Skybox/Procedural"),
        };
    }

    public static class MatFactory
    {
        public static Material Create(MatDef d)
        {
            Shader sh = Shader.Find(string.IsNullOrEmpty(d.shader) ? "Universal Render Pipeline/Lit" : d.shader);
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            var m = new Material(sh);
            m.name = d.name;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", d.color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", d.color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", d.smooth);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", d.metal);
            if (d.transparent)
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)RenderQueue.Transparent;
                m.SetShaderPassEnabled("ShadowCaster", false);
            }
            m.enableInstancing = true;
            return m;
        }
    }

    /// <summary>Holds generated assets that must be included in the player build (materials, animator, skybox).</summary>
    public class GameAssets : ScriptableObject
    {
        public List<Material> materials = new List<Material>();
        public RuntimeAnimatorController humanoid;

        static GameAssets inst;
        public static GameAssets Instance
        {
            get
            {
                if (inst == null) inst = Resources.Load<GameAssets>("GameAssets");
                return inst;
            }
        }

        public Material FindMaterial(string n)
        {
            foreach (var m in materials) if (m != null && m.name == n) return m;
            return null;
        }
    }

    public static class Mats
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        public static Material Get(string name)
        {
            Material m;
            if (cache.TryGetValue(name, out m) && m != null) return m;
            var assets = GameAssets.Instance;
            if (assets != null) m = assets.FindMaterial(name);
            if (m == null)
            {
                foreach (var d in MatLibrary.Defs) if (d.name == name) { m = MatFactory.Create(d); break; }
            }
            if (m == null) m = MatFactory.Create(new MatDef(name, Color.magenta));
            cache[name] = m;
            return m;
        }

        /// <summary>A tinted instance of a base material (cached per colour).</summary>
        public static Material Tint(string baseName, Color c)
        {
            string key = baseName + "#" + ColorUtility.ToHtmlStringRGB(c);
            Material m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            m = new Material(Get(baseName));
            m.name = key;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            cache[key] = m;
            return m;
        }
    }
}
