using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace MyUmrah
{
    /// <summary>
    /// Entry point of the MauritiusCastel scene. The scene only contains a Sun, a Camera and this component
    /// (created by the editor tool); everything else is built here at load: world, vehicles, NPCs, player, UI.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        GameUI ui;
        PlayerController player;
        CameraRig rig;
        WorldBuilder world;
        List<VehicleController> vehicles;
        bool started;
        float saveTimer;

        IEnumerator Start()
        {
            // Reset static state (supports "Enter Play Mode without domain reload")
            Missions.ResetEvents(); Inventory.ResetEvents(); Loc.ResetEvents();
            Interactable.All.Clear(); WaterVolume.All.Clear(); GameInput.ClearOneShots();
            Time.timeScale = 1f; GameState.Paused = false;

            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;

            GameState.Load();
            Loc.Set((Language)Mathf.Clamp(GameState.Data.language, 0, 4));
            QualityManager.Apply(GameState.Data.quality);

            EnsureEventSystem();
            ui = new GameObject("GameUI").AddComponent<GameUI>();
            ui.BuildShell();
            yield return null;

            SetupLighting();
            world = new GameObject("World").AddComponent<WorldBuilder>();
            yield return StartCoroutine(world.Build());

            vehicles = world.SpawnVehicles();
            world.SpawnNpcs();
            world.SpawnPickups();
            yield return null;

            SpawnPlayer();
            var tm = new GameObject("Traffic").AddComponent<TrafficManager>();
            tm.player = player.transform;

            ui.BuildGame(player, rig);
            ui.OnStartGame = OnStartGame;
            ui.OnSaveRequested = SaveNow;
            ui.SetLoading(false);
        }

        static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        static void SetupLighting()
        {
            Light sun = null;
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { sun = l; break; }
            if (sun == null)
            {
                var g = new GameObject("Sun");
                sun = g.AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            sun.shadows = LightShadows.Soft;
            sun.intensity = 1.25f;
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            RenderSettings.sun = sun;

            RenderSettings.skybox = Mats.Get("Skybox");
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 1.1f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 150f;
            RenderSettings.fogEndDistance = 430f;
            RenderSettings.fogColor = new Color(0.72f, 0.82f, 0.9f);
            DynamicGI.UpdateEnvironment();
        }

        void SpawnPlayer()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var cg = new GameObject("Main Camera");
                cg.tag = "MainCamera";
                cam = cg.AddComponent<Camera>();
            }
            rig = cam.GetComponent<CameraRig>();
            if (rig == null) rig = cam.gameObject.AddComponent<CameraRig>();
            rig.Init(cam);

            var pgo = new GameObject("Player");
            pgo.transform.position = WorldLayout.PlayerSpawn;
            player = pgo.AddComponent<PlayerController>();

            bool female = GameState.Data.gender == 1;
            var spec = new HumanoidSpec
            {
                female = female,
                skin = HumanoidFactory.Skins[0],
                outfit = HumanoidFactory.Outfits[0],
                accent = female ? HumanoidFactory.Scarves[0] : new Color(0.95f, 0.95f, 0.93f),
                hair = new Color(0.08f, 0.06f, 0.05f)
            };
            player.Anim = HumanoidFactory.Create(pgo.transform, spec);
            player.Rig = rig;
            player.CacheRenderers();

            rig.SetTarget(pgo.transform, false, null, 4.2f);
            rig.SetYaw(0f);
            rig.ModeChanged += () =>
            {
                player.SetModelVisible(!rig.FirstPerson);
                GameState.Data.cameraMode = rig.FirstPerson ? 1 : 0;
            };
            rig.SetFirstPerson(GameState.Data.cameraMode == 1);
            player.SetModelVisible(!rig.FirstPerson);
        }

        void OnStartGame(bool cont)
        {
            started = true;
            if (cont && GameState.Data.hasPosition)
            {
                var d = GameState.Data;
                player.Teleport(new Vector3(d.px, d.py + 0.3f, d.pz), d.yaw);
            }
            else
            {
                int lang = GameState.Data.language, q = GameState.Data.quality, cam = GameState.Data.cameraMode;
                SaveSystem.Delete();
                GameState.Data = new SaveData { language = lang, quality = q, cameraMode = cam };
            }
        }

        void SaveNow()
        {
            if (!started || player == null) return;
            var d = GameState.Data;
            Vector3 p = player.Driving && player.Vehicle != null ? player.Vehicle.ExitPosition : player.transform.position;
            d.hasPosition = true; d.px = p.x; d.py = p.y; d.pz = p.z;
            d.yaw = rig != null ? rig.Yaw : 0f;
            d.language = (int)Loc.Current;
            d.cameraMode = rig != null && rig.FirstPerson ? 1 : 0;
            GameState.Save();
        }

        void Update()
        {
            if (!started) return;
            GameState.Data.playSeconds += Time.unscaledDeltaTime;
            saveTimer += Time.unscaledDeltaTime;
            if (saveTimer > 30f) { saveTimer = 0f; SaveNow(); }
        }

        void OnApplicationPause(bool paused) { if (paused) SaveNow(); }
        void OnApplicationQuit() { SaveNow(); }
    }
}
