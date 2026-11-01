using UnityEngine;

namespace MyUmrah
{
    public struct HumanoidSpec
    {
        public bool female;
        public Color skin, outfit, accent, hair;
    }

    /// <summary>
    /// Builds a procedural humanoid (capsules/spheres). TEMPORARY PLACEHOLDER MODEL: replace with a rigged
    /// humanoid FBX + Avatar later. Limb joints are named ArmL/ArmR/LegL/LegR so the generated
    /// Animator clips (Assets/Animations) drive them.
    /// </summary>
    public static class HumanoidFactory
    {
        public static readonly Color[] Skins =
        {
            new Color(0.87f, 0.67f, 0.52f), new Color(0.65f, 0.45f, 0.32f),
            new Color(0.45f, 0.30f, 0.20f), new Color(0.95f, 0.78f, 0.64f)
        };
        public static readonly Color[] Outfits =
        {
            new Color(0.96f, 0.96f, 0.93f), new Color(0.85f, 0.88f, 0.9f),
            new Color(0.3f, 0.5f, 0.45f), new Color(0.75f, 0.7f, 0.6f)
        };
        public static readonly Color[] Scarves =
        {
            new Color(0.1f, 0.4f, 0.3f), new Color(0.25f, 0.3f, 0.5f),
            new Color(0.6f, 0.2f, 0.3f), new Color(0.9f, 0.9f, 0.88f)
        };

        public static HumanoidAnimator Create(Transform parent, HumanoidSpec s)
        {
            var root = new GameObject("Humanoid");
            root.transform.SetParent(parent, false);
            var rt = root.transform;

            Material skin = Mats.Tint("Skin", s.skin);
            Material cloth = Mats.Tint("Cloth", s.outfit);
            Material accent = Mats.Tint("Cloth", s.accent);
            Material hair = Mats.Tint("Hair", s.hair);
            Material dark = Mats.Get("Dark");

            if (s.female)
            {
                // Long abaya-style robe + hijab
                Prim.Make(PrimitiveType.Capsule, "Torso", rt, new Vector3(0, 1.12f, 0), new Vector3(0.46f, 0.34f, 0.28f), cloth);
                Prim.Make(PrimitiveType.Cylinder, "Robe", rt, new Vector3(0, 0.62f, 0), new Vector3(0.56f, 0.5f, 0.4f), cloth);
                Prim.Make(PrimitiveType.Sphere, "Head", rt, new Vector3(0, 1.66f, 0), new Vector3(0.25f, 0.27f, 0.25f), skin);
                Prim.Make(PrimitiveType.Sphere, "Hijab", rt, new Vector3(0, 1.68f, -0.02f), new Vector3(0.31f, 0.34f, 0.31f), accent);
                Prim.Make(PrimitiveType.Capsule, "Shawl", rt, new Vector3(0, 1.4f, 0), new Vector3(0.56f, 0.2f, 0.34f), accent);
                Prim.Make(PrimitiveType.Sphere, "Face", rt, new Vector3(0, 1.66f, 0.07f), new Vector3(0.22f, 0.25f, 0.2f), skin);
            }
            else
            {
                // Thobe + topi (white cap)
                Prim.Make(PrimitiveType.Capsule, "Torso", rt, new Vector3(0, 1.12f, 0), new Vector3(0.46f, 0.34f, 0.28f), cloth);
                Prim.Make(PrimitiveType.Cylinder, "Robe", rt, new Vector3(0, 0.7f, 0), new Vector3(0.5f, 0.42f, 0.34f), cloth);
                Prim.Make(PrimitiveType.Sphere, "Head", rt, new Vector3(0, 1.66f, 0), new Vector3(0.25f, 0.27f, 0.25f), skin);
                Prim.Make(PrimitiveType.Sphere, "Hair", rt, new Vector3(0, 1.7f, -0.02f), new Vector3(0.26f, 0.2f, 0.26f), hair);
                Prim.Make(PrimitiveType.Sphere, "Topi", rt, new Vector3(0, 1.78f, 0), new Vector3(0.27f, 0.13f, 0.27f), accent);
            }
            Prim.Make(PrimitiveType.Sphere, "EyeL", rt, new Vector3(-0.055f, 1.69f, 0.115f), Vector3.one * 0.035f, dark, false, default(Vector3), false);
            Prim.Make(PrimitiveType.Sphere, "EyeR", rt, new Vector3(0.055f, 1.69f, 0.115f), Vector3.one * 0.035f, dark, false, default(Vector3), false);

            MakeArm(rt, "ArmL", -0.30f, cloth, skin);
            MakeArm(rt, "ArmR", 0.30f, cloth, skin);
            MakeLeg(rt, "LegL", -0.1f, skin, dark);
            MakeLeg(rt, "LegR", 0.1f, skin, dark);

            var animator = root.AddComponent<Animator>();
            var assets = GameAssets.Instance;
            if (assets != null && assets.humanoid != null) animator.runtimeAnimatorController = assets.humanoid;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            var ha = root.AddComponent<HumanoidAnimator>();
            ha.Init(rt, animator);
            return ha;
        }

