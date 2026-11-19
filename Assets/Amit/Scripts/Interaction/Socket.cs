using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MansionGame
{
    /// <summary>
    /// A place a <see cref="Grabbable"/> snaps into when released nearby (a shelf slot, a mirror frame).
    /// The occupant's pivot is lerped onto <c>anchor</c> and parented to it. With <c>lockWhenFilled</c> the
    /// occupant can no longer be picked up.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Socket : MonoBehaviour
    {
        static readonly List<Socket> all = new List<Socket>();

        /// <summary>Every enabled Socket in the loaded scenes.</summary>
        public static IReadOnlyList<Socket> All => all;

        [SerializeField, Tooltip("Grabbable ids this socket takes. Empty accepts anything.")]
        private string[] acceptedIds = new string[0];
        [SerializeField, Tooltip("Where the occupant's pivot ends up. Defaults to this transform.")]
        private Transform anchor;
        [SerializeField, Min(0.01f), Tooltip("A released object whose pivot is this close (m) to the anchor snaps in.")]
        private float captureRadius = 0.18f;
        [SerializeField, Tooltip("When filled, the occupant can no longer be grabbed.")]
        private bool lockWhenFilled;
        [SerializeField, Min(0f), Tooltip("Seconds the snap-in lerp takes.")]
        private float snapSeconds = 0.12f;
        [SerializeField, Tooltip("Optional: inserted silently at Start.")]
        private Grabbable startOccupant;

        public UnityEvent<Grabbable> onFilled = new UnityEvent<Grabbable>();
        public UnityEvent onEmptied = new UnityEvent();

        Coroutine snapRoutine;

        public Grabbable Occupant { get; private set; }
        public bool IsFilled => Occupant != null;
        public Transform Anchor => anchor != null ? anchor : transform;
        public float CaptureRadius { get => captureRadius; set => captureRadius = Mathf.Max(0.01f, value); }
        public bool LockWhenFilled { get => lockWhenFilled; set => lockWhenFilled = value; }

        void OnEnable() => all.Add(this);

        void OnDisable()
        {
            all.Remove(this);
            FinishSnap();
        }

        void Start()
        {
            if (startOccupant != null && Occupant == null && Accepts(startOccupant))
                Place(startOccupant, null, false, false);
        }

        /// <summary>True if this socket is empty (or already holds g) and g's id is accepted.</summary>
        public bool Accepts(Grabbable g)
        {
            if (g == null || !isActiveAndEnabled)
                return false;
            if (Occupant != null && Occupant != g)
                return false;
            if (acceptedIds == null || acceptedIds.Length == 0)
                return true;
            for (int i = 0; i < acceptedIds.Length; i++)
                if (string.Equals(acceptedIds[i], g.Id, System.StringComparison.Ordinal))
                    return true;
            return false;
        }

        /// <summary>Snaps g in (thud and haptic tick). Returns false if it isn't accepted.</summary>
        public bool Insert(Grabbable g) => Insert(g, null);

        /// <summary>Snaps g in; <paramref name="by"/> is the hand that let go, if any, for the haptic tick.</summary>
        public bool Insert(Grabbable g, MansionHand by)
        {
            if (g == null)
                return false;
            if (Occupant == g)
                return true;
            if (!Accepts(g))
                return false;
            Place(g, by, true, true);
            return true;
        }

        /// <summary>Lets the occupant go (it stays where it is). Fires onEmptied.</summary>
        public void Release()
        {
            if (Occupant == null)
                return;
            FinishSnap();
            var g = Occupant;
            Occupant = null;
            g.DetachFromSocket(this);
            onEmptied.Invoke();
        }

        /// <summary>Locks the socket: from now on its occupant (current or future) cannot be grabbed.</summary>
        public void Lock()
        {
            lockWhenFilled = true;
            if (Occupant != null)
                Occupant.CanGrab = false;
        }

        /// <summary>Nearest enabled, empty socket that accepts g with its anchor within captureRadius of point.</summary>
        public static Socket FindBest(Grabbable g, Vector3 point)
        {
            Socket best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < all.Count; i++)
            {
                var s = all[i];
                if (s == null || s.Occupant != null || !s.Accepts(g))
                    continue;
                float sqr = (s.Anchor.position - point).sqrMagnitude;
                if (sqr <= s.captureRadius * s.captureRadius && sqr < bestSqr)
                {
                    best = s;
                    bestSqr = sqr;
                }
            }
            return best;
        }

        void Place(Grabbable g, MansionHand by, bool animate, bool feedback)
        {
            if (g.IsHeld)
                g.DetachFromHolder();
            if (g.CurrentSocket != null && g.CurrentSocket != this)
                g.CurrentSocket.Release();

            Occupant = g;
            var a = Anchor;
            g.AttachToSocket(this, a);
            if (lockWhenFilled)
                g.CanGrab = false;

            FinishSnap();
            if (animate && snapSeconds > 0f && isActiveAndEnabled)
                snapRoutine = StartCoroutine(Snap(g.transform));
            else
                g.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            if (feedback)
            {
                GameAudio.Play(Cue.BookThud, a.position);
                if (by != null)
                    Haptics.Pulse(by.Controller, 0.4f, 0.06f);
            }
            onFilled.Invoke(g);
        }

        IEnumerator Snap(Transform t)
        {
            t.GetLocalPositionAndRotation(out var fromPos, out var fromRot);
            for (float time = 0f; time < snapSeconds; time += Time.deltaTime)
            {
                if (t == null || Occupant == null || t != Occupant.transform)
                {
                    snapRoutine = null;
                    yield break;
                }
                float k = Mathf.SmoothStep(0f, 1f, time / snapSeconds);
                t.SetLocalPositionAndRotation(Vector3.Lerp(fromPos, Vector3.zero, k),
                    Quaternion.Slerp(fromRot, Quaternion.identity, k));
                yield return null;
            }
            snapRoutine = null;
            if (t != null && Occupant != null && t == Occupant.transform)
                t.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        void FinishSnap()
        {
            if (snapRoutine == null)
                return;
            StopCoroutine(snapRoutine);
            snapRoutine = null;
            if (Occupant != null)
                Occupant.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.9f, 0.7f, 0.3f, 0.6f);
            Gizmos.DrawWireSphere(Anchor.position, captureRadius);
        }
    }
}
