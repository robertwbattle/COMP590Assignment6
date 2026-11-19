using UnityEngine;
using UnityEngine.UI;

namespace MansionGame
{
    /// <summary>
    /// A small wrist readout on the left controller showing the time left and seals broken.
    /// Built at runtime with legacy UGUI.
    /// </summary>
    public sealed class PocketWatch : MonoBehaviour
    {
        [SerializeField] private Vector3 localOffset = new Vector3(0f, 0.03f, 0.06f);
        [Tooltip("Tilt (degrees about X) so the face turns up toward the eye.")]
        [SerializeField] private float tilt = 65f;

        static readonly Color Bg = new Color(0.098f, 0.086f, 0.098f, 0.75f);
        static readonly Color Ivory = new Color(0.875f, 0.784f, 0.627f, 1f);
        static readonly Color Red = new Color(0.85f, 0.25f, 0.15f, 1f);

        Canvas canvas;
        Text timeText;
        Text sealText;
        bool parented;
        int lastSecond = -1;
        int lastSeals = -1;
        bool lastRed;
        bool shown = true;

        void Awake()
        {
            Build();
            SetShown(false);
        }

        void OnDestroy()
        {
            if (canvas != null) Destroy(canvas.gameObject);
        }

        void Update()
        {
            if (!parented)
            {
                var rig = FindAnyObjectByType<OVRCameraRig>();
                if (rig == null || rig.leftControllerAnchor == null) return;
                var t = canvas.transform;
                t.SetParent(rig.leftControllerAnchor, false);
                t.localPosition = localOffset;
                t.localRotation = Quaternion.Euler(tilt, 0f, 0f);
                t.localScale = Vector3.one * 0.0006f;
                parented = true;
            }

            var g = EscapeGame.Instance;
            // Hide the watch when the left controller is asleep or untracked (its anchor pose would be stale).
            bool tracked = !OVRManager.isHmdPresent || OVRInput.GetControllerPositionTracked(OVRInput.Controller.LTouch);
            bool visible = tracked && g != null && (g.State == GameState.Intro || g.State == GameState.Playing);
            SetShown(visible);
            if (!visible) return;

            int sec = Mathf.CeilToInt(g.TimeRemaining);
            bool red = g.TimeRemaining < 60f;
            if (sec != lastSecond)
            {
                lastSecond = sec;
                timeText.text = EscapeGame.FormatTime(g.TimeRemaining);
            }
            if (red != lastRed || lastSeals < 0)
            {
                lastRed = red;
                timeText.color = red ? Red : Ivory;
            }
            if (g.PuzzlesSolved != lastSeals)
            {
                lastSeals = g.PuzzlesSolved;
                sealText.text = "seals " + lastSeals + "/" + EscapeGame.PuzzleCount;
            }
        }

        void SetShown(bool value)
        {
            if (shown == value || canvas == null) return;
            shown = value;
            canvas.enabled = value;
        }

        void Build()
        {
            var go = new GameObject("Pocket Watch");
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300f, 150f);
            rt.localScale = Vector3.one * 0.0006f;

            var bg = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var bgRt = (RectTransform)bg.transform;
            bgRt.SetParent(rt, false);
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
            var img = bg.GetComponent<Image>();
            img.color = Bg;
            img.raycastTarget = false;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            timeText = MakeText("Time", rt, font, 90, new Vector4(0f, 0.3f, 1f, 1f));
            sealText = MakeText("Seals", rt, font, 36, new Vector4(0f, 0f, 1f, 0.32f));
            timeText.fontStyle = FontStyle.Bold;
        }

        static Text MakeText(string name, RectTransform parent, Font font, int size, Vector4 a)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(a.x, a.y);
            rt.anchorMax = new Vector2(a.z, a.w);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.color = Ivory;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }
    }
}
