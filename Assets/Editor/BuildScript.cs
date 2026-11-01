using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MyUmrah.EditorTools
{
    /// <summary>
    /// Android builds. Menu: Umrah Journey > Build. Command line:
    ///   Unity -batchmode -nographics -quit -projectPath . -buildTarget Android
    ///         -executeMethod MyUmrah.EditorTools.BuildScript.BuildDebugApk -logFile build.log
    /// Release builds need environment variables (never commit secrets):
    ///   UMRAH_KEYSTORE_PATH, UMRAH_KEYSTORE_PASS, UMRAH_KEY_ALIAS, UMRAH_KEY_PASS.  Optional: UMRAH_OUT (default Builds/Android).
    /// </summary>
    public static class BuildScript
    {
        [MenuItem("Umrah Journey/Build/Android Debug APK")]
        public static void BuildDebugApk() { Run(false, false); }

        [MenuItem("Umrah Journey/Build/Android Release APK (needs keystore env vars)")]
        public static void BuildReleaseApk() { Run(false, true); }

        [MenuItem("Umrah Journey/Build/Android Release AAB for Google Play (needs keystore env vars)")]
        public static void BuildReleaseAab() { Run(true, true); }

        static string Env(string n) { return System.Environment.GetEnvironmentVariable(n); }

        static void Run(bool aab, bool release)
        {
            int code = 0;
            try
            {
                ProjectBuilder.BuildAll();
                string outDir = Env("UMRAH_OUT");
                if (string.IsNullOrEmpty(outDir)) outDir = "Builds/Android";
                Directory.CreateDirectory(outDir);

                if (release)
                {
                    string ks = Env("UMRAH_KEYSTORE_PATH"), kp = Env("UMRAH_KEYSTORE_PASS"), ka = Env("UMRAH_KEY_ALIAS"), kap = Env("UMRAH_KEY_PASS");
                    if (string.IsNullOrEmpty(ks) || string.IsNullOrEmpty(kp) || string.IsNullOrEmpty(ka) || string.IsNullOrEmpty(kap) || !File.Exists(ks))
                    {
                        Debug.LogError("[MyUmrah] Release build needs UMRAH_KEYSTORE_PATH (existing file), UMRAH_KEYSTORE_PASS, UMRAH_KEY_ALIAS, UMRAH_KEY_PASS. Nothing was built.");
                        Finish(2);
                        return;
                    }
                    PlayerSettings.Android.useCustomKeystore = true;
                    PlayerSettings.Android.keystoreName = ks;
                    PlayerSettings.Android.keystorePass = kp;
                    PlayerSettings.Android.keyaliasName = ka;
                    PlayerSettings.Android.keyaliasPass = kap;
                }
                else PlayerSettings.Android.useCustomKeystore = false; // debug keystore

                EditorUserBuildSettings.buildAppBundle = aab;
                string file = Path.Combine(outDir, "MyUmrahJourney-" + (release ? "release" : "debug") + (aab ? ".aab" : ".apk"));
                var opts = new BuildPlayerOptions
                {
                    scenes = new[] { ProjectBuilder.ScenePath },
                    locationPathName = file,
                    target = BuildTarget.Android,
                    targetGroup = BuildTargetGroup.Android,
                    options = release ? BuildOptions.None : BuildOptions.Development
                };

                BuildReport report = BuildPipeline.BuildPlayer(opts);
                var s = report.summary;
                Debug.Log("[MyUmrah] Build result: " + s.result + ", errors: " + s.totalErrors + ", size: " + s.totalSize + " bytes, output: " + s.outputPath);
                code = s.result == BuildResult.Succeeded ? 0 : 1;
            }
            catch (System.Exception e)
            {
                Debug.LogError("[MyUmrah] Build exception: " + e);
                code = 3;
            }
            Finish(code);
        }

        static void Finish(int code)
        {
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }
    }
}
