using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MyUmrah
{
    /// <summary>
    /// All native Unity uGUI: touch joystick, look zone, action buttons, HUD, title, pause, missions, inventory, info.
    /// Built in code at runtime (reference resolution 1920x1080, safe-area aware).
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        public static GameUI I { get; private set; }
        public static void Toast(string t) { if (I != null) I.ShowToast(t); }
        public static void ToastKey(string key, params object[] args)
        {
            if (I == null) return;
            I.ShowToast(args != null && args.Length > 0 ? Loc.T(key, args) : Loc.T(key));
        }

        public System.Action<bool> OnStartGame;   // true = continue from save
        public System.Action OnSaveRequested;
        public bool TitleActive { get; private set; }

        RectTransform root, safe;
        PlayerController player;
        Text missionTxt, moneyTxt, toastTxt, driveTxt, interactTxt, langTxt, qualTxt, missionsBody, invBody, infoTitle, infoBody;
        Image interactImg, runImg;
        GameObject footGroup, driveGroup, loadingPanel, titlePanel, pausePanel, missionsPanel, inventoryPanel, infoPanel, hud;
        float toastTimer, hudTimer;
        Rect lastSafe;

        static readonly Color Teal = new Color(0.12f, 0.42f, 0.32f, 1f);
        static readonly Color Btn = new Color(0.1f, 0.1f, 0.12f, 0.55f);
        static readonly Color Gold = new Color(1f, 0.86f, 0.45f);

        void Awake() { I = this; }
        void OnDestroy() { if (I == this) I = null; }

        // ------------------------------------------------------------------ Shell (shown while the world loads)
        public void BuildShell()
        {
            var cgo = new GameObject("Canvas");
            cgo.transform.SetParent(transform, false);
            var canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var sc = cgo.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920f, 1080f);
            sc.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            sc.matchWidthOrHeight = 0.5f;
            cgo.AddComponent<GraphicRaycaster>();
            root = (RectTransform)cgo.transform;

            safe = UiKit.Node("Safe", root);
            UiKit.Stretch(safe);

            var lp = UiKit.Img(root, "Loading", new Color(0.03f, 0.08f, 0.07f, 1f));
            UiKit.Stretch(lp.rectTransform);
            var lt = UiKit.Label(lp.transform, "Text", Loc.T("loading"), 54, TextAnchor.MiddleCenter, Gold);
            UiKit.Stretch(lt.rectTransform);
            loadingPanel = lp.gameObject;
        }

        public void SetLoading(bool on) { if (loadingPanel != null) loadingPanel.SetActive(on); }

        // ------------------------------------------------------------------ Game UI
        public void BuildGame(PlayerController p, CameraRig rig)
        {
            player = p;

            // Touch zones first so buttons sit on top
            var look = UiKit.Img(safe, "LookZone", new Color(0, 0, 0, 0));
            UiKit.Place(look.rectTransform, new Vector2(1, 0), new Vector2(1, 0), Vector2.zero, Vector2.zero);
            look.rectTransform.anchorMin = new Vector2(0.4f, 0f); look.rectTransform.anchorMax = Vector2.one;
            look.rectTransform.offsetMin = Vector2.zero; look.rectTransform.offsetMax = Vector2.zero;
            look.gameObject.AddComponent<LookZone>();

            BuildJoystick();

            hud = UiKit.Node("HUD", safe).gameObject;
            UiKit.Stretch((RectTransform)hud.transform);

            missionTxt = HudText("Mission", 34, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -26), new Vector2(900, 110));
            moneyTxt = HudText("Money", 40, TextAnchor.UpperCenter, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -22), new Vector2(500, 60));
            toastTxt = HudText("Toast", 40, TextAnchor.UpperCenter, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -100), new Vector2(1300, 120));
            toastTxt.color = Gold;
            driveTxt = HudText("Drive", 44, TextAnchor.LowerCenter, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(900, 70));

            // Top-right: menu, missions, inventory, camera
            RoundBtn(hud.transform, "BtnMenu", "menu", new Vector2(1, 1), new Vector2(-30, -30), 110, Btn, 28).OnDown = GameInput.PressMenu;
            RoundBtn(hud.transform, "BtnMissions", "missions", new Vector2(1, 1), new Vector2(-160, -30), 110, Btn, 24).OnDown = OpenMissions;
            RoundBtn(hud.transform, "BtnInventory", "inventory", new Vector2(1, 1), new Vector2(-290, -30), 110, Btn, 24).OnDown = OpenInventory;
            RoundBtn(hud.transform, "BtnCamera", "camera", new Vector2(1, 1), new Vector2(-420, -30), 110, Btn, 28).OnDown = GameInput.PressCamera;

            // Context button (interact / enter / exit / talk / learn)
            var ib = RoundBtn(hud.transform, "BtnInteract", "interact", new Vector2(1, 0), new Vector2(-360, 90), 170, new Color(0.12f, 0.5f, 0.38f, 0.85f), 34);
            ib.OnDown = GameInput.PressInteract;
            interactImg = ib.GetComponent<Image>();
            interactTxt = ib.GetComponentInChildren<Text>();
            Destroy(interactTxt.GetComponent<LocLabel>());

            // On-foot controls
            footGroup = UiKit.Node("OnFoot", hud.transform).gameObject;
            UiKit.Stretch((RectTransform)footGroup.transform);
            RoundBtn(footGroup.transform, "BtnJump", "jump", new Vector2(1, 0), new Vector2(-70, 110), 190, Btn, 38).OnDown = GameInput.PressJump;
            var run = RoundBtn(footGroup.transform, "BtnRun", "run", new Vector2(1, 0), new Vector2(-110, 330), 140, Btn, 30);
            runImg = run.GetComponent<Image>();
            run.OnDown = () => { GameInput.TouchSprint = !GameInput.TouchSprint; runImg.color = GameInput.TouchSprint ? new Color(0.9f, 0.7f, 0.2f, 0.85f) : Btn; };

            // Driving controls
            driveGroup = UiKit.Node("Driving", hud.transform).gameObject;
            UiKit.Stretch((RectTransform)driveGroup.transform);
            var acc = RoundBtn(driveGroup.transform, "BtnAccel", "accel", new Vector2(1, 0), new Vector2(-70, 110), 210, new Color(0.15f, 0.55f, 0.25f, 0.7f), 34);
            acc.OnDown = () => GameInput.TouchAccel = true; acc.OnUp = () => GameInput.TouchAccel = false;
            var brk = RoundBtn(driveGroup.transform, "BtnBrake", "brake", new Vector2(1, 0), new Vector2(-110, 340), 160, new Color(0.7f, 0.15f, 0.15f, 0.7f), 34);
            brk.OnDown = () => GameInput.TouchBrake = true; brk.OnUp = () => GameInput.TouchBrake = false;
            driveGroup.SetActive(false);

            BuildPanels();
            BuildTitle();

            Missions.Completed += d => ShowToast(Loc.T("mission_complete") + "  " + Loc.T(d.titleKey) + "  (" + Loc.T("reward", d.reward) + ")");
            Loc.Changed += RefreshDynamic;
            RefreshDynamic();

            hud.SetActive(false);
        }

        Text HudText(string name, int size, TextAnchor anchor, Vector2 a, Vector2 pivot, Vector2 pos, Vector2 sz)
        {
            var t = UiKit.Label(hud.transform, name, "", size, anchor, Color.white);
            UiKit.Place(t.rectTransform, a, pivot, pos, sz);
            t.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2f, -2f);
            return t;
        }

        void BuildJoystick()
        {
            var zone = UiKit.Img(safe, "JoystickZone", new Color(0, 0, 0, 0));
            zone.rectTransform.anchorMin = new Vector2(0f, 0f);
            zone.rectTransform.anchorMax = new Vector2(0.4f, 0.7f);
            zone.rectTransform.offsetMin = Vector2.zero; zone.rectTransform.offsetMax = Vector2.zero;
            zone.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            var bas = UiKit.Img(zone.transform, "Base", new Color(1, 1, 1, 0.17f), UiKit.Circle, false);
            UiKit.Place(bas.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-150f, -60f), new Vector2(280f, 280f));
            var knob = UiKit.Img(bas.transform, "Knob", new Color(1, 1, 1, 0.35f), UiKit.Circle, false);
            UiKit.Place(knob.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(130f, 130f));

            var js = zone.gameObject.AddComponent<JoystickZone>();
            js.zone = zone.rectTransform; js.baseRt = bas.rectTransform; js.knobRt = knob.rectTransform; js.radius = 120f;
            js.Setup();
        }

        HoldButton RoundBtn(Transform parent, string name, string key, Vector2 anchor, Vector2 pos, float size, Color col, int font)
        {
            var img = UiKit.Img(parent, name, col, UiKit.Circle);
            UiKit.Place(img.rectTransform, anchor, anchor, pos, new Vector2(size, size));
            var hb = img.gameObject.AddComponent<HoldButton>();
            var t = UiKit.Label(img.transform, "Label", Loc.T(key), font, TextAnchor.MiddleCenter, Color.white);
            UiKit.Stretch(t.rectTransform);
            t.rectTransform.offsetMin = new Vector2(8, 8); t.rectTransform.offsetMax = new Vector2(-8, -8);
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 14; t.resizeTextMaxSize = font;
            var ll = t.gameObject.AddComponent<LocLabel>();
            ll.key = key; ll.Refresh();
            return hb;
        }

        // ------------------------------------------------------------------ Panels
        RectTransform MakePanel(string name, string titleKey, out RectTransform content, out GameObject panel)
        {
            var back = UiKit.Img(root, name, new Color(0, 0, 0, 0.65f));
            UiKit.Stretch(back.rectTransform);
            var box = UiKit.Img(back.transform, "Box", new Color(0.06f, 0.1f, 0.12f, 0.97f));
            UiKit.Place(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 840f));
            var title = UiKit.Label(box.transform, "Title", Loc.T(titleKey), 56, TextAnchor.MiddleCenter, Gold);
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(1000, 90));
            var ll = title.gameObject.AddComponent<LocLabel>(); ll.key = titleKey; ll.Refresh();
            content = UiKit.Node("Content", box.transform);
            UiKit.Place(content, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -5), new Vector2(1000, 600));
            var close = MenuButton(box.transform, "close", ClosePanels, null);
            var crt = (RectTransform)close.transform;
            UiKit.Place(crt, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(380, 80));
            Destroy(close.GetComponent<LayoutElement>());
            back.gameObject.SetActive(false);
            panel = back.gameObject;
            return back.rectTransform;
        }

        Button MenuButton(Transform parent, string key, System.Action click, Text[] labelOut)
        {
            var img = UiKit.Img(parent, key ?? "Button", Teal);
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.onClick.AddListener(() => click());
            var t = UiKit.Label(img.transform, "Text", key != null ? Loc.T(key) : "", 36, TextAnchor.MiddleCenter, Color.white);
            UiKit.Stretch(t.rectTransform);
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 16; t.resizeTextMaxSize = 36;
            if (key != null) { var ll = t.gameObject.AddComponent<LocLabel>(); ll.key = key; ll.Refresh(); }
            if (labelOut != null && labelOut.Length > 0) labelOut[0] = t;
            var le = img.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = 80;
            return b;
        }

        void BuildPanels()
        {
            RectTransform content;

            MakePanel("PausePanel", "menu", out content, out pausePanel);
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(480, 84); grid.spacing = new Vector2(24, 18);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.UpperCenter;
            MenuButton(content, "resume", ClosePanels, null);
            MenuButton(content, "save", () => { if (OnSaveRequested != null) OnSaveRequested(); ShowToast(Loc.T("saved")); ClosePanels(); }, null);
            MenuButton(content, "missions", OpenMissions, null);
            MenuButton(content, "inventory", OpenInventory, null);
            var lt = new Text[1]; MenuButton(content, null, CycleLanguage, lt); langTxt = lt[0];
            var qt = new Text[1]; MenuButton(content, null, CycleQuality, qt); qualTxt = qt[0];
            MenuButton(content, "quit", Quit, null);

            MakePanel("MissionsPanel", "missions", out content, out missionsPanel);
            missionsBody = BodyText(content);

            MakePanel("InventoryPanel", "inventory", out content, out inventoryPanel);
            invBody = BodyText(content);

            MakePanel("InfoPanel", "learn", out content, out infoPanel);
            infoTitle = UiKit.Label(content, "InfoTitle", "", 46, TextAnchor.UpperCenter, Gold);
            UiKit.Place(infoTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(980, 130));
            infoBody = UiKit.Label(content, "InfoBody", "", 36, TextAnchor.UpperLeft, Color.white);
            UiKit.Place(infoBody.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(980, 440));
        }

        Text BodyText(RectTransform content)
        {
            var t = UiKit.Label(content, "Body", "", 28, TextAnchor.UpperLeft, Color.white);
            UiKit.Stretch(t.rectTransform);
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        void BuildTitle()
        {
            var back = UiKit.Img(root, "TitlePanel", new Color(0.02f, 0.12f, 0.1f, 0.94f));
            UiKit.Stretch(back.rectTransform);
            titlePanel = back.gameObject;

            var t = UiKit.Label(back.transform, "Title", Loc.T("title"), 110, TextAnchor.MiddleCenter, Gold);
            UiKit.Place(t.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -90), new Vector2(1700, 160));
            var l1 = t.gameObject.AddComponent<LocLabel>(); l1.key = "title"; l1.Refresh();

            var sub = UiKit.Label(back.transform, "Subtitle", "Mauritius Island - Castel", 40, TextAnchor.MiddleCenter, Color.white);
            UiKit.Place(sub.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -260), new Vector2(1500, 70));

            var play = MenuButton(back.transform, "play", () => StartGame(false), null);
            UiKit.Place((RectTransform)play.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(560, 100));
            if (SaveSystem.Exists)
            {
                var cont = MenuButton(back.transform, "continue", () => StartGame(true), null);
                UiKit.Place((RectTransform)cont.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -70), new Vector2(560, 100));
            }

            for (int i = 0; i < 5; i++)
            {
                int idx = i;
                var lb = MenuButton(back.transform, null, () => SetLanguage((Language)idx), null);
                lb.GetComponentInChildren<Text>().text = Loc.EnglishNames[i];
                UiKit.Place((RectTransform)lb.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-640 + i * 320, 150), new Vector2(300, 80));
            }

            var hint = UiKit.Label(back.transform, "Hint", Loc.T("controls_hint"), 30, TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.8f));
            UiKit.Place(hint.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(1700, 80));
            var l2 = hint.gameObject.AddComponent<LocLabel>(); l2.key = "controls_hint"; l2.Refresh();

            TitleActive = true;
            GameState.Paused = true;
            Time.timeScale = 0f;
        }

        /// <summary>Used by PlayMode tests to skip the title screen.</summary>
        public void StartNewGameForTests() { StartGame(false); }

        void StartGame(bool cont)
        {
            titlePanel.SetActive(false);
            hud.SetActive(true);
            TitleActive = false;
            GameState.Paused = false;
            Time.timeScale = 1f;
            GameInput.ClearOneShots();
            if (OnStartGame != null) OnStartGame(cont);
        }

        // ------------------------------------------------------------------ Panel control
        bool AnyPanelOpen
        {
            get { return pausePanel.activeSelf || missionsPanel.activeSelf || inventoryPanel.activeSelf || infoPanel.activeSelf; }
        }

        void Open(GameObject panel)
        {
            pausePanel.SetActive(false); missionsPanel.SetActive(false); inventoryPanel.SetActive(false); infoPanel.SetActive(false);
            panel.SetActive(true);
            GameState.Paused = true;
            Time.timeScale = 0f;
            GameInput.TouchMove = Vector2.zero;
        }

        public void ClosePanels()
        {
            pausePanel.SetActive(false); missionsPanel.SetActive(false); inventoryPanel.SetActive(false); infoPanel.SetActive(false);
            if (!TitleActive) { GameState.Paused = false; Time.timeScale = 1f; }
            GameInput.ClearOneShots();
        }

        public void OpenMissions() { RefreshMissions(); Open(missionsPanel); }
        public void OpenInventory() { RefreshInventory(); Open(inventoryPanel); }

        public void ShowInfo(string title, string body)
        {
            infoTitle.text = title; infoBody.text = body;
            Open(infoPanel);
        }

        void RefreshMissions()
        {
            var sb = new StringBuilder();
            foreach (var d in Missions.All)
            {
                bool done = Missions.IsDone(d);
                sb.Append(done ? "[x] " : "[ ] ").Append(Loc.T(d.titleKey))
                  .Append("   (").Append((int)Missions.Progress(d)).Append('/').Append((int)d.target).Append(")\n")
                  .Append("      ").Append(Loc.T(d.descKey)).Append("\n\n");
            }
            missionsBody.text = sb.ToString();
        }

        void RefreshInventory()
        {
            var sb = new StringBuilder();
            sb.Append("Rs ").Append(Inventory.Money).Append("\n\n");
            if (GameState.Data.items.Count == 0) sb.Append(Loc.T("empty_items"));
            foreach (var it in GameState.Data.items) sb.Append("- ").Append(Loc.T(it)).Append('\n');
            invBody.text = sb.ToString();
        }

        void CycleLanguage() { SetLanguage((Language)(((int)Loc.Current + 1) % 5)); }

        void SetLanguage(Language l)
        {
            GameState.Data.language = (int)l;
            Loc.Set(l);
        }

        void CycleQuality()
        {
            GameState.Data.quality = (GameState.Data.quality + 1) % 3;
            QualityManager.Apply(GameState.Data.quality);
            RefreshDynamic();
        }

        void RefreshDynamic()
        {
            if (langTxt != null) langTxt.text = Loc.T("language") + ": " + Loc.EnglishNames[(int)Loc.Current];
            if (qualTxt != null) qualTxt.text = Loc.T("quality") + ": " + Loc.T(QualityManager.Keys[Mathf.Clamp(GameState.Data.quality, 0, 2)]);
            if (missionsPanel != null && missionsPanel.activeSelf) RefreshMissions();
            if (inventoryPanel != null && inventoryPanel.activeSelf) RefreshInventory();
        }

        void Quit()
        {
            if (OnSaveRequested != null) OnSaveRequested();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void ShowToast(string text) { toastTxt.text = text; toastTimer = 3.5f; }

        // ------------------------------------------------------------------ Per-frame
        void Update()
        {
            if (safe == null) return;
            ApplySafeArea();
            if (player == null || TitleActive) return;

            if (GameInput.ConsumeMenu())
            {
                if (AnyPanelOpen) ClosePanels(); else Open(pausePanel);
            }

            bool driving = player.Driving;
            if (footGroup.activeSelf == driving) footGroup.SetActive(!driving);
            if (driveGroup.activeSelf != driving) driveGroup.SetActive(driving);

            if (toastTimer > 0f)
            {
                toastTimer -= Time.unscaledDeltaTime;
                if (toastTimer <= 0f) toastTxt.text = "";
            }

            // Context button
            string label; bool active;
            if (driving) { label = Loc.T("exit_vehicle"); active = true; }
            else if (player.Nearest != null) { label = Loc.T(player.Nearest.PromptKey); active = true; }
            else { label = Loc.T("interact"); active = false; }
            interactTxt.text = label;
            interactImg.color = new Color(0.12f, 0.5f, 0.38f, active ? 0.9f : 0.3f);

            hudTimer -= Time.unscaledDeltaTime;
            if (hudTimer <= 0f)
            {
                hudTimer = 0.15f;
                var a = Missions.FirstActive();
                missionTxt.text = a != null
                    ? Loc.T(a.titleKey) + "  (" + (int)Missions.Progress(a) + "/" + (int)a.target + ")\n" + Loc.T(a.descKey)
                    : Loc.T("mission_complete");
                moneyTxt.text = "Rs " + Inventory.Money;
                driveTxt.text = driving && player.Vehicle != null
                    ? Mathf.RoundToInt(player.Vehicle.Kmh) + " km/h     " + Loc.T("fuel") + ": " + Mathf.RoundToInt(player.Vehicle.FuelNormalized * 100f) + "%"
                    : "";
            }
        }

        void ApplySafeArea()
        {
            Rect sa = Screen.safeArea;
            if (sa == lastSafe) return;
            lastSafe = sa;
            Vector2 mn = sa.position, mx = sa.position + sa.size;
            mn.x /= Screen.width; mn.y /= Screen.height; mx.x /= Screen.width; mx.y /= Screen.height;
            safe.anchorMin = mn; safe.anchorMax = mx;
            safe.offsetMin = Vector2.zero; safe.offsetMax = Vector2.zero;
        }
    }
}