        static void MakeArm(Transform root, string name, float x, Material sleeve, Material skin)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(root, false);
            pivot.localPosition = new Vector3(x, 1.4f, 0);
            Prim.Make(PrimitiveType.Capsule, "Sleeve", pivot, new Vector3(0, -0.27f, 0), new Vector3(0.11f, 0.27f, 0.11f), sleeve);
            Prim.Make(PrimitiveType.Sphere, "Hand", pivot, new Vector3(0, -0.57f, 0), Vector3.one * 0.1f, skin);
        }

        static void MakeLeg(Transform root, string name, float x, Material skin, Material shoe)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(root, false);
            pivot.localPosition = new Vector3(x, 0.82f, 0);
            Prim.Make(PrimitiveType.Capsule, "Leg", pivot, new Vector3(0, -0.38f, 0), new Vector3(0.13f, 0.38f, 0.13f), skin);
            Prim.Make(PrimitiveType.Cube, "Shoe", pivot, new Vector3(0, -0.78f, 0.05f), new Vector3(0.14f, 0.07f, 0.26f), shoe);
        }
    }

    /// <summary>
    /// Drives the humanoid. Uses the generated Animator controller when available; otherwise falls back to
    /// a procedural limb swing so the character is always animated.
    /// </summary>
    public class HumanoidAnimator : MonoBehaviour
    {
        Animator animator;
        Transform visual, armL, armR, legL, legR;
        bool useController;
        float speed, phase, swimBlend;
        bool swimming, sitting, hideLegs;

        public void Init(Transform root, Animator a)
        {
            visual = root; animator = a;
            armL = root.Find("ArmL"); armR = root.Find("ArmR");
            legL = root.Find("LegL"); legR = root.Find("LegR");
            useController = a != null && a.runtimeAnimatorController != null;
        }

        /// <summary>speedParam: 0 idle, 1 walk, 2 run.</summary>
        public void Set(float speedParam, bool isSwimming) { speed = speedParam; swimming = isSwimming; }

        public void SetSitting(bool sit, bool hideLegGeometry)
        {
            sitting = sit; hideLegs = sit && hideLegGeometry;
            if (legL != null) legL.gameObject.SetActive(!hideLegs);
            if (legR != null) legR.gameObject.SetActive(!hideLegs);
        }

        public void Jump() { if (useController && animator != null) animator.SetTrigger("Jump"); }

        void Update()
        {
            float dt = Time.deltaTime;
            swimBlend = Mathf.MoveTowards(swimBlend, swimming ? 1f : 0f, dt * 4f);
            if (visual != null)
            {
                visual.localRotation = Quaternion.Euler(75f * swimBlend, 0f, 0f);
                visual.localPosition = new Vector3(0f, 0.95f * swimBlend, -1.0f * swimBlend);
            }

            if (useController && animator != null)
            {
                animator.SetFloat("Speed", speed, 0.1f, dt);
                animator.SetBool("Swimming", swimming);
                animator.SetBool("Sitting", sitting);
                return;
            }
            Procedural(dt);
        }

        void Procedural(float dt)
        {
            if (armL == null) return;
            float amp = Mathf.Clamp01(speed / 2f) * 0.5f + Mathf.Clamp01(speed) * 0.5f;
            phase += dt * (3f + speed * 4f);
            float swing = Mathf.Sin(phase) * 40f * amp * (speed > 1.2f ? 1.5f : 1f);
            if (sitting)
            {
                legL.localRotation = Quaternion.Euler(-85f, 0, 0);
                legR.localRotation = Quaternion.Euler(-85f, 0, 0);
                armL.localRotation = Quaternion.Euler(-30f, 0, 0);
                armR.localRotation = Quaternion.Euler(-30f, 0, 0);
            }
            else if (swimming)
            {
                float a = phase * 40f;
                armL.localRotation = Quaternion.Euler(-a, 0, 0);
                armR.localRotation = Quaternion.Euler(-a - 180f, 0, 0);
                legL.localRotation = Quaternion.Euler(Mathf.Sin(phase * 3f) * 15f, 0, 0);
                legR.localRotation = Quaternion.Euler(-Mathf.Sin(phase * 3f) * 15f, 0, 0);
            }
            else
            {
                legL.localRotation = Quaternion.Euler(swing, 0, 0);
                legR.localRotation = Quaternion.Euler(-swing, 0, 0);
                armL.localRotation = Quaternion.Euler(-swing * 0.8f, 0, 0);
                armR.localRotation = Quaternion.Euler(swing * 0.8f, 0, 0);
            }
        }
    }
}
