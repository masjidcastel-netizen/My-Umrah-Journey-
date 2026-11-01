using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MyUmrah
{
    /// <summary>Procedural primitive helper (genuine Unity 3D meshes, no external models required).</summary>
    public static class Prim
    {
        public static GameObject Make(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale,
            Material mat, bool collider = false, Vector3 euler = default(Vector3), bool shadows = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            t.localEulerAngles = euler;
            t.localScale = scale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            if (!shadows) r.shadowCastingMode = ShadowCastingMode.Off;
            if (!collider)
            {
                var c = go.GetComponent<Collider>();
                if (c != null) Object.Destroy(c);
            }
            return go;
        }

        public static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 size, Material mat,
            bool collider = false, Vector3 euler = default(Vector3), bool shadows = true)
        {
            return Make(PrimitiveType.Cube, name, parent, pos, size, mat, collider, euler, shadows);
        }

        public static void SetLayer(GameObject go, int layer)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        }
    }

    /// <summary>Simple object pool (used by the ambient traffic).</summary>
    public class SimplePool<T> where T : Component
    {
        readonly Stack<T> free = new Stack<T>();
        readonly System.Func<T> create;
        public SimplePool(System.Func<T> create) { this.create = create; }

        public T Get()
        {
            T t = free.Count > 0 ? free.Pop() : create();
            t.gameObject.SetActive(true);
            return t;
        }

        public void Release(T t)
        {
            t.gameObject.SetActive(false);
            free.Push(t);
        }
    }

    /// <summary>Base class for anything the player can interact with via the context button.</summary>
    public abstract class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();

        public string promptKey = "interact";
        public float range = 3f;

        protected virtual void OnEnable() { All.Add(this); }
        protected virtual void OnDisable() { All.Remove(this); }

        public virtual Vector3 InteractPoint { get { return transform.position; } }
        public virtual string PromptKey { get { return promptKey; } }
        public virtual bool CanInteract(PlayerController p) { return true; }
        public abstract void OnInteract(PlayerController p);
    }

    /// <summary>Adjustable graphics settings (mobile friendly). Uses the active URP asset.</summary>
    public static class QualityManager
    {
        public static readonly string[] Keys = { "q_low", "q_med", "q_high" };
        static readonly float[] scale = { 0.7f, 0.85f, 1f };
        static readonly float[] shadowDist = { 25f, 45f, 70f };
        static readonly int[] msaa = { 1, 1, 2 };
        static readonly float[] lod = { 0.7f, 1f, 1.4f };

        public static void Apply(int level)
        {
            level = Mathf.Clamp(level, 0, 2);
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null)
            {
                urp.renderScale = scale[level];
                urp.shadowDistance = shadowDist[level];
                urp.shadowCascadeCount = level == 0 ? 1 : 2;
                urp.msaaSampleCount = msaa[level];
            }
            QualitySettings.lodBias = lod[level];
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = level == 0 ? 30 : 60;
        }

        /// <summary>Distance-based culling: small props on layer 8 vanish beyond 170 m.</summary>
        public static void ApplyCulling(Camera cam)
        {
            var d = new float[32];
            d[8] = 170f;
            cam.layerCullDistances = d;
            cam.layerCullSpherical = true;
            cam.farClipPlane = 500f;
        }
    }
}
