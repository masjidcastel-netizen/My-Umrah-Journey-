using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace MyUmrah
{
    /// <summary>
    /// Procedurally builds the Castel test area from Unity primitives: terrain, lake, sea, roads, roundabout,
    /// Masjid Castel (placeholder architecture), houses, shops, petrol station and vegetation (with LODs).
    /// All geometry is real 3D. Everything here is a TEMPORARY PLACEHOLDER layout, not survey-accurate.
    /// </summary>
    public class WorldBuilder : MonoBehaviour
    {
        readonly System.Random rng = new System.Random(20251101);
        readonly List<Vector2> occupied = new List<Vector2>();
        Transform tStatic;
        const float Y0 = WorldLayout.PlateauHeight;

        float R(float a, float b) { return a + (float)rng.NextDouble() * (b - a); }

        static Transform Group(string name, Transform parent)
        {
            var g = new GameObject(name).transform;
            g.SetParent(parent, false);
            return g;
        }

        public IEnumerator Build()
        {
            tStatic = Group("StaticWorld", transform);
            BuildTerrain(); yield return null;
            BuildWater(); BuildRoads(); yield return null;
            BuildMasjid(); yield return null;
            BuildPetrolStation(); BuildHousesAndShops(); yield return null;
            StaticBatchingUtility.Combine(tStatic.gameObject);
            yield return null;
            BuildVegetation(); yield return null;
        }

        // ------------------------------------------------------------------ Terrain
        void BuildTerrain()
        {
            var td = new TerrainData();
            td.heightmapResolution = 257;
            td.size = new Vector3(WorldLayout.TerrainSize, WorldLayout.TerrainHeight, WorldLayout.TerrainSize);
            int res = td.heightmapResolution;
            float half = WorldLayout.TerrainSize * 0.5f;
            var h = new float[res, res];
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    float wx = -half + x / (float)(res - 1) * WorldLayout.TerrainSize;
                    float wz = -half + y / (float)(res - 1) * WorldLayout.TerrainSize;
                    h[y, x] = Mathf.Clamp01((WorldLayout.Height(wx, wz) - WorldLayout.TerrainBaseY) / WorldLayout.TerrainHeight);
                }
            td.SetHeights(0, 0, h);

            var grass = MakeLayer(new Color(0.25f, 0.5f, 0.16f), new Color(0.36f, 0.62f, 0.22f), 1);
            var sand = MakeLayer(new Color(0.78f, 0.7f, 0.48f), new Color(0.88f, 0.8f, 0.58f), 2);
            var rock = MakeLayer(new Color(0.38f, 0.34f, 0.3f), new Color(0.5f, 0.46f, 0.4f), 3);
            td.terrainLayers = new[] { grass, sand, rock };

            td.alphamapResolution = 256;
            int ar = td.alphamapResolution;
            var a = new float[ar, ar, 3];
            for (int y = 0; y < ar; y++)
                for (int x = 0; x < ar; x++)
                {
                    float wx = -half + x / (float)(ar - 1) * WorldLayout.TerrainSize;
                    float wz = -half + y / (float)(ar - 1) * WorldLayout.TerrainSize;
                    float hh = WorldLayout.Height(wx, wz);
                    float slope = (Mathf.Abs(WorldLayout.Height(wx + 3f, wz) - hh) + Mathf.Abs(WorldLayout.Height(wx, wz + 3f) - hh)) / 6f;
                    float sw = 1f - WorldLayout.Smooth(2.4f, 3.8f, hh);
                    float rw = WorldLayout.Smooth(0.45f, 0.9f, slope) * (1f - sw);
                    float gw = Mathf.Max(0f, 1f - sw - rw);
                    float sum = sw + rw + gw + 0.0001f;
                    a[y, x, 0] = gw / sum; a[y, x, 1] = sw / sum; a[y, x, 2] = rw / sum;
                }
            td.SetAlphamaps(0, 0, a);

            var go = Terrain.CreateTerrainGameObject(td);
            go.name = "Terrain";
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(-half, WorldLayout.TerrainBaseY, -half);
            var t = go.GetComponent<Terrain>();
            t.materialTemplate = Mats.Get("TerrainLit");
            t.heightmapPixelError = 10f;
            t.basemapDistance = 120f;
            t.shadowCastingMode = ShadowCastingMode.Off;
            t.drawInstanced = true;
        }

        static TerrainLayer MakeLayer(Color a, Color b, int seed)
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x * Mathf.PI * 2f / n, w = y * Mathf.PI * 2f / n;
                    float v1 = Mathf.PerlinNoise(Mathf.Cos(u) * 2f + seed * 7f, Mathf.Sin(u) * 2f + seed * 3f);
                    float v2 = Mathf.PerlinNoise(Mathf.Cos(w) * 2f + seed * 5f + 40f, Mathf.Sin(w) * 2f + seed * 9f + 40f);
                    tex.SetPixel(x, y, Color.Lerp(a, b, (v1 + v2) * 0.5f));
                }
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.Apply(true, true);
            var layer = new TerrainLayer();
            layer.diffuseTexture = tex;
            layer.tileSize = new Vector2(10f, 10f);
            return layer;
        }

        // ------------------------------------------------------------------ Water
        void BuildWater()
        {
            var water = Mats.Get("Water");
            var sea = Prim.Make(PrimitiveType.Plane, "Sea", transform, new Vector3(0f, WorldLayout.SeaLevel, 0f),
                new Vector3(200f, 1f, 200f), water, false, default(Vector3), false);
            var sv = sea.AddComponent<WaterVolume>();
            sv.surfaceY = WorldLayout.SeaLevel; sv.radius = 0f;

            var lake = Prim.Make(PrimitiveType.Cylinder, "Lake", transform,
                new Vector3(WorldLayout.LakeCenter.x, WorldLayout.LakeLevel, WorldLayout.LakeCenter.y),
                new Vector3(60f, 0.01f, 60f), water, false, default(Vector3), false);
            var lv = lake.AddComponent<WaterVolume>();
            lv.surfaceY = WorldLayout.LakeLevel; lv.center = WorldLayout.LakeCenter; lv.radius = WorldLayout.LakeVolumeRadius;
        }

        // ------------------------------------------------------------------ Roads
        void BuildRoads()
        {
            var parent = Group("Roads", tStatic);
            Material asphalt = Mats.Get("Asphalt"), line = Mats.Get("RoadLine");
            float ry = WorldLayout.RoadY, th = WorldLayout.RoadThickness;

            for (int i = 0; i < WorldLayout.Polylines.Count; i++)
            {
                var pl = WorldLayout.Polylines[i];
                bool closed = WorldLayout.PolylineClosed[i];
                int segs = closed ? pl.Length : pl.Length - 1;
                for (int k = 0; k < segs; k++)
                {
                    Vector2 a = pl[k], b = pl[(k + 1) % pl.Length];
                    Vector2 d = b - a;
                    float len = d.magnitude;
                    if (len < 0.01f) continue;
                    Vector2 mid = (a + b) * 0.5f;
                    float yaw = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;
                    Prim.Box("Road", parent, new Vector3(mid.x, ry, mid.y), new Vector3(WorldLayout.RoadWidth, th, len + 0.3f),
                        asphalt, false, new Vector3(0f, yaw, 0f), false);
                    Prim.Make(PrimitiveType.Cylinder, "Joint", parent, new Vector3(a.x, ry, a.y),
                        new Vector3(WorldLayout.RoadWidth, th * 0.5f, WorldLayout.RoadWidth), asphalt, false, default(Vector3), false);

                    if (i != 1) // centre dashes (not on the roundabout ring)
                    {
                        int n = Mathf.FloorToInt(len / 8f);
                        for (int j = 0; j < n; j++)
                        {
                            Vector2 p = a + d / len * (j * 8f + 4f);
                            Prim.Box("Dash", parent, new Vector3(p.x, ry + 0.012f, p.y), new Vector3(0.18f, 0.01f, 3.2f),
                                line, false, new Vector3(0f, yaw, 0f), false);
                        }
                    }
                }
            }

            // Roundabout island
            Vector3 c = new Vector3(WorldLayout.RingCenter.x, 0f, WorldLayout.RingCenter.y);
            Prim.Make(PrimitiveType.Cylinder, "RoundaboutKerb", parent, new Vector3(c.x, Y0 + 0.08f, c.z), new Vector3(24f, 0.08f, 24f), Mats.Get("Concrete"), true);
            Prim.Make(PrimitiveType.Cylinder, "RoundaboutGrass", parent, new Vector3(c.x, Y0 + 0.14f, c.z), new Vector3(22f, 0.08f, 22f), Mats.Get("Grass"), true);
        }

        // ------------------------------------------------------------------ Masjid Castel
        void BuildMasjid()
        {
            var m = Group("MasjidCastel", tStatic);
            Material white = Mats.Get("WallWhite"), green = Mats.Get("MasjidGreen"), gold = Mats.Get("Gold"),
                     marble = Mats.Get("Marble"), dark = Mats.Get("Dark");

            Prim.Box("Courtyard", m, new Vector3(0, Y0 + 0.05f, -12f), new Vector3(60f, 0.1f, 24f), marble);
            Prim.Box("Plinth", m, new Vector3(0, Y0 + 0.3f, 11f), new Vector3(40f, 0.6f, 34f), marble, true);
            float hy = Y0 + 0.6f;                       // top of plinth
            Prim.Box("Hall", m, new Vector3(0, hy + 4.5f, 13f), new Vector3(36f, 9f, 26f), white, true);
            Prim.Box("BandLow", m, new Vector3(0, hy + 0.6f, 13f), new Vector3(36.3f, 1f, 26.3f), green);
            Prim.Box("BandTop", m, new Vector3(0, hy + 8.4f, 13f), new Vector3(36.3f, 1f, 26.3f), green);
            Prim.Box("Parapet", m, new Vector3(0, hy + 9.3f, 13f), new Vector3(37f, 0.6f, 27f), white);
            float roofY = hy + 9.6f;

            // Main dome
            Prim.Make(PrimitiveType.Cylinder, "DomeDrum", m, new Vector3(0, roofY + 0.9f, 13f), new Vector3(12f, 0.9f, 12f), white);
            Prim.Make(PrimitiveType.Sphere, "MainDome", m, new Vector3(0, roofY + 1.8f, 13f), new Vector3(16f, 13f, 16f), green);
            Prim.Make(PrimitiveType.Cylinder, "DomeSpire", m, new Vector3(0, roofY + 9.2f, 13f), new Vector3(0.2f, 1f, 0.2f), gold);
            Prim.Make(PrimitiveType.Sphere, "DomeFinial", m, new Vector3(0, roofY + 10.5f, 13f), Vector3.one * 0.8f, gold);

            // Four corner domes
            foreach (float x in new[] { -14f, 14f })
                foreach (float z in new[] { 3f, 23f })
                {
                    Prim.Make(PrimitiveType.Sphere, "CornerDome", m, new Vector3(x, roofY + 0.3f, z), new Vector3(6f, 5f, 6f), green);
                    Prim.Make(PrimitiveType.Sphere, "CornerFinial", m, new Vector3(x, roofY + 3.0f, z), Vector3.one * 0.5f, gold);
                }

            // Front porch with colonnade
            Prim.Box("PorchRoof", m, new Vector3(0, hy + 6.9f, -3f), new Vector3(34f, 0.6f, 6.4f), white, true);
            Prim.Box("PorchTrim", m, new Vector3(0, hy + 6.45f, -6.1f), new Vector3(34.2f, 0.4f, 0.3f), green);
            for (int i = 0; i < 8; i++)
                Prim.Make(PrimitiveType.Cylinder, "Column", m, new Vector3(-14f + i * 4f, hy + 3.3f, -5.5f), new Vector3(1f, 3.3f, 1f), marble, true);

            // Entrance portal + windows
            Prim.Box("Portal", m, new Vector3(0, hy + 3f, -0.1f), new Vector3(7f, 6f, 0.3f), dark);
            Prim.Box("PortalFrameL", m, new Vector3(-3.9f, hy + 3.2f, -0.2f), new Vector3(0.6f, 6.4f, 0.4f), gold);
            Prim.Box("PortalFrameR", m, new Vector3(3.9f, hy + 3.2f, -0.2f), new Vector3(0.6f, 6.4f, 0.4f), gold);
            Prim.Box("PortalLintel", m, new Vector3(0, hy + 6.3f, -0.2f), new Vector3(8.4f, 0.6f, 0.4f), gold);
            foreach (float x in new[] { -14f, -9f, 9f, 14f })
                Prim.Box("Window", m, new Vector3(x, hy + 3.6f, -0.1f), new Vector3(2f, 3.6f, 0.2f), dark, false, default(Vector3), false);

            // Minarets
            foreach (float x in new[] { -21f, 21f })
            {
                Prim.Box("MinaretBase", m, new Vector3(x, Y0 + 1.5f, -3f), new Vector3(4.6f, 3f, 4.6f), white, true);
                Prim.Make(PrimitiveType.Cylinder, "MinaretShaft", m, new Vector3(x, Y0 + 13f, -3f), new Vector3(2.6f, 10f, 2.6f), white, true);
                Prim.Make(PrimitiveType.Cylinder, "MinaretRing1", m, new Vector3(x, Y0 + 8f, -3f), new Vector3(3f, 0.2f, 3f), green);
                Prim.Make(PrimitiveType.Cylinder, "MinaretRing2", m, new Vector3(x, Y0 + 16f, -3f), new Vector3(3f, 0.2f, 3f), green);
                Prim.Make(PrimitiveType.Cylinder, "MinaretBalcony", m, new Vector3(x, Y0 + 23f, -3f), new Vector3(4.2f, 0.25f, 4.2f), marble);
                Prim.Make(PrimitiveType.Cylinder, "MinaretUpper", m, new Vector3(x, Y0 + 25f, -3f), new Vector3(2f, 2f, 2f), white);
                Prim.Make(PrimitiveType.Sphere, "MinaretDome", m, new Vector3(x, Y0 + 27f, -3f), new Vector3(2.6f, 3.2f, 2.6f), green);
                Prim.Make(PrimitiveType.Cylinder, "MinaretSpire", m, new Vector3(x, Y0 + 29.2f, -3f), new Vector3(0.12f, 1.2f, 0.12f), gold);
                Prim.Make(PrimitiveType.Sphere, "MinaretFinial", m, new Vector3(x, Y0 + 30.6f, -3f), Vector3.one * 0.45f, gold);
            }

            // Compound wall + gate
            Prim.Box("WallBack", m, new Vector3(0, Y0 + 1.2f, 40f), new Vector3(64f, 2.4f, 0.6f), white, true);
            Prim.Box("WallWest", m, new Vector3(-32f, Y0 + 1.2f, 8f), new Vector3(0.6f, 2.4f, 64f), white, true);
            Prim.Box("WallEast", m, new Vector3(32f, Y0 + 1.2f, 8f), new Vector3(0.6f, 2.4f, 64f), white, true);
            Prim.Box("WallFrontW", m, new Vector3(-18.5f, Y0 + 1.2f, -24f), new Vector3(27f, 2.4f, 0.6f), white, true);
            Prim.Box("WallFrontE", m, new Vector3(18.5f, Y0 + 1.2f, -24f), new Vector3(27f, 2.4f, 0.6f), white, true);
            foreach (float x in new[] { -5.6f, 5.6f })
            {
                Prim.Box("GatePillar", m, new Vector3(x, Y0 + 2f, -24f), new Vector3(1.2f, 4f, 1.2f), white, true);
                Prim.Make(PrimitiveType.Sphere, "GateCap", m, new Vector3(x, Y0 + 4.4f, -24f), Vector3.one * 0.9f, gold);
            }
            Prim.Box("GateLintel", m, new Vector3(0, Y0 + 4.3f, -24f), new Vector3(12f, 0.8f, 0.8f), green);
            WorldLabel(m, new Vector3(0, Y0 + 6.4f, -24.6f), Loc.T("masjid_name"), 14f);

            // Wudhu area (placeholder; interactive wudhu comes in Phase C)
            Prim.Box("WudhuWall", m, new Vector3(-24f, Y0 + 1.5f, -14f), new Vector3(12f, 3f, 0.5f), white, true);
            Prim.Box("WudhuRoof", m, new Vector3(-24f, Y0 + 3.15f, -12.4f), new Vector3(13f, 0.3f, 4f), green);
            Prim.Box("WudhuBench", m, new Vector3(-24f, Y0 + 0.3f, -12.6f), new Vector3(12f, 0.5f, 1.2f), marble, true);
            for (int i = 0; i < 6; i++)
                Prim.Make(PrimitiveType.Cylinder, "Tap", m, new Vector3(-28.5f + i * 1.8f, Y0 + 1.1f, -13.6f), new Vector3(0.12f, 0.2f, 0.12f), Mats.Get("Chrome"));

            // Interactable landmark trigger point
            var lm = new GameObject("MasjidLandmark");
            lm.transform.SetParent(transform, false);
            lm.transform.position = new Vector3(0, Y0, -20f);
            lm.AddComponent<LandmarkInteractable>();

            occupied.Add(new Vector2(0f, 8f));
        }

        static void WorldLabel(Transform parent, Vector3 pos, string text, float widthMetres)
        {
            var go = new GameObject("WorldLabel");
            go.transform.SetParent(parent, false);
            var cv = go.AddComponent<Canvas>();
            cv.renderMode = RenderMode.WorldSpace;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(900f, 170f);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * (widthMetres / 900f);

            var bg = new GameObject("Bg").AddComponent<Image>();
            bg.transform.SetParent(go.transform, false);
            bg.color = new Color(0.04f, 0.33f, 0.2f, 0.95f);
            var brt = bg.rectTransform;
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one; brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;

            var tx = new GameObject("Text").AddComponent<Text>();
            tx.transform.SetParent(go.transform, false);
            tx.font = UiKit.Font;
            tx.text = text;
            tx.fontSize = 56;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.color = new Color(1f, 0.93f, 0.7f);
            tx.horizontalOverflow = HorizontalWrapMode.Wrap;
            var trt = tx.rectTransform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = new Vector2(20f, 8f); trt.offsetMax = new Vector2(-20f, -8f);
        }

        // ------------------------------------------------------------------ Petrol station
        void BuildPetrolStation()
        {
            var p = Group("PetrolStation", tStatic);
            float cx = -65f, cz = -78f;
            Prim.Box("Canopy", p, new Vector3(cx, Y0 + 5f, cz), new Vector3(14f, 0.5f, 7f), Mats.Get("WallWhite"), true);
            Prim.Box("CanopyTrim", p, new Vector3(cx, Y0 + 4.7f, cz), new Vector3(14.2f, 0.25f, 7.2f), Mats.Get("AwningRed"));
            foreach (float dx in new[] { -5.5f, 5.5f })
                foreach (float dz in new[] { -2.5f, 2.5f })
                    Prim.Make(PrimitiveType.Cylinder, "Column", p, new Vector3(cx + dx, Y0 + 2.5f, cz + dz), new Vector3(0.6f, 2.5f, 0.6f), Mats.Get("Concrete"), true);
            Prim.Box("Island", p, new Vector3(cx, Y0 + 0.1f, cz), new Vector3(12f, 0.2f, 3f), Mats.Get("Concrete"), true);
            foreach (float dx in new[] { -2.5f, 2.5f })
            {
                Prim.Box("Pump", p, new Vector3(cx + dx, Y0 + 1.05f, cz), new Vector3(0.8f, 1.7f, 0.6f), Mats.Get("CarRed"), true);
                Prim.Box("PumpTop", p, new Vector3(cx + dx, Y0 + 2f, cz), new Vector3(0.9f, 0.25f, 0.7f), Mats.Get("WallWhite"));
            }
            Prim.Box("Shop", p, new Vector3(cx, Y0 + 1.75f, cz + 8f), new Vector3(10f, 3.5f, 6f), Mats.Get("WallCream"), true);
            Prim.Box("ShopGlass", p, new Vector3(cx, Y0 + 1.6f, cz + 4.95f), new Vector3(7f, 2f, 0.1f), Mats.Get("Glass"), false, default(Vector3), false);

            var pump = new GameObject("FuelPump");
            pump.transform.SetParent(transform, false);
            pump.transform.position = new Vector3(cx, Y0, cz + 1.5f);
            pump.AddComponent<FuelPump>();
            occupied.Add(new Vector2(cx, cz + 4f));
        }

        // ------------------------------------------------------------------ Houses & shops
        void BuildHousesAndShops()
        {
            var houses = Group("Houses", tStatic);
            string[] walls = { "WallWhite", "WallCream", "WallBlue", "WallPink", "WallGreen" };
            string[] roofs = { "RoofRed", "RoofTile" };

            // Shops along the south road (inner side), facing south.
            float[] shopX = { 20f, 44f, 68f, 94f };
            string[] signs = { "WallBlue", "AwningRed", "WallGreen", "WallPink" };
            for (int i = 0; i < shopX.Length; i++)
            {
                Shop(houses, new Vector3(shopX[i], Y0, -76f), signs[i]);
                occupied.Add(new Vector2(shopX[i], -76f));
            }

            int n = WorldLayout.LoopCorners.Length;
            for (int e = 0; e < n; e++)
            {
                Vector2 A = WorldLayout.LoopCorners[e], B = WorldLayout.LoopCorners[(e + 1) % n];
                Vector2 dir = (B - A).normalized;
                Vector2 left = new Vector2(-dir.y, dir.x);
                float len = Vector2.Distance(A, B);
                for (float t = 32f; t < len - 32f; t += R(24f, 32f))
                {
                    foreach (int side in new[] { 1, -1 })
                    {
                        Vector2 pos = A + dir * t + left * side * R(15f, 18f);
                        if (!WorldLayout.IsFree(pos, 8f, 9f)) continue;
                        bool clash = false;
                        foreach (var o in occupied) if (Vector2.Distance(o, pos) < 17f) { clash = true; break; }
                        if (clash) continue;
                        Vector2 toRoad = -left * side;
                        float yaw = Mathf.Atan2(toRoad.x, toRoad.y) * Mathf.Rad2Deg;
                        House(houses, new Vector3(pos.x, Y0, pos.y), yaw,
                            new Vector3(R(8f, 11f), R(3.2f, 4f), R(7f, 9f)),
                            walls[rng.Next(walls.Length)], roofs[rng.Next(roofs.Length)]);
                        occupied.Add(pos);
                    }
                }
            }
        }

        void House(Transform parent, Vector3 pos, float yaw, Vector3 size, string wallMat, string roofMat)
        {
            var root = Group("House", parent);
            root.position = pos;
            root.rotation = Quaternion.Euler(0f, yaw, 0f);
            Prim.Box("Walls", root, new Vector3(0, size.y * 0.5f, 0), size, Mats.Get(wallMat), true);
            float a = 28f * Mathf.Deg2Rad;
            float slab = size.z * 0.5f / Mathf.Cos(a) + 0.35f;
            float ry = size.y + size.z * 0.25f * Mathf.Tan(a);
            Material roof = Mats.Get(roofMat);
            Prim.Box("RoofFront", root, new Vector3(0, ry, size.z * 0.25f), new Vector3(size.x + 0.7f, 0.18f, slab), roof, false, new Vector3(28f, 0, 0));
            Prim.Box("RoofBack", root, new Vector3(0, ry, -size.z * 0.25f), new Vector3(size.x + 0.7f, 0.18f, slab), roof, false, new Vector3(-28f, 0, 0));
            Prim.Box("Door", root, new Vector3(0, 1.05f, size.z * 0.5f + 0.03f), new Vector3(1f, 2.1f, 0.1f), Mats.Get("WoodDark"), false, default(Vector3), false);
            foreach (float s in new[] { -1f, 1f })
                Prim.Box("Window", root, new Vector3(s * size.x * 0.3f, 1.6f, size.z * 0.5f + 0.03f), new Vector3(1.1f, 1f, 0.1f), Mats.Get("Glass"), false, default(Vector3), false);
        }

        void Shop(Transform parent, Vector3 pos, string signMat)
        {
            var root = Group("Shop", parent);
            root.position = pos;
            root.rotation = Quaternion.Euler(0f, 180f, 0f); // front faces south
            Prim.Box("Body", root, new Vector3(0, 2.1f, 0), new Vector3(11f, 4.2f, 8f), Mats.Get("WallCream"), true);
            Prim.Box("Parapet", root, new Vector3(0, 4.4f, 0), new Vector3(11.4f, 0.4f, 8.4f), Mats.Get("WallWhite"));
            Prim.Box("Sign", root, new Vector3(0, 3.6f, 4.1f), new Vector3(7f, 0.9f, 0.25f), Mats.Get(signMat));
            for (int i = 0; i < 5; i++)
                Prim.Box("Awning", root, new Vector3(-4.56f + i * 2.28f, 3.0f, 5.2f), new Vector3(2.28f, 0.12f, 2.6f),
                    Mats.Get(i % 2 == 0 ? "AwningRed" : "WallWhite"), false, new Vector3(20f, 0, 0));
            Prim.Box("Door", root, new Vector3(-2f, 1.2f, 4.03f), new Vector3(2f, 2.4f, 0.1f), Mats.Get("Glass"), false, default(Vector3), false);
            Prim.Box("Window", root, new Vector3(2.6f, 1.5f, 4.03f), new Vector3(4f, 1.8f, 0.1f), Mats.Get("Glass"), false, default(Vector3), false);
        }

        // ------------------------------------------------------------------ Vegetation (LOD + layer-8 distance culling)
        void BuildVegetation()
        {
            var veg = Group("Vegetation", transform);
            var placed = new List<Vector2>();

            // Hand-placed palms inside the Masjid compound and on the roundabout island.
            foreach (var p in new[] { new Vector2(-12, -17), new Vector2(12, -17), new Vector2(27, -12), new Vector2(27, 33), new Vector2(-27, 33), new Vector2(0, -45) })
            {
                Palm(veg, new Vector3(p.x, Y0 + (p.x == 0f && p.y == -45f ? 0.18f : 0f), p.y), R(7f, 9f));
                placed.Add(p);
            }

            int guard = 0, palms = 0;
            while (palms < 55 && guard++ < 4000)
            {
                Vector2 p = new Vector2(R(-190f, 190f), R(-190f, 190f));
                if (!WorldLayout.IsFree(p, 2f, 7f) || TooClose(placed, p, 6f)) continue;
                Palm(veg, new Vector3(p.x, Y0, p.y), R(6f, 10f));
                placed.Add(p); palms++;
            }

            guard = 0; int broad = 0;
            while (broad < 25 && guard++ < 4000)
            {
                Vector2 p = new Vector2(R(-190f, 190f), R(-190f, 190f));
                if (!WorldLayout.IsFree(p, 3f, 8f) || TooClose(placed, p, 8f)) continue;
                BroadTree(veg, new Vector3(p.x, Y0, p.y), R(4f, 5.5f), rng.NextDouble() < 0.35 ? "Flamboyant" : "Leaf", true);
                placed.Add(p); broad++;
            }

            // Forest on the hills outside the plateau.
            guard = 0; int forest = 0;
            while (forest < 110 && guard++ < 6000)
            {
                float ang = R(0f, Mathf.PI * 2f), rad = R(205f, 310f);
                Vector2 p = new Vector2(Mathf.Cos(ang) * rad, Mathf.Sin(ang) * rad);
                float h = WorldLayout.Height(p.x, p.y);
                if (h < 4f) continue;
                BroadTree(veg, new Vector3(p.x, h - 0.2f, p.y), R(5f, 8f), "Leaf", false);
                forest++;
            }
        }

        static bool TooClose(List<Vector2> list, Vector2 p, float d)
        {
            for (int i = 0; i < list.Count; i++) if ((list[i] - p).sqrMagnitude < d * d) return true;
            return false;
        }

        void Palm(Transform parent, Vector3 pos, float h)
        {
            var root = new GameObject("Palm");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            var trunk = Prim.Make(PrimitiveType.Cylinder, "Trunk", root.transform, new Vector3(0, h * 0.5f, 0),
                new Vector3(0.35f, h * 0.5f, 0.35f), Mats.Get("Trunk"), false, new Vector3(R(-4f, 4f), 0, R(-4f, 4f)));
            var crown = new GameObject("Crown").transform;
            crown.SetParent(root.transform, false);
            crown.localPosition = new Vector3(0, h, 0);

            var lod0 = new List<Renderer> { trunk.GetComponent<Renderer>() };
            for (int i = 0; i < 8; i++)
            {
                var holder = new GameObject("Frond").transform;
                holder.SetParent(crown, false);
                holder.localEulerAngles = new Vector3(0, i * 45f + R(-8f, 8f), 0);
                var leaf = Prim.Box("Leaf", holder, new Vector3(0, 0, 1.5f), new Vector3(0.55f, 0.05f, 3.2f), Mats.Get("Leaf"), false, new Vector3(22f, 0, 0));
                lod0.Add(leaf.GetComponent<Renderer>());
            }
            var blob = Prim.Make(PrimitiveType.Sphere, "CrownLow", crown, new Vector3(0, 0.1f, 0), new Vector3(5.5f, 1.1f, 5.5f), Mats.Get("Leaf"), false, default(Vector3), false);
            var lod1 = new List<Renderer> { trunk.GetComponent<Renderer>(), blob.GetComponent<Renderer>() };

            var lg = root.AddComponent<LODGroup>();
            lg.SetLODs(new[] { new LOD(0.10f, lod0.ToArray()), new LOD(0.02f, lod1.ToArray()) });
            lg.RecalculateBounds();

            var cap = root.AddComponent<CapsuleCollider>();
            cap.radius = 0.4f; cap.height = h; cap.center = new Vector3(0, h * 0.5f, 0);
            Prim.SetLayer(root, 8);
        }

        void BroadTree(Transform parent, Vector3 pos, float h, string leafMat, bool full)
        {
            var root = new GameObject("Tree");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            var trunk = Prim.Make(PrimitiveType.Cylinder, "Trunk", root.transform, new Vector3(0, h * 0.4f, 0),
                new Vector3(0.5f, h * 0.4f, 0.5f), Mats.Get("Trunk"));
            Material leaf = Mats.Get(leafMat);
            var lod0 = new List<Renderer> { trunk.GetComponent<Renderer>() };
            var canopy = new List<Renderer>();
            float r = h * 0.55f;
            if (full)
            {
                foreach (var o in new[] { new Vector3(0, h * 0.95f, 0), new Vector3(r * 0.45f, h * 0.8f, 0.3f), new Vector3(-r * 0.4f, h * 0.85f, -0.3f) })
                    lod0.Add(Prim.Make(PrimitiveType.Sphere, "Canopy", root.transform, o, new Vector3(r * 1.5f, r * 1.1f, r * 1.5f), leaf).GetComponent<Renderer>());
                var low = Prim.Make(PrimitiveType.Sphere, "CanopyLow", root.transform, new Vector3(0, h * 0.9f, 0), new Vector3(r * 2.2f, r * 1.3f, r * 2.2f), leaf, false, default(Vector3), false);
                canopy.Add(trunk.GetComponent<Renderer>()); canopy.Add(low.GetComponent<Renderer>());
                var lg = root.AddComponent<LODGroup>();
                lg.SetLODs(new[] { new LOD(0.10f, lod0.ToArray()), new LOD(0.02f, canopy.ToArray()) });
                lg.RecalculateBounds();
            }
            else
            {
                lod0.Add(Prim.Make(PrimitiveType.Sphere, "Canopy", root.transform, new Vector3(0, h * 0.9f, 0), new Vector3(r * 2f, r * 1.4f, r * 2f), leaf, false, default(Vector3), false).GetComponent<Renderer>());
                var lg = root.AddComponent<LODGroup>();
                lg.SetLODs(new[] { new LOD(0.03f, lod0.ToArray()) });
                lg.RecalculateBounds();
            }
            if (full)
            {
                var cap = root.AddComponent<CapsuleCollider>();
                cap.radius = 0.45f; cap.height = h * 0.8f; cap.center = new Vector3(0, h * 0.4f, 0);
            }
            Prim.SetLayer(root, 8);
        }

        // ------------------------------------------------------------------ Spawned actors
        public List<VehicleController> SpawnVehicles()
        {
            var list = new List<VehicleController>();
            var vs = Group("Vehicles", transform);
            list.Add(VehicleFactory.Create(VehicleKind.Car, new Vector3(-7.5f, Y0 + 0.5f, -80f), 0f));
            list.Add(VehicleFactory.Create(VehicleKind.Taxi, new Vector3(-7.5f, Y0 + 0.5f, -72f), 0f));
            list.Add(VehicleFactory.Create(VehicleKind.Bus, new Vector3(11f, Y0 + 0.6f, -72f), 0f));
            list.Add(VehicleFactory.Create(VehicleKind.Motorcycle, new Vector3(6.5f, Y0 + 0.4f, -82f), 0f));
            foreach (var v in list) v.transform.SetParent(vs, true);
            return list;
        }

        public void SpawnNpcs()
        {
            var ns = Group("NPCs", transform);
            Vector3[] homes =
            {
                new Vector3(-8, 0, -12), new Vector3(10, 0, -14), new Vector3(0, 0, -30), new Vector3(40, 0, -66),
                new Vector3(80, 0, -66), new Vector3(-60, 0, -66), new Vector3(-100, 0, 60), new Vector3(100, 0, 80)
            };
            for (int i = 0; i < homes.Length; i++)
            {
                var go = new GameObject("NPC_" + i);
                go.transform.SetParent(ns, false);
                go.transform.position = new Vector3(homes[i].x, WorldLayout.Height(homes[i].x, homes[i].z), homes[i].z);
                var spec = new HumanoidSpec
                {
                    female = i % 2 == 1,
                    skin = HumanoidFactory.Skins[i % HumanoidFactory.Skins.Length],
                    outfit = HumanoidFactory.Outfits[(i + 1) % HumanoidFactory.Outfits.Length],
                    accent = HumanoidFactory.Scarves[i % HumanoidFactory.Scarves.Length],
                    hair = new Color(0.08f, 0.06f, 0.05f)
                };
                var anim = HumanoidFactory.Create(go.transform, spec);
                var cap = go.AddComponent<CapsuleCollider>();
                cap.radius = 0.3f; cap.height = 1.8f; cap.center = new Vector3(0, 0.9f, 0);
                var rb = go.AddComponent<Rigidbody>(); rb.isKinematic = true;
                var npc = go.AddComponent<WanderNPC>();
                npc.home = go.transform.position; npc.Anim = anim; npc.lineIndex = i;
                npc.wanderRadius = i < 3 ? 14f : 20f;
            }
        }

        public void SpawnPickups()
        {
            if (Inventory.Has("item_prayer_mat")) return;
            var go = new GameObject("PrayerMat");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(-10f, Y0 + 0.4f, -14f);
            Prim.Box("Mat", go.transform, Vector3.zero, new Vector3(0.7f, 0.06f, 1.2f), Mats.Get("PrayerMat"));
            Prim.Box("MatBorder", go.transform, new Vector3(0, -0.01f, 0), new Vector3(0.8f, 0.04f, 1.3f), Mats.Get("Gold"));
            go.AddComponent<PickupItem>();
        }
    }
}
