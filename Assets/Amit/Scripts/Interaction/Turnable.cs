using UnityEngine;
using UnityEngine.Events;

namespace MansionGame
{
    /// <summary>
    /// Something a hand can twist about world Y in fixed steps (the turnable moonlight mirrors).
    /// Steps are counted from the authored yaw, so Awake never moves the object. Each step clicks and ticks the
    /// turning hand's controller.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Turnable : MonoBehaviour
    {
        [SerializeField, Min(0.1f), Tooltip("Snap size in degrees.")]
        private float stepDegrees = 15f;
        [SerializeField, Tooltip("Limit how far it can turn from its authored yaw.")]
        private bool clampRange;
        [SerializeField, Tooltip("Lowest yaw offset (degrees) from the authored yaw, when clampRange is on.")]
        private float minYawOffset = -90f;
        [SerializeField, Tooltip("Highest yaw offset (degrees) from the authored yaw, when clampRange is on.")]
        private float maxYawOffset = 90f;
        [SerializeField, Tooltip("Whether a hand may turn it.")]
        private bool canTurn = true;

        /// <summary>Fires with the new world yaw (degrees, 0..360) after each step.</summary>
        public UnityEvent<float> onStep = new UnityEvent<float>();

        Quaternion authoredRotation;
        float authoredYaw;
        float offset;                 // snapped offset from the authored yaw
        bool initialised;

        Transform turningHand;
        OVRInput.Controller turningController = OVRInput.Controller.None;
        Vector3 lastHandForward;
        float accumulated;            // hand yaw change since BeginTurn
        float offsetAtBegin;

        public bool CanTurn { get => canTurn; set => canTurn = value; }
        public bool IsTurning => turningHand != null;
        /// <summary>Current snapped world yaw in degrees (0..360).</summary>
        public float CurrentYaw { get { Init(); return Mathf.Repeat(authoredYaw + offset, 360f); } }
        /// <summary>Current snapped yaw relative to the authored yaw.</summary>
        public float YawOffset => offset;
        public float StepDegrees => stepDegrees;

        void Awake() => Init();

        void Init()
        {
            if (initialised)
                return;
            initialised = true;
            authoredRotation = transform.rotation;
            authoredYaw = transform.eulerAngles.y;
            offset = 0f;
        }

        /// <summary>Starts turning with this hand. Returns false if turning is disabled.</summary>
        public bool BeginTurn(Transform hand)
        {
            Init();
            if (!canTurn || hand == null)
                return false;
            turningHand = hand;
            var mh = hand.GetComponent<MansionHand>();
            turningController = mh != null ? mh.Controller : OVRInput.Controller.None;
            lastHandForward = Flat(hand.forward);
            accumulated = 0f;
            offsetAtBegin = offset;
            return true;
        }

        /// <summary>Applies the hand's yaw change since BeginTurn, snapped to steps.</summary>
        public void UpdateTurn(Transform hand)
        {
            if (hand == null || hand != turningHand || !canTurn)
                return;
            var fwd = Flat(hand.forward);
            if (fwd == Vector3.zero || lastHandForward == Vector3.zero)
            {
                if (fwd != Vector3.zero)
                    lastHandForward = fwd;
                return;
            }
            accumulated += Vector3.SignedAngle(lastHandForward, fwd, Vector3.up);
            lastHandForward = fwd;
            ApplyOffset(offsetAtBegin + accumulated, true, turningController);
        }

        public void EndTurn()
        {
            turningHand = null;
            turningController = OVRInput.Controller.None;
        }

        /// <summary>Sets the world yaw (snapped and clamped). With notify, plays the click and fires onStep.</summary>
        public void SetYaw(float worldYaw, bool notify = false)
        {
            Init();
            ApplyOffset(Mathf.DeltaAngle(authoredYaw, worldYaw), notify, OVRInput.Controller.None);
        }

        void ApplyOffset(float raw, bool notify, OVRInput.Controller tick)
        {
            float snapped = Mathf.Round(raw / stepDegrees) * stepDegrees;
            if (clampRange)
                snapped = Mathf.Clamp(snapped, minYawOffset, maxYawOffset);
            if (Mathf.Approximately(snapped, offset))
                return;
            offset = snapped;
            transform.rotation = Quaternion.AngleAxis(offset, Vector3.up) * authoredRotation;
            if (!notify)
                return;
            GameAudio.Play(Cue.MirrorClick, transform.position);
            if (tick != OVRInput.Controller.None)
                Haptics.Pulse(tick, 0.15f, 0.03f, 0.8f);
            onStep.Invoke(CurrentYaw);
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude < 1e-4f ? Vector3.zero : v.normalized;
        }
    }
}
