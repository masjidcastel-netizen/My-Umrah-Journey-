using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MyUmrah
{
    /// <summary>Small helpers for building uGUI in code (no web tech; all native Unity UI).</summary>
    public static class UiKit
    {
        static Font font;
        static Sprite circle;

        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (circle != null) return circle;
                const int n = 128;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                float c = (n - 1) * 0.5f;
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                        float a = Mathf.Clamp01((1f - d) * 20f);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                tex.Apply();
                circle = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
                return circle;
            }
        }

        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static void Place(RectTransform r, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            r.anchorMin = anchor; r.anchorMax = anchor; r.pivot = pivot;
            r.anchoredPosition = pos; r.sizeDelta = size;
        }

        public static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        }

        public static Image Img(Transform parent, string name, Color c, Sprite s = null, bool raycast = true)
        {
            var r = Node(name, parent);
            var img = r.gameObject.AddComponent<Image>();
            img.color = c; img.sprite = s; img.raycastTarget = raycast;
            return img;
        }

        public static Text Label(Transform parent, string name, string text, int size, TextAnchor anchor, Color color)
        {
            var r = Node(name, parent);
            var t = r.gameObject.AddComponent<Text>();
            t.font = Font; t.text = text; t.fontSize = size; t.alignment = anchor; t.color = color;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            return t;
        }
    }

    /// <summary>Text bound to a localisation key; refreshes when the language changes; flips alignment for RTL.</summary>
    public class LocLabel : MonoBehaviour
    {
        public string key;
        public string format; // optional prefix e.g. "{0}: "
        Text text;
        TextAnchor baseAnchor;

        void Awake() { text = GetComponent<Text>(); baseAnchor = text.alignment; }
        void OnEnable() { Loc.Changed += Refresh; Refresh(); }
        void OnDisable() { Loc.Changed -= Refresh; }

        public void Refresh()
        {
            if (text == null || string.IsNullOrEmpty(key)) return;
            text.text = Loc.T(key);
            if (Loc.IsRtl)
            {
                if (baseAnchor == TextAnchor.UpperLeft) text.alignment = TextAnchor.UpperRight;
                else if (baseAnchor == TextAnchor.MiddleLeft) text.alignment = TextAnchor.MiddleRight;
                else if (baseAnchor == TextAnchor.LowerLeft) text.alignment = TextAnchor.LowerRight;
                else text.alignment = baseAnchor;
            }
            else text.alignment = baseAnchor;
        }
    }

    /// <summary>Press-and-hold or tap button for touch controls.</summary>
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public System.Action OnDown, OnUp;
        public bool Held { get; private set; }
        public void OnPointerDown(PointerEventData e) { Held = true; if (OnDown != null) OnDown(); }
        public void OnPointerUp(PointerEventData e) { Release(); }
        public void OnPointerExit(PointerEventData e) { Release(); }
        void OnDisable() { Release(); }
        void Release() { if (!Held) return; Held = false; if (OnUp != null) OnUp(); }
    }

    /// <summary>Floating virtual joystick covering a screen zone (left side).</summary>
    public class JoystickZone : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform zone, baseRt, knobRt;
        public float radius = 120f;
        Vector2 defaultPos;
        int pointer = int.MinValue;

        public void Setup() { defaultPos = baseRt.anchoredPosition; SetAlpha(0.35f); }

        void SetAlpha(float a)
        {
            baseRt.GetComponent<Image>().color = new Color(1f, 1f, 1f, a * 0.5f);
            knobRt.GetComponent<Image>().color = new Color(1f, 1f, 1f, a);
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (pointer != int.MinValue) return;
            pointer = e.pointerId;
            Vector2 lp;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(zone, e.position, null, out lp);
            baseRt.anchoredPosition = lp;
            knobRt.anchoredPosition = Vector2.zero;
            SetAlpha(0.8f);
            Drag(e);
        }

        public void OnDrag(PointerEventData e) { if (e.pointerId == pointer) Drag(e); }

        void Drag(PointerEventData e)
        {
            Vector2 lp;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(zone, e.position, null, out lp);
            Vector2 v = Vector2.ClampMagnitude((lp - baseRt.anchoredPosition) / radius, 1f);
            knobRt.anchoredPosition = v * radius;
            GameInput.TouchMove = v.magnitude < 0.1f ? Vector2.zero : v;
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId != pointer) return;
            pointer = int.MinValue;
            GameInput.TouchMove = Vector2.zero;
            baseRt.anchoredPosition = defaultPos;
            knobRt.anchoredPosition = Vector2.zero;
            SetAlpha(0.35f);
        }

        void OnDisable() { GameInput.TouchMove = Vector2.zero; pointer = int.MinValue; }
    }

    /// <summary>Drag anywhere in the zone to rotate the camera.</summary>
    public class LookZone : MonoBehaviour, IDragHandler
    {
        public void OnDrag(PointerEventData e) { GameInput.AddLook(e.delta); }
    }
}
