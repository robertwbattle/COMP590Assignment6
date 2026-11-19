using System.Collections.Generic;
using UnityEngine;

namespace MansionGame
{
    /// <summary>
    /// An object a <see cref="MansionHand"/> can pick up. Kinematic while held or resting in a <see cref="Socket"/>;
    /// dropped with no socket nearby it becomes a normal physics body carrying the (clamped) throw velocity.
    /// If it ever falls below <c>respawnBelowY</c> it is put back where it started.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public sealed class Grabbable : MonoBehaviour
    {
        static readonly List<Grabbable> all = new List<Grabbable>();

        /// <summary>Every enabled Grabbable in the loaded scenes.</summary>
        public static IReadOnlyList<Grabbable> All => all;

        [SerializeField, Tooltip("Identifier sockets and puzzles match against, e.g. \"Book_Red\" or \"MirrorPane\".")]
        private string id = "";
        [SerializeField, Range(0f, 1f), Tooltip("Pseudo-haptic weight (Rietzler et al. 2018): heavier objects trail and sag behind the hand.")]
        private float heaviness;
        [SerializeField, Tooltip("Whether a hand may pick this up. Puzzles toggle this at runtime.")]
        private bool canGrab = true;
        [SerializeField, Min(0f), Tooltip("Largest throw speed (m/s) applied on release.")]
        private float maxThrowSpeed = 4f;
        [SerializeField, Min(0f), Tooltip("Largest throw spin (rad/s) applied on release.")]
        private float maxThrowSpin = 12f;
        [SerializeField, Tooltip("Below this world height the object is returned to its start pose.")]
        private float respawnBelowY = -2f;

        Rigidbody body;
        Collider[] colliders;
        bool[] authoredTrigger;
        Transform startParent;
        Vector3 startPosition;
        Quaternion startRotation;
        bool startKinematic;

        public string Id { get => id; set => id = value; }
        public bool CanGrab { get => canGrab; set => canGrab = value; }
        public float Heaviness => heaviness;
        public bool IsHeld => Holder != null;
        public Socket CurrentSocket { get; private set; }
        public MansionHand Holder { get; private set; }
        public Rigidbody Body => body;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            colliders = GetComponentsInChildren<Collider>(true);
            authoredTrigger = new bool[colliders.Length];
            for (int i = 0; i < colliders.Length; i++)
                authoredTrigger[i] = colliders[i].isTrigger;
            startParent = transform.parent;
            startPosition = transform.position;
            startRotation = transform.rotation;
            startKinematic = body.isKinematic;
        }

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        void Update()
        {
            if (Holder == null && CurrentSocket == null && transform.position.y < respawnBelowY)
                Respawn();
        }

        /// <summary>Called by MansionHand when it picks this object up.</summary>
        public void OnGrabbed(MansionHand hand)
        {
            if (Holder != null && Holder != hand)
                Holder.Forget(this);
            Holder = hand;
            if (CurrentSocket != null)
                CurrentSocket.Release();
            MakeKinematic();
            // Held objects are triggers so they cannot shove the player capsule or knock things over.
            SetHeldColliders(true);
        }

        /// <summary>
        /// Called by MansionHand when it lets go. Snaps into the best nearby Socket if there is one,
        /// otherwise falls with the given (clamped) velocity.
        /// </summary>
        public void OnReleased(Vector3 velocity, Vector3 angularVelocity)
        {
            var hand = Holder;
            Holder = null;
            SetHeldColliders(false);

            var socket = Socket.FindBest(this, transform.position);
            if (socket != null && socket.Insert(this, hand))
                return;

            MakeDynamic();
            body.linearVelocity = Vector3.ClampMagnitude(velocity, maxThrowSpeed);
            body.angularVelocity = Vector3.ClampMagnitude(angularVelocity, maxThrowSpin);
        }

        /// <summary>Puts the object back at its start pose and authored physics state.</summary>
        public void Respawn()
        {
            if (Holder != null)
                DetachFromHolder();
            if (CurrentSocket != null)
                CurrentSocket.Release();
            MakeKinematic();
            transform.SetParent(startParent, true);
            transform.SetPositionAndRotation(startPosition, startRotation);
            if (!startKinematic) MakeDynamic();
        }

        // ---- Socket / hand bookkeeping (called by Socket and MansionHand) ----

        internal void AttachToSocket(Socket socket, Transform anchor)
        {
            CurrentSocket = socket;
            MakeKinematic();
            SetHeldColliders(false);
            transform.SetParent(anchor, true);
        }

        internal void DetachFromSocket(Socket socket)
        {
            if (CurrentSocket != socket)
                return;
            CurrentSocket = null;
            transform.SetParent(startParent, true);
            if (Holder == null)
                MakeDynamic();
        }

        /// <summary>Makes the current holder forget this object without any physics or socket logic.</summary>
        internal void DetachFromHolder()
        {
            var hand = Holder;
            Holder = null;
            SetHeldColliders(false);
            if (hand != null)
                hand.Forget(this);
        }

        void MakeKinematic()
        {
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            body.isKinematic = true;
            // Interpolation on a kinematic body overwrites the transform we drive by hand/socket, so turn it off.
            body.interpolation = RigidbodyInterpolation.None;
        }

        void MakeDynamic()
        {
            body.isKinematic = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.WakeUp();
        }

        void SetHeldColliders(bool held)
        {
            for (int i = 0; i < colliders.Length; i++)
                if (colliders[i] != null)
                    colliders[i].isTrigger = held || authoredTrigger[i];
        }
    }
}
