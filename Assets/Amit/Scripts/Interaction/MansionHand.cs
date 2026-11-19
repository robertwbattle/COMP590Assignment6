using System.Collections.Generic;
using UnityEngine;

namespace MansionGame
{
    /// <summary>
    /// Grab and turn for one Touch controller. Lives on "Left Candle Pointer" / "Right Candle Pointer"
    /// (transform.forward is the aim ray). Grip grabs: first a Grabbable touching the hand, then a Turnable touching
    /// the hand, then whatever the ray points at (Grabbables fly to the hand, Turnables start turning).
    /// The index trigger also grabs, but only while the ray is on a Grabbable or Turnable, so it never competes
    /// with the candle pointer. Does nothing while the controller is not tracked.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MansionHand : MonoBehaviour
    {
        static readonly List<MansionHand> all = new List<MansionHand>();

        /// <summary>Every enabled hand (used by ArmSwingLocomotion to check nothing is held).</summary>
        public static IReadOnlyList<MansionHand> All => all;

        const int HistoryLength = 6;
        const float RayHoldDistance = 0.12f;

        [SerializeField, Tooltip("LTouch on the left pointer, RTouch on the right one.")]
        private OVRInput.Controller controller = OVRInput.Controller.RTouch;
        [SerializeField, Min(0.01f), Tooltip("Radius (m) of the touch-grab sphere around the hand.")]
        private float grabRadius = 0.1f;
        [SerializeField, Min(0.1f), Tooltip("Length (m) of the grab ray. Matches the candle pointer.")]
        private float rayReach = 2.25f;
        [SerializeField, Tooltip("Layers the grab sphere and ray can hit.")]
        private LayerMask mask = Physics.DefaultRaycastLayers;
        [SerializeField, Min(0f), Tooltip("Seconds a ray-grabbed object takes to fly to the hand.")]
        private float pullSeconds = 0.15f;

        readonly Collider[] overlapBuffer = new Collider[16];
        readonly RaycastHit[] rayBuffer = new RaycastHit[32];
        readonly Vector3[] posHistory = new Vector3[HistoryLength];
        readonly Quaternion[] rotHistory = new Quaternion[HistoryLength];
        readonly float[] timeHistory = new float[HistoryLength];
        int historyCount, historyHead;

        Grabbable held;
        Vector3 holdLocalPos;          // object pivot in hand space
        Quaternion holdLocalRot;       // object rotation in hand space
        bool pulling;
        float pullStart;
        Vector3 pullFromPos;
        Quaternion pullFromRot;

        Turnable turning;
        OVRInput.Button activeButton;
        bool scriptHold;               // grabbed by DebugGrab: ignore the grip state until DebugRelease

        Component rayTarget;           // Grabbable or Turnable under the ray this frame
        bool rayOnCandle;

        public OVRInput.Controller Controller => controller;
        /// <summary>The Grabbable or Turnable that a grab press would take right now (null if none).</summary>
        public Component HoverTarget { get; private set; }
        public Grabbable Held => held;
        public Turnable Turning => turning;

        void OnEnable()
        {
            all.Add(this);
            // OVRCameraRig refreshes the anchors again just before rendering; follow then too so held objects don't lag.
            Application.onBeforeRender += Follow;
        }

        void OnDisable()
        {
            all.Remove(this);
            Application.onBeforeRender -= Follow;
            // Let go quietly (no socket snap, sounds or puzzle events) because this usually runs during teardown.
            scriptHold = false;
            pulling = false;
            if (turning != null) { turning.EndTurn(); turning = null; }
            if (held != null) { var g = held; held = null; if (g.Holder == this) g.DetachFromHolder(); }
            HoverTarget = null;
        }

        bool IsTracked()
        {
            return OVRManager.isHmdPresent
                && OVRManager.hasInputFocus
                && OVRInput.IsControllerConnected(controller)
                && OVRInput.GetControllerPositionTracked(controller)
                && OVRInput.GetControllerOrientationTracked(controller);
        }

        void Update()
        {
            if (!IsTracked())
            {
                HoverTarget = null;
                return;
            }

            if (held != null || turning != null)
            {
                HoverTarget = null;
                if (!scriptHold
                    && (!OVRInput.Get(activeButton, controller) || OVRInput.GetUp(activeButton, controller)))
                    ReleaseFromInput();
                return;
            }

            ScanRay();
            Component near = FindNearGrabbable();
            if (near == null)
                near = FindNearTurnable();
            HoverTarget = near != null ? near : rayTarget;

            if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, controller))
            {
                if (near != null)
                    StartInteraction(near, false, OVRInput.Button.PrimaryHandTrigger);
                else if (rayTarget != null)
                    StartInteraction(rayTarget, true, OVRInput.Button.PrimaryHandTrigger);
            }
            else if (rayTarget != null && !rayOnCandle
                     && OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, controller))
            {
                StartInteraction(rayTarget, true, OVRInput.Button.PrimaryIndexTrigger);
            }
        }

        void LateUpdate()
        {
            RecordHistory();
            FollowCore(true);
        }

        void Follow() => FollowCore(false);

        // Pseudo-haptic weight state: the displayed object trails the real hand by this offset.
        Grabbable weightTarget;
        Vector3 weightOffset;
        Vector3 lastDisplayed;
        const float MaxWeightOffset = 0.12f;

        void FollowCore(bool advance)
        {
            if (turning != null)
                turning.UpdateTurn(transform);

            if (held == null)
                return;
            if (held.Holder != this)
            {
                held = null;
                return;
            }

            var targetRot = transform.rotation * holdLocalRot;
            var targetPos = transform.position + transform.rotation * holdLocalPos;
            if (pulling)
            {
                float k = pullSeconds > 0f ? Mathf.Clamp01((Time.time - pullStart) / pullSeconds) : 1f;
                k = Mathf.SmoothStep(0f, 1f, k);
                targetPos = Vector3.Lerp(pullFromPos, targetPos, k);
                targetRot = Quaternion.Slerp(pullFromRot, targetRot, k);
                if (k >= 1f)
                    pulling = false;
            }
            if (weightTarget != held)
            {
                weightTarget = held;
                weightOffset = Vector3.zero;
                lastDisplayed = held.transform.position;
            }
            if (!pulling && held.Heaviness > 0f)
            {
                // Heavier objects lag and sag behind the hand (perceivable tracking offset), advanced once per frame.
                if (advance)
                {
                    float rate = Mathf.Lerp(25f, 5f, held.Heaviness);
                    var sagged = targetPos + Vector3.down * (0.06f * held.Heaviness);
                    var smoothed = Vector3.Lerp(lastDisplayed, sagged, 1f - Mathf.Exp(-rate * Time.deltaTime));
                    weightOffset = Vector3.ClampMagnitude(smoothed - targetPos, MaxWeightOffset);
                }
                targetPos += weightOffset;
            }
            else
            {
                weightOffset = Vector3.zero;
            }
            lastDisplayed = targetPos;
            held.transform.SetPositionAndRotation(targetPos, targetRot);
        }

        // ---------------- Targeting ----------------

        void ScanRay()
        {
            rayTarget = null;
            rayOnCandle = false;
            if (!FirstRayHit(out var hit))
                return;
            if (hit.collider.GetComponentInParent<Puzzle1_1>() != null)
            {
                rayOnCandle = true;
                return;
            }
            var g = hit.collider.GetComponentInParent<Grabbable>();
            if (IsGrabbableForMe(g))
            {
                rayTarget = g;
                return;
            }
            var t = hit.collider.GetComponentInParent<Turnable>();
            if (t != null && t.CanTurn && !t.IsTurning)
                rayTarget = t;
        }

        bool FirstRayHit(out RaycastHit best)
        {
            best = default;
            int n = Physics.RaycastNonAlloc(transform.position, transform.forward, rayBuffer, rayReach, mask,
                QueryTriggerInteraction.Ignore);
            float bestDist = float.MaxValue;
            bool found = false;
            for (int i = 0; i < n; i++)
            {
                var h = rayBuffer[i];
                if (h.collider == null || h.collider is CharacterController || h.distance >= bestDist)
                    continue;
                best = h;
                bestDist = h.distance;
                found = true;
            }
            return found;
        }

        Grabbable FindNearGrabbable()
        {
            var p = transform.position;
            int n = Physics.OverlapSphereNonAlloc(p, grabRadius, overlapBuffer, mask, QueryTriggerInteraction.Collide);
            Grabbable best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var c = overlapBuffer[i];
                if (c == null || c is CharacterController)
                    continue;
                var g = c.GetComponentInParent<Grabbable>();
                if (!IsGrabbableForMe(g))
                    continue;
                float sqr = (ClosestPoint(c, p) - p).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    best = g;
                    bestSqr = sqr;
                }
            }
            return best;
        }

        Turnable FindNearTurnable()
        {
            var p = transform.position;
            int n = Physics.OverlapSphereNonAlloc(p, grabRadius, overlapBuffer, mask, QueryTriggerInteraction.Ignore);
            Turnable best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var c = overlapBuffer[i];
                if (c == null || c is CharacterController)
                    continue;
                var t = c.GetComponentInParent<Turnable>();
                if (t == null || !t.CanTurn || t.IsTurning)
                    continue;
                float sqr = (ClosestPoint(c, p) - p).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    best = t;
                    bestSqr = sqr;
                }
            }
            return best;
        }

        bool IsGrabbableForMe(Grabbable g) =>
            g != null && g.isActiveAndEnabled && g.CanGrab && (g.Holder == null || g.Holder == this);

        static Vector3 ClosestPoint(Collider c, Vector3 p)
        {
            bool supported = c is BoxCollider || c is SphereCollider || c is CapsuleCollider
                || (c is MeshCollider mc && mc.convex);
            return supported ? c.ClosestPoint(p) : c.bounds.ClosestPoint(p);
        }

        // ---------------- Grab / turn ----------------

        void StartInteraction(Component target, bool fromRay, OVRInput.Button button)
        {
            if (target is Grabbable g)
            {
                if (BeginHold(g, fromRay, fromRay && pullSeconds > 0f))
                    activeButton = button;
            }
            else if (target is Turnable t)
            {
                if (t.BeginTurn(transform))
                {
                    turning = t;
                    activeButton = button;
                    Haptics.Pulse(controller, 0.2f, 0.04f);
                }
            }
        }

        bool BeginHold(Grabbable g, bool centreOnRay, bool pull)
        {
            if (!IsGrabbableForMe(g))
                return false;
            ReleaseCurrent(Vector3.zero, Vector3.zero);
            scriptHold = false;

            var handRot = transform.rotation;
            var invHand = Quaternion.Inverse(handRot);
            var objT = g.transform;
            holdLocalRot = invHand * objT.rotation;
            if (centreOnRay)
            {
                // Put the object's visual centre a little way along the ray, keeping its orientation.
                var centreOffset = invHand * (WorldCentre(g) - objT.position);
                holdLocalPos = new Vector3(0f, 0f, RayHoldDistance) - centreOffset;
            }
            else
            {
                holdLocalPos = invHand * (objT.position - transform.position);
            }

            pulling = pull;
            pullStart = Time.time;
            pullFromPos = objT.position;
            pullFromRot = objT.rotation;

            held = g;
            g.OnGrabbed(this);
            historyCount = 0;
            Haptics.Pulse(controller, 0.25f, 0.05f);
            GameAudio.Play(Cue.Grab, objT.position);
            return true;
        }

        static Vector3 WorldCentre(Grabbable g)
        {
            var renderers = g.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return g.transform.position;
            var b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);
            return b.center;
        }

        void ReleaseFromInput()
        {
            EstimateVelocity(out var v, out var w);
            ReleaseCurrent(v, w);
        }

        void ReleaseCurrent(Vector3 velocity, Vector3 angularVelocity)
        {
            scriptHold = false;
            if (turning != null)
            {
                turning.EndTurn();
                turning = null;
            }
            if (held != null)
            {
                var g = held;
                held = null;
                pulling = false;
                if (g.Holder == this)
                    g.OnReleased(velocity, angularVelocity);
            }
        }

        /// <summary>Called by Grabbable when something else (a socket, another hand) takes the object away.</summary>
        internal void Forget(Grabbable g)
        {
            if (held == g)
            {
                held = null;
                pulling = false;
                scriptHold = false;
            }
        }

        void RecordHistory()
        {
            posHistory[historyHead] = transform.position;
            rotHistory[historyHead] = transform.rotation;
            timeHistory[historyHead] = Time.time;
            historyHead = (historyHead + 1) % HistoryLength;
            if (historyCount < HistoryLength)
                historyCount++;
        }

        void EstimateVelocity(out Vector3 velocity, out Vector3 angularVelocity)
        {
            velocity = Vector3.zero;
            angularVelocity = Vector3.zero;
            if (historyCount < 2)
                return;
            int newest = (historyHead - 1 + HistoryLength) % HistoryLength;
            int oldest = (historyHead - historyCount + HistoryLength) % HistoryLength;
            float dt = timeHistory[newest] - timeHistory[oldest];
            if (dt <= 1e-4f)
                return;
            velocity = (posHistory[newest] - posHistory[oldest]) / dt;
            var delta = rotHistory[newest] * Quaternion.Inverse(rotHistory[oldest]);
            delta.ToAngleAxis(out float angle, out var axis);
            if (angle > 180f)
                angle -= 360f;
            if (float.IsFinite(axis.x) && float.IsFinite(axis.y) && float.IsFinite(axis.z))
                angularVelocity = axis * (angle * Mathf.Deg2Rad / dt);
        }

        // ---------------- Script / test entry points (no input needed) ----------------

        /// <summary>Grabs g immediately (centred on the ray, no fly-in). Returns false if it can't be grabbed.</summary>
        public bool DebugGrab(Grabbable g)
        {
            if (!BeginHold(g, true, false))
                return false;
            activeButton = OVRInput.Button.PrimaryHandTrigger;
            scriptHold = true;
            LateUpdate();
            return true;
        }

        /// <summary>Lets go of whatever is held (snapping into a nearby socket if any) or stops turning.</summary>
        public void DebugRelease() => ReleaseCurrent(Vector3.zero, Vector3.zero);

        /// <summary>Turns t to a world yaw (snapped), with click and onStep. Returns false if it can't turn.</summary>
        public bool DebugTurnTo(Turnable t, float yaw)
        {
            if (t == null || !t.CanTurn)
                return false;
            t.SetYaw(yaw, true);
            return true;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.9f, 0.7f, 0.3f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, grabRadius);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * rayReach);
        }
    }
}
