using System.Collections;
using UnityEngine;

namespace MansionGame
{
    /// <summary>
    /// The way out. Open() swings the iron bars aside (and hides the glass), then the player wins by reaching a
    /// hand into <c>reachZone</c>, or by leaning within <c>headWinDistance</c> of it and pressing a trigger.
    /// Opens on its own when puzzle 3 is solved; MoonSeal.onSolved can also call Open(). Open is idempotent.
    /// </summary>
    public sealed class WindowEscape : MonoBehaviour
    {
        [SerializeField, Tooltip("Hinge pivot whose child is the bars group.")]
        private Transform barsHinge;
        [SerializeField, Tooltip("Local yaw (degrees) added to the hinge's closed rotation when open.")]
        private float openYaw = -110f;
        [SerializeField, Min(0.01f)]
        private float openSeconds = 2.5f;
        [SerializeField, Tooltip("Optional glass or sash, hidden once the bars are open.")]
        private Transform glassOrSash;
        [SerializeField, Tooltip("Box (isTrigger) outside the window. Only its shape is used; keep it enabled or not.")]
        private BoxCollider reachZone;
        [SerializeField, Min(0f), Tooltip("Head this close (m) to the reach zone + a trigger press also wins.")]
        private float headWinDistance = 0.9f;
        [SerializeField, Tooltip("Open automatically when EscapeGame reports puzzle 3 solved.")]
        private bool openOnPuzzle3 = true;

        EscapeGame subscribed;
        Transform head;

        public bool IsOpening { get; private set; }
        public bool IsOpen { get; private set; }
        public bool Escaped { get; private set; }

        void Start()
        {
            subscribed = EscapeGame.Instance;
            if (subscribed != null)
            {
                subscribed.PuzzleSolved += OnPuzzleSolved;
                if (openOnPuzzle3 && subscribed.IsSolved(3))
                    Open();
            }
        }

        void OnDestroy()
        {
            if (subscribed != null)
                subscribed.PuzzleSolved -= OnPuzzleSolved;
        }

        void OnPuzzleSolved(int puzzle)
        {
            if (openOnPuzzle3 && puzzle == 3)
                Open();
        }

        /// <summary>Swings the bars open (once) and then arms the win check.</summary>
        public void Open()
        {
            if (IsOpening || IsOpen)
                return;
            IsOpening = true;
            var at = barsHinge != null ? barsHinge.position : transform.position;
            GameAudio.Play(Cue.BarsOpen, at);
            Haptics.PulseBoth(0.3f, openSeconds, 0.15f);
            if (isActiveAndEnabled)
                StartCoroutine(OpenRoutine());
            else
                FinishOpen();
        }

        public void DebugOpen() => Open();

        IEnumerator OpenRoutine()
        {
            if (barsHinge != null)
            {
                var closed = barsHinge.localRotation;
                var open = closed * Quaternion.Euler(0f, openYaw, 0f);
                for (float t = 0f; t < openSeconds; t += Time.deltaTime)
                {
                    barsHinge.localRotation = Quaternion.Slerp(closed, open, Mathf.SmoothStep(0f, 1f, t / openSeconds));
                    yield return null;
                }
                barsHinge.localRotation = open;
            }
            FinishOpen();
        }

        void FinishOpen()
        {
            if (barsHinge != null && !isActiveAndEnabled)
                barsHinge.localRotation *= Quaternion.Euler(0f, openYaw, 0f);
            if (glassOrSash != null)
                glassOrSash.gameObject.SetActive(false);
            IsOpening = false;
            IsOpen = true;
        }

        void Update()
        {
            if (!IsOpen || Escaped || reachZone == null)
                return;

            var hands = MansionHand.All;
            for (int i = 0; i < hands.Count; i++)
            {
                if (hands[i] != null && InZone(hands[i].transform.position))
                {
                    Escape();
                    return;
                }
            }

            if (head == null)
            {
                var cam = Camera.main;
                if (cam != null)
                    head = cam.transform;
            }
            if (head == null)
                return;

            bool trigger = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch)
                || OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
            if (trigger && (ClosestInZone(head.position) - head.position).sqrMagnitude <= headWinDistance * headWinDistance)
                Escape();
        }

        void Escape()
        {
            if (Escaped)
                return;
            var game = EscapeGame.Instance;
            if (game == null)
            {
                Escaped = true;
                return;
            }
            game.Win();
            // Win() is ignored outside Playing (e.g. still in the intro); keep checking until it takes.
            Escaped = game.State == GameState.Won;
        }

        // The reach zone as an oriented box, so it works whether or not the collider is enabled.
        bool InZone(Vector3 world)
        {
            var local = reachZone.transform.InverseTransformPoint(world) - reachZone.center;
            var half = reachZone.size * 0.5f;
            return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
        }

        Vector3 ClosestInZone(Vector3 world)
        {
            var local = reachZone.transform.InverseTransformPoint(world) - reachZone.center;
            var half = reachZone.size * 0.5f;
            local = new Vector3(Mathf.Clamp(local.x, -half.x, half.x), Mathf.Clamp(local.y, -half.y, half.y),
                Mathf.Clamp(local.z, -half.z, half.z));
            return reachZone.transform.TransformPoint(local + reachZone.center);
        }
    }
}
