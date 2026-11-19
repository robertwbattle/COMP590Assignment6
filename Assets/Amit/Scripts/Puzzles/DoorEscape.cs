using System.Collections;
using UnityEngine;

namespace MansionGame
{
    /// <summary>
    /// The way out. The front door starts barred; when the third seal breaks, the iron bars clatter to the floor one by
    /// one, the door swings outward, and walking out into the moonlit garden (head inside <see cref="winZone"/>) wins.
    /// </summary>
    public sealed class DoorEscape : MonoBehaviour
    {
        [SerializeField, Tooltip("Iron bars across the door. Each falls to the floor when the door unlocks.")]
        private Transform[] bars = new Transform[0];
        [SerializeField, Tooltip("Pivot on the door's hinge edge; the door leaf is its child.")]
        private Transform doorHinge;
        [SerializeField, Tooltip("Yaw (degrees) added to the hinge when open. Negative swings the door outward.")]
        private float doorOpenYaw = -100f;
        [SerializeField, Min(0.1f)] private float doorSeconds = 2.2f;
        [SerializeField, Tooltip("Floor height the bars land on.")]
        private float floorY = 0.03f;
        [SerializeField, Tooltip("Box out in the garden. When the player's head is inside it, they have escaped.")]
        private BoxCollider winZone;
        [SerializeField, Tooltip("Open automatically when EscapeGame reports puzzle 3 solved.")]
        private bool openOnPuzzle3 = true;

        public bool IsOpening { get; private set; }
        public bool IsOpen { get; private set; }
        public bool Escaped { get; private set; }

        EscapeGame subscribed;
        Transform head;

        void OnEnable() => TrySubscribe();

        void OnDisable()
        {
            if (subscribed != null) subscribed.PuzzleSolved -= OnPuzzleSolved;
            subscribed = null;
        }

        void TrySubscribe()
        {
            if (!openOnPuzzle3 || subscribed != null || EscapeGame.Instance == null) return;
            subscribed = EscapeGame.Instance;
            subscribed.PuzzleSolved += OnPuzzleSolved;
        }

        void OnPuzzleSolved(int n)
        {
            if (n == 3) Open();
        }

        /// <summary>Drops the bars and swings the door open. Safe to call more than once.</summary>
        public void Open()
        {
            if (IsOpening || IsOpen) return;
            IsOpening = true;
            StartCoroutine(OpenRoutine());
        }

        /// <summary>Opens straight away (testing).</summary>
        public void DebugOpen() => Open();

        IEnumerator OpenRoutine()
        {
            var doorPos = doorHinge != null ? doorHinge.position + Vector3.up * 1.2f : transform.position;
            GameAudio.Play(Cue.SealUnlock, doorPos);
            Haptics.PulseBoth(0.35f, 0.2f);
            yield return new WaitForSeconds(0.35f);

            // Bars fall one after another, top first, landing scattered on the hall floor.
            var order = (Transform[])bars.Clone();
            System.Array.Sort(order, (a, b) => (b != null ? b.position.y : 0f).CompareTo(a != null ? a.position.y : 0f));
            for (int i = 0; i < order.Length; i++)
            {
                if (order[i] != null) StartCoroutine(Fall(order[i], i));
                yield return new WaitForSeconds(0.12f);
            }
            yield return new WaitForSeconds(0.7f);

            GameAudio.Play(Cue.BarsOpen, doorPos);
            Haptics.PulseBoth(0.25f, 0.8f, 0.2f);
            if (doorHinge != null)
            {
                var closed = doorHinge.localRotation;
                var open = closed * Quaternion.Euler(0f, doorOpenYaw, 0f);
                for (float t = 0f; t < doorSeconds; t += Time.deltaTime)
                {
                    doorHinge.localRotation = Quaternion.Slerp(closed, open, Mathf.SmoothStep(0f, 1f, t / doorSeconds));
                    yield return null;
                }
                doorHinge.localRotation = open;
            }
            IsOpening = false;
            IsOpen = true;
        }

        IEnumerator Fall(Transform bar, int index)
        {
            var startPos = bar.position;
            var startRot = bar.rotation;
            // Land a little into the hall (+Z from the door), lying flat at a random angle.
            var rng = new System.Random(index * 7919 + 17);
            float spread = (float)rng.NextDouble();
            var endPos = new Vector3(startPos.x + (spread - 0.5f) * 0.5f, floorY + 0.025f * (index % 2), startPos.z + 0.25f + spread * 0.45f);
            float yaw = (float)rng.NextDouble() * 50f - 25f;
            // Upright bars tip over onto the floor; horizontal bars just drop flat.
            var endRot = bar.lossyScale.y > bar.lossyScale.x
                ? Quaternion.Euler(90f, yaw, 0f)
                : Quaternion.Euler(0f, yaw, 0f);
            const float duration = 0.45f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                bar.SetPositionAndRotation(Vector3.Lerp(startPos, endPos, k * k), Quaternion.Slerp(startRot, endRot, k));
                yield return null;
            }
            bar.SetPositionAndRotation(endPos, endRot);
            GameAudio.Play(Cue.BookThud, endPos, 0.8f);
        }

        void Update()
        {
            if (subscribed == null) TrySubscribe();
            if (!IsOpen || Escaped || winZone == null) return;
            if (head == null)
            {
                var cam = Camera.main;
                if (cam == null) return;
                head = cam.transform;
            }
            if (!winZone.bounds.Contains(head.position)) return;
            var g = EscapeGame.Instance;
            if (g == null) return;
            g.Win(); // ignored until the intro is over; retried next frame
            if (g.State == GameState.Won) Escaped = true;
        }
    }
}
