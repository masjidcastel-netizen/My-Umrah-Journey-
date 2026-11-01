using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MyUmrah.EditorTools
{
    /// <summary>Automated project validation (menu or -executeMethod MyUmrah.EditorTools.ProjectValidator.RunBatch). Writes validation-report.txt.</summary>
    public static class ProjectValidator
    {
        class Result { public string name; public bool ok; public string detail; }

        static void Add(List<Result> r, string name, bool ok, string detail = "")
        {
            r.Add(new Result { name = name, ok = ok, detail = detail });
        }

        public static List<string> Run(out int failures)
        {
            var r = new List<Result>();

            Add(r, "Scene file exists", File.Exists(ProjectBuilder.ScenePath), ProjectBuilder.ScenePath);
            Add(r, "Scene is enabled in Build Settings",
                EditorBuildSettings.scenes.Any(s => s.path == ProjectBuilder.ScenePath && s.enabled));
            Add(r, "Android application id", PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) == ProjectBuilder.AppId,
                PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android));
            Add(r, "ARM64 target", (PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) != 0);
            Add(r, "IL2CPP scripting backend", PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) == ScriptingImplementation.IL2CPP);
            Add(r, "Landscape only",
                !PlayerSettings.allowedAutorotateToPortrait && !PlayerSettings.allowedAutorotateToPortraitUpsideDown &&
                PlayerSettings.allowedAutorotateToLandscapeLeft && PlayerSettings.allowedAutorotateToLandscapeRight);
            Add(r, "URP asset is the active render pipeline", GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset);

            var ga = AssetDatabase.LoadAssetAtPath<GameAssets>(ProjectBuilder.GameAssetsPath);
            Add(r, "GameAssets (materials) present", ga != null && ga.materials.Count >= MatLibrary.Defs.Length && ga.materials.All(m => m != null));
            Add(r, "Humanoid AnimatorController assigned", ga != null && ga.humanoid != null);
            Add(r, "Package manifest lists URP + Input System",
                File.Exists("Packages/manifest.json") &&
                File.ReadAllText("Packages/manifest.json").Contains("com.unity.render-pipelines.universal") &&
                File.ReadAllText("Packages/manifest.json").Contains("com.unity.inputsystem"));

            // Localisation completeness
            int bad = 0;
            foreach (var k in Loc.Keys)
            {
                var a = Loc.Raw(k);
                if (a == null || a.Length != 5 || a.Any(string.IsNullOrWhiteSpace)) bad++;
            }
            Add(r, "Localisation: every key has 5 languages", bad == 0, bad + " incomplete keys");

            // Missing scripts / null references in the scene
            if (File.Exists(ProjectBuilder.ScenePath))
            {
                var scene = EditorSceneManager.OpenScene(ProjectBuilder.ScenePath, OpenSceneMode.Single);
                int missing = 0;
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var t in root.GetComponentsInChildren<Transform>(true))
                        missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                Add(r, "No missing scripts in scene", missing == 0, missing + " missing");
                Add(r, "Scene has GameBootstrap", Object.FindFirstObjectByType<GameBootstrap>() != null);
            }

            failures = r.Count(x => !x.ok);
            return r.Select(x => (x.ok ? "PASS  " : "FAIL  ") + x.name + (string.IsNullOrEmpty(x.detail) ? "" : "  [" + x.detail + "]")).ToList();
        }

        [MenuItem("Umrah Journey/Validate Project")]
        public static void RunMenu()
        {
            int f; var lines = Run(out f);
            string report = string.Join("\n", lines) + "\n\nFailures: " + f;
            File.WriteAllText("validation-report.txt", report);
            Debug.Log("[MyUmrah] Validation\n" + report);
        }

        public static void RunBatch()
        {
            ProjectBuilder.BuildAll();
            int f; var lines = Run(out f);
            string report = string.Join("\n", lines) + "\n\nFailures: " + f;
            File.WriteAllText("validation-report.txt", report);
            Debug.Log("[MyUmrah] Validation\n" + report);
            EditorApplication.Exit(f == 0 ? 0 : 1);
        }
    }
}
