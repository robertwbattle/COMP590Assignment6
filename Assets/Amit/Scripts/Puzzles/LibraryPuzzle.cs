using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace MansionGame
{
    /// <summary>
    /// Puzzle 2 (library): three books go into three shelf slots. When every slot is filled, the right order locks
    /// the books, marks puzzle 2 solved and swings the cupboard doors open to reveal the mirror pane; the wrong
    /// order groans and buzzes both hands once, then waits until a slot is emptied before judging again.
    /// </summary>
    public sealed class LibraryPuzzle : MonoBehaviour
    {
        [Header("Shelf")]
        [SerializeField, Tooltip("Book slots, left to right.")]
        private Socket[] slots = new Socket[0];
        [SerializeField, Tooltip("Book ids that solve the puzzle, left to right.")]
        private string[] correctOrder = { "Book_Red", "Book_Slate", "Book_Green" };

        [Header("Cupboard")]
        [SerializeField, Tooltip("Door transforms, pivot on the hinge edge.")]
        private Transform[] doors = new Transform[0];
        [SerializeField, Tooltip("Open angle per door, about the door's local Y (e.g. -100 and +100).")]
        private float[] doorOpenYaw = { -100f, 100f };
        [SerializeField, Tooltip("If on, doors rotate to these local euler angles instead of using doorOpenYaw.")]
        private bool useDoorOpenEuler;
        [SerializeField, Tooltip("Local euler targets per door, used when useDoorOpenEuler is on.")]
        private Vector3[] doorOpenEuler = new Vector3[0];
        [SerializeField, Min(0.01f)]
        private float doorSeconds = 1.2f;
        [SerializeField, Tooltip("The mirror pane behind the doors. Not grabbable until solved.")]
        private Grabbable reward;

        public UnityEvent onSolved = new UnityEvent();
        public UnityEvent onWrongOrder = new UnityEvent();

        bool armed = true;
        bool suppress;
        Quaternion[] doorClosed;

        public bool Solved { get; private set; }

        void Awake()
        {
            doorClosed = new Quaternion[doors.Length];
            for (int i = 0; i < doors.Length; i++)
                doorClosed[i] = doors[i] != null ? doors[i].localRotation : Quaternion.identity;
        }

        void OnEnable()
        {
            foreach (var s in slots)
            {
                if (s == null) continue;
                s.onFilled.AddListener(OnSlotFilled);
                s.onEmptied.AddListener(OnSlotEmptied);
            }
        }

        void OnDisable()
        {
            foreach (var s in slots)
            {
                if (s == null) continue;
                s.onFilled.RemoveListener(OnSlotFilled);
                s.onEmptied.RemoveListener(OnSlotEmptied);
            }
        }

        void Start()
        {
            if (reward != null && !Solved)
                reward.CanGrab = false;
        }

        void OnSlotFilled(Grabbable _)
        {
            if (Solved || suppress || !armed)
                return;
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] == null || !slots[i].IsFilled)
                    return;

            if (IsCorrect())
            {
                Solve();
                return;
            }

            armed = false;
            GameAudio.Play(Cue.WrongOrder, ShelfCentre());
            Haptics.PulseBoth(0.6f, 0.25f);
            onWrongOrder.Invoke();
        }

        void OnSlotEmptied()
        {
            if (!Solved)
                armed = true;
        }

        bool IsCorrect()
        {
            if (correctOrder == null || correctOrder.Length < slots.Length)
                return false;
            for (int i = 0; i < slots.Length; i++)
            {
                var occ = slots[i] != null ? slots[i].Occupant : null;
                if (occ == null || !string.Equals(occ.Id, correctOrder[i], System.StringComparison.Ordinal))
                    return false;
            }
            return true;
        }

        void Solve()
        {
            if (Solved)
                return;
            Solved = true;
            foreach (var s in slots)
                if (s != null)
                    s.Lock();

            EscapeGame.Instance?.MarkSolved(2);
            GameAudio.Play(Cue.CupboardUnlock, DoorsCentre());
            if (doors.Length > 0 && isActiveAndEnabled)
                StartCoroutine(SwingDoors());
            if (reward != null)
                reward.CanGrab = true;
            onSolved.Invoke();
        }

        IEnumerator SwingDoors()
        {
            var targets = new Quaternion[doors.Length];
            for (int i = 0; i < doors.Length; i++)
            {
                if (useDoorOpenEuler && doorOpenEuler != null && i < doorOpenEuler.Length)
                    targets[i] = Quaternion.Euler(doorOpenEuler[i]);
                else
                {
                    float yaw = doorOpenYaw != null && i < doorOpenYaw.Length ? doorOpenYaw[i] : 0f;
                    targets[i] = doorClosed[i] * Quaternion.Euler(0f, yaw, 0f);
                }
            }

            for (float t = 0f; t < doorSeconds; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / doorSeconds);
                for (int i = 0; i < doors.Length; i++)
                    if (doors[i] != null)
                        doors[i].localRotation = Quaternion.Slerp(doorClosed[i], targets[i], k);
                yield return null;
            }
            for (int i = 0; i < doors.Length; i++)
                if (doors[i] != null)
                    doors[i].localRotation = targets[i];
        }

        Vector3 ShelfCentre()
        {
            Vector3 sum = Vector3.zero;
            int n = 0;
            foreach (var s in slots)
                if (s != null) { sum += s.Anchor.position; n++; }
            return n > 0 ? sum / n : transform.position;
        }

        Vector3 DoorsCentre()
        {
            Vector3 sum = Vector3.zero;
            int n = 0;
            foreach (var d in doors)
                if (d != null) { sum += d.position; n++; }
            return n > 0 ? sum / n : transform.position;
        }

        /// <summary>Puts the right books in the right slots (when they can be found) and solves.</summary>
        public void DebugSolve()
        {
            if (Solved)
                return;
            suppress = true;
            foreach (var s in slots)
                if (s != null)
                    s.Release();
            for (int i = 0; i < slots.Length && correctOrder != null && i < correctOrder.Length; i++)
            {
                var book = FindBook(correctOrder[i]);
                if (slots[i] != null && book != null)
                    slots[i].Insert(book);
            }
            suppress = false;
            Solve();
        }

        static Grabbable FindBook(string id)
        {
            var all = Grabbable.All;
            for (int i = 0; i < all.Count; i++)
                if (all[i] != null && string.Equals(all[i].Id, id, System.StringComparison.Ordinal))
                    return all[i];
            return null;
        }
    }
}
