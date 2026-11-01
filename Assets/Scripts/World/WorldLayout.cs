using System.Collections.Generic;
using UnityEngine;

namespace MyUmrah
{
    /// <summary>
    /// Pure-math description of the Castel test area (no GameObjects): terrain height, roads, exclusion zones.
    /// NOT geographically accurate. It is a stylised layout, to be replaced by verified map data in a later phase.
    /// </summary>
    public static class WorldLayout
    {
        public const float TerrainSize = 800f;
        public const float TerrainHeight = 100f;
        public const float TerrainBaseY = -10f;
        public const float PlateauHeight = 8f;
        public const float PlateauRadius = 200f;
        public const float SeaLevel = 1.5f;
        public const float LakeLevel = 6.2f;
        public const float RoadWidth = 7f;
        public const float RoadY = 8.005f;
        public const float RoadThickness = 0.02f;

        public static readonly Vector2 LakeCenter = new Vector2(82f, 10f);
        public const float LakeRadius = 28f;
        public const float LakeVolumeRadius = 38f;

        public static readonly Vector2 RingCenter = new Vector2(0f, -45f);
        public const float RingRadius = 16f;

        public static readonly Rect MasjidCompound = new Rect(-32f, -24f, 64f, 64f); // x, z, width, depth
        public static readonly Rect PetrolStation = new Rect(-73f, -84f, 16f, 18f);

        public static readonly Vector3 PlayerSpawn = new Vector3(2f, PlateauHeight + 0.1f, -84f);

        public static readonly Vector2[] LoopCorners =
        {
            new Vector2(-150f, -90f), new Vector2(130f, -90f), new Vector2(130f, 110f), new Vector2(-150f, 110f)
        };

        public static Vector2[] LoopPath;
        public static float[] LoopCum;
        public static float LoopLength;

        /// <summary>All road centre-lines. Each entry: points + whether the polyline is closed.</summary>
        public static readonly List<Vector2[]> Polylines = new List<Vector2[]>();
        public static readonly List<bool> PolylineClosed = new List<bool>();

        static WorldLayout() { BuildRoads(); }

        public static float Smooth(float a, float b, float x)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, x));
        }

        /// <summary>World-space terrain height in metres at (x, z).</summary>
        public static float Height(float x, float z)
        {
            float d = Mathf.Sqrt(x * x + z * z);
            float rise = Smooth(PlateauRadius, PlateauRadius + 60f, d);
            float fall = Smooth(295f, 355f, d);
            float n = Mathf.PerlinNoise(x * 0.011f + 37.1f, z * 0.011f + 91.7f);
            float land = PlateauHeight + n * 24f * rise;
            float h = Mathf.Lerp(land, -5f, fall);

            float ld = Vector2.Distance(new Vector2(x, z), LakeCenter);
            if (ld < LakeRadius + 8f)
            {
                float w = 1f - Smooth(LakeRadius - 9f, LakeRadius + 8f, ld);
                h = Mathf.Lerp(h, 3.2f, w);
            }
            return h;
        }

        static void BuildRoads()
        {
            // Rounded rectangular loop (quadratic Bezier corners).
            var pts = new List<Vector2>();
            const float R = 20f;
            int n = LoopCorners.Length;
            for (int i = 0; i < n; i++)
            {
                Vector2 prev = LoopCorners[(i + n - 1) % n];
                Vector2 c = LoopCorners[i];
                Vector2 next = LoopCorners[(i + 1) % n];
                Vector2 a = c + (prev - c).normalized * R;
                Vector2 b = c + (next - c).normalized * R;
                for (int k = 0; k <= 8; k++)
                {
                    float t = k / 8f;
                    pts.Add(Vector2.Lerp(Vector2.Lerp(a, c, t), Vector2.Lerp(c, b, t), t));
                }
            }
            LoopPath = pts.ToArray();
            LoopCum = new float[LoopPath.Length];
            float acc = 0f;
            for (int i = 0; i < LoopPath.Length; i++)
            {
                LoopCum[i] = acc;
                acc += Vector2.Distance(LoopPath[i], LoopPath[(i + 1) % LoopPath.Length]);
            }
            LoopLength = acc;
            Polylines.Add(LoopPath); PolylineClosed.Add(true);

            // Roundabout in front of the Masjid.
            var ring = new Vector2[28];
            for (int i = 0; i < ring.Length; i++)
            {
                float a = i * Mathf.PI * 2f / ring.Length;
                ring[i] = RingCenter + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * RingRadius;
            }
            Polylines.Add(ring); PolylineClosed.Add(true);

            Polylines.Add(new[] { new Vector2(0f, -90f), new Vector2(0f, RingCenter.y - RingRadius) }); PolylineClosed.Add(false);
            Polylines.Add(new[] { new Vector2(0f, RingCenter.y + RingRadius), new Vector2(0f, -22f) }); PolylineClosed.Add(false);
            Polylines.Add(new[] { new Vector2(130f, 10f), new Vector2(195f, 10f) }); PolylineClosed.Add(false); // exit road for future expansion
        }

        /// <summary>Position and unit direction along the loop at arc length s (wraps).</summary>
        public static void SampleLoop(float s, out Vector2 pos, out Vector2 dir)
        {
            s = Mathf.Repeat(s, LoopLength);
            int lo = 0, hi = LoopCum.Length - 1;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                if (LoopCum[mid] <= s) lo = mid; else hi = mid - 1;
            }
            Vector2 a = LoopPath[lo];
            Vector2 b = LoopPath[(lo + 1) % LoopPath.Length];
            float segLen = Vector2.Distance(a, b);
            float t = segLen > 0.0001f ? (s - LoopCum[lo]) / segLen : 0f;
            pos = Vector2.Lerp(a, b, t);
            dir = (b - a).normalized;
        }

        public static float DistanceToRoads(Vector2 p)
        {
            float best = float.MaxValue;
            for (int i = 0; i < Polylines.Count; i++)
            {
                var pl = Polylines[i];
                int count = PolylineClosed[i] ? pl.Length : pl.Length - 1;
                for (int k = 0; k < count; k++)
                {
                    float d = DistToSegment(p, pl[k], pl[(k + 1) % pl.Length]);
                    if (d < best) best = d;
                }
            }
            return best;
        }

        static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float len2 = ab.sqrMagnitude;
            float t = len2 < 0.0001f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
            return Vector2.Distance(p, a + ab * t);
        }

        static bool InRect(Rect r, Vector2 p, float margin)
        {
            return p.x > r.xMin - margin && p.x < r.xMax + margin && p.y > r.yMin - margin && p.y < r.yMax + margin;
        }

        /// <summary>True if a building/tree of radius r can be placed at p (ignores other props).</summary>
        public static bool IsFree(Vector2 p, float r, float roadClear)
        {
            if (p.magnitude > PlateauRadius - 15f) return false;
            if (DistanceToRoads(p) < roadClear + r) return false;
            if (InRect(MasjidCompound, p, r + 4f)) return false;
            if (InRect(PetrolStation, p, r + 4f)) return false;
            if (Vector2.Distance(p, LakeCenter) < LakeVolumeRadius + r) return false;
            return true;
        }
    }
}
