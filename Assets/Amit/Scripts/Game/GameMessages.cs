using UnityEngine;
using UnityEngine.UI;

namespace MansionGame
{
    /// <summary>
    /// Head-locked message panel (legacy UGUI, built at runtime) for intro, toasts and the end screens.
    /// The panel hangs 1.4 m ahead and slowly re-centres when you turn away from it.
    /// </summary>
    public sealed class GameMessages : MonoBehaviour
    {
        const float Distance = 1.4f;
        const float TitleHeight = 150f;
        const float LineHeight = 44f;
        const float RecentreAngle = 25f;
        const float Fade = 0.4f;

        static readonly Color Bg = new Color(0.098f, 0.086f, 0.098f, 0.75f);
        static readonly Color Ivory = new Color(0.875f, 0.784f, 0.627f, 1f);

        Transform panel;
        RectTransform panelRect;
        CanvasGroup group;
        Text titleText;
        Text bodyText;
        Transform head;
        Vector3 dir = Vector3.forward;
        bool dirInit;
        bool recentring;
        bool showing;
        float hold;
        float alpha;

        void Awake()
        {
            Build();
        }

        void Start()
        {
            var g = EscapeGame.Instance;
            if (g == null) return;
            g.StateChanged += OnState;
            g.PuzzleSolved += OnSolved;
            if (g.State == GameState.Intro)
                Show("The Ashcombe Parlour", "The candle on the mantel is all the time you have.\nFind the way out before it dies.", g.IntroSeconds);
        }

        void OnDestroy()
        {
            var g = EscapeGame.Instance;
            if (g != null)
            {
                g.StateChanged -= OnState;
                g.PuzzleSolved -= OnSolved;
            }
            if (panel != null) Destroy(panel.gameObject);
        }

        /// <summary>Shows a message. seconds &lt;= 0 keeps it up until Hide() or the next Show().</summary>
        public void Show(string title, string body, float seconds)
        {
            if (titleText == null) return;
            titleText.text = title ?? "";
            bodyText.text = body ?? "";
            // Grow the panel to fit the body (e.g. the leaderboard on the win screen).
            int lines = 1;
            foreach (char c in bodyText.text) if (c == '\n') lines++;
            if (panelRect != null)
                panelRect.sizeDelta = new Vector2(900f, TitleHeight + 20f + Mathf.Max(4, lines) * LineHeight);
            showing = true;
            hold = seconds > 0f ? seconds : float.PositiveInfinity;
        }

        public void Hide() { showing = false; }

        void OnState(GameState s)
        {
            var g = EscapeGame.Instance;
            if (g == null) return;
            if (s == GameState.Won)
            {
                string t = "Time: " + EscapeGame.FormatTime(g.Elapsed);
                if (g.NewBest) t += "  ·  New record!";
                var board = new System.Text.StringBuilder("\n\nFastest escapes\n");
                var top = g.TopTimes;
                for (int i = 0; i < top.Count; i++)
                    board.Append(i + 1).Append(".  ").Append(EscapeGame.FormatTime(top[i]))
                         .Append(i == g.LastRank ? "   ← you" : "").Append('\n');
                Show("You escaped", t + board + "\nPull a trigger to play again.", 0f);
            }
            else if (s == GameState.Lost)
            {
                Show("The candle is out", "The house keeps you.\nPull a trigger to try again.", 0f);
            }
            else if (s == GameState.Playing && showing && float.IsPositiveInfinity(hold) == false && hold > 0f)
            {
                // intro ends with the state change; let it fade now
                hold = Mathf.Min(hold, 0.01f);
            }
        }

        void OnSolved(int n)
        {
            var g = EscapeGame.Instance;
            if (g != null && (g.State == GameState.Won || g.State == GameState.Lost)) return;
            string line;
            switch (n)
            {
                case 1: line = "Something shifts behind the bookcase."; break;
                case 2: line = "A cupboard creaks open in the hidden room."; break;
                case 3: line = "The moonlight burns through the lock. The window bars are opening."; break;
                default: line = ""; break;
            }
            Show("A seal breaks… (" + n + "/" + EscapeGame.PuzzleCount + ")", line, 4.5f);
        }

        void Build()
        {
            var go = new GameObject("Game Message Panel");
            panel = go.transform;
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            group = go.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            group.alpha = 0f;
            var rt = go.GetComponent<RectTransform>();
            panel = rt; // the Canvas swapped the Transform for a RectTransform; keep the live one
            panelRect = rt;
            rt.sizeDelta = new Vector2(900f, 360f);
            rt.localScale = Vector3.one * 0.0012f;

            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var bgRt = (RectTransform)bgGo.transform;
            bgRt.SetParent(rt, false);
            Stretch(bgRt, 0f, 0f, 1f, 1f);
            var img = bgGo.GetComponent<Image>();
            img.color = Bg;
            img.raycastTarget = false;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText = MakeText("Title", rt, font, 58, FontStyle.Italic, new Vector4(0f, 1f, 1f, 1f));
            ((RectTransform)titleText.transform).offsetMin = new Vector2(30f, -TitleHeight);
            bodyText = MakeText("Body", rt, font, 36, FontStyle.Normal, new Vector4(0f, 0f, 1f, 1f));
            ((RectTransform)bodyText.transform).offsetMax = new Vector2(-30f, -TitleHeight);
            go.SetActive(true);
        }

        static Text MakeText(string name, RectTransform parent, Font font, int size, FontStyle style, Vector4 anchors)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            Stretch(rt, anchors.x, anchors.y, anchors.z, anchors.w);
            rt.offsetMin = new Vector2(30f, 10f);
            rt.offsetMax = new Vector2(-30f, -10f);
            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = Ivory;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.raycastTarget = false;
            return t;
        }

        static void Stretch(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            if (showing)
            {
                alpha = Mathf.Min(1f, alpha + dt / Fade);
                hold -= dt;
                if (hold <= 0f) showing = false;
            }
            else
            {
                alpha = Mathf.Max(0f, alpha - dt / Fade);
            }
            if (group != null) group.alpha = alpha;
            if (alpha <= 0f || panel == null) return;

            if (head == null)
            {
                var rig = FindAnyObjectByType<OVRCameraRig>();
                if (rig != null && rig.centerEyeAnchor != null) head = rig.centerEyeAnchor;
                else if (Camera.main != null) head = Camera.main.transform;
                if (head == null) return;
            }

            Vector3 fwd = head.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 1e-4f) fwd = dirInit ? dir : Vector3.forward;
            fwd.Normalize();
            if (!dirInit) { dir = fwd; dirInit = true; }

            float angle = Vector3.Angle(dir, fwd);
            if (angle > RecentreAngle) recentring = true;
            if (recentring)
            {
                dir = Vector3.Slerp(dir, fwd, 1f - Mathf.Exp(-3f * dt));
                if (angle < 3f) recentring = false;
            }

            // Pull the panel in front of any wall or furniture so the depth test never hides it.
            float dist = Distance;
            if (Physics.Raycast(head.position, dir, out var block, Distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                dist = Mathf.Max(0.35f, block.distance - 0.15f);
            Vector3 pos = head.position + dir * dist;
            panel.position = pos;
            panel.rotation = Quaternion.LookRotation(pos - head.position, Vector3.up);
        }
    }
}
