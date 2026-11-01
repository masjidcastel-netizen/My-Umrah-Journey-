using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MyUmrah.EditorTools
{
    /// <summary>
    /// One-click project setup: Player Settings (Android, ARM64, IL2CPP, landscape), materials, animations,
    /// URP mobile asset, the MauritiusCastel scene and Build Settings. Runs automatically on first open
    /// (when the scene is missing) and from the "Umrah Journey" menu.
    /// </summary>
    public static class ProjectBuilder
    {
        public const string AppId = "com.masjidcastel.myumrahjourney";
        public const string ScenePath = "Assets/Scenes/MauritiusCastel.unity";
        public const string GameAssetsPath = "Assets/Resources/GameAssets.asset";
        public const string UrpPath = "Assets/Settings/UmrahMobile_URP.asset";
        public const string UrpRendererPath = "Assets/Settings/UmrahMobile_Renderer.asset";

        [InitializeOnLoadMethod]
        static void AutoRun()
        {
            EditorApplication.delayCall += () =>
            {
                if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
                if (!File.Exists(ScenePath) || !File.Exists(GameAssetsPath)) BuildAll();
            };
        }

        [MenuItem("Umrah Journey/Setup/Build Everything (settings, materials, animations, URP, scene)")]
        public static void BuildAll()
        {
            ConfigurePlayerSettings();
            EnsureFolders();
            var materials = CreateMaterials();
            var controller = AnimationBuilder.Build();
            CreateGameAssets(materials, controller);
            CreateUrp();
            CreateScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MyUmrah] Project setup complete: " + ScenePath);
        }

        static void EnsureFolders()
        {
            foreach (var f in new[] { "Materials", "Resources", "Settings", "Scenes", "Animations" })
                if (!AssetDatabase.IsValidFolder("Assets/" + f)) AssetDatabase.CreateFolder("Assets", f);
        }

        [MenuItem("Umrah Journey/Setup/Apply Android Player Settings")]
        public static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "MasjidCastel";
            PlayerSettings.productName = "My Umrah Journey";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AppId);
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.Android.bundleVersionCode = 1;

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Medium);
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });

            // The code uses the new Input System package. 0 = old only; 1 = new only; 2 = both.
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets != null && assets.Length > 0)
            {
                var so = new SerializedObject(assets[0]);
                var p = so.FindProperty("activeInputHandler");
                if (p != null && p.intValue == 0)
                {
                    p.intValue = 1;
                    so.ApplyModifiedProperties();
                    Debug.LogWarning("[MyUmrah] Active Input Handler set to 'Input System Package'. If Unity asks to restart, accept.");
                }
            }
        }

        static List<Material> CreateMaterials()
        {
            var list = new List<Material>();
            foreach (var def in MatLibrary.Defs)
            {
                string path = "Assets/Materials/" + def.name + ".mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null)
                {
                    m = MatFactory.Create(def);
                    AssetDatabase.CreateAsset(m, path);
                }
                list.Add(m);
            }
            return list;
        }

        static void CreateGameAssets(List<Material> materials, AnimatorController controller)
        {
            var ga = AssetDatabase.LoadAssetAtPath<GameAssets>(GameAssetsPath);
            if (ga == null)
            {
                ga = ScriptableObject.CreateInstance<GameAssets>();
                AssetDatabase.CreateAsset(ga, GameAssetsPath);
            }
            ga.materials = materials;
            ga.humanoid = controller;
            EditorUtility.SetDirty(ga);
        }

        static void SetBool(SerializedObject so, string n, bool v) { var p = so.FindProperty(n); if (p != null) p.boolValue = v; }
        static void SetInt(SerializedObject so, string n, int v) { var p = so.FindProperty(n); if (p != null) p.intValue = v; }
        static void SetFloat(SerializedObject so, string n, float v) { var p = so.FindProperty(n); if (p != null) p.floatValue = v; }

        static void CreateUrp()
        {
            var rd = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(UrpRendererPath);
            if (rd == null)
            {
                rd = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rd, UrpRendererPath);
            }
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpPath);
            if (asset == null)
            {
                asset = UniversalRenderPipelineAsset.Create(rd);
                AssetDatabase.CreateAsset(asset, UrpPath);
            }

            // Mobile-friendly defaults (QualityManager adjusts render scale / shadows at runtime).
            var so = new SerializedObject(asset);
            SetBool(so, "m_SupportsHDR", false);
            SetInt(so, "m_MSAA", 1);
            SetFloat(so, "m_RenderScale", 0.85f);
            SetFloat(so, "m_ShadowDistance", 45f);
            SetInt(so, "m_ShadowCascadeCount", 2);
            SetInt(so, "m_MainLightShadowmapResolution", 1024);
            SetInt(so, "m_AdditionalLightsRenderingMode", 0);
            SetBool(so, "m_SupportsDynamicBatching", true);
            SetBool(so, "m_UseSRPBatcher", true);
            SetBool(so, "m_RequireDepthTexture", false);
            SetBool(so, "m_RequireOpaqueTexture", false);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);

            GraphicsSettings.defaultRenderPipeline = asset;
            int cur = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = asset;
            }
            QualitySettings.SetQualityLevel(cur, false);
        }

        [MenuItem("Umrah Journey/Setup/Create Scene Only")]
        public static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.nearClipPlane = 0.15f; cam.farClipPlane = 500f;
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = WorldLayout.PlayerSpawn + new Vector3(0f, 3f, -8f);
            camGo.transform.rotation = Quaternion.Euler(10f, 0f, 0f);

            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.intensity = 1.25f;
            sun.color = new Color(1f, 0.96f, 0.88f);
            sunGo.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            RenderSettings.sun = sun;

            var sky = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Skybox.mat");
            if (sky != null) RenderSettings.skybox = sky;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 150f;
            RenderSettings.fogEndDistance = 430f;
            RenderSettings.fogColor = new Color(0.72f, 0.82f, 0.9f);

            new GameObject("GameBootstrap").AddComponent<GameBootstrap>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }
    }
}
