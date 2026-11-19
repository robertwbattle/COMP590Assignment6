using UnityEngine;

namespace MansionGame
{
    /// <summary>
    /// Arm-swing walking (after McCullough et al. 2015, "Myo arm: swinging to explore a VE", ACM SAP): hold both grips
    /// with empty hands and swing your arms like you're walking; you move the way you're looking, faster the harder you
    /// swing. Works alongside the thumbstick locomotion of OVRPlayerController and uses its CharacterController, so
    /// walls and gravity behave the same.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class ArmSwingLocomotion : MonoBehaviour
    {
        [SerializeField, Tooltip("Head used for the walking direction. Found from OVRCameraRig when empty.")]
        private Transform head;
        [SerializeField, Min(0f), Tooltip("Metres per second of walking per metre per second of average hand speed.")]
        private float gain = 1.6f;
        [SerializeField, Min(0f), Tooltip("Hand speed (m/s) below which swinging is ignored, so small fidgets don't move you.")]
        private float deadZone = 0.35f;
        [SerializeField, Min(0.1f), Tooltip("Top walking speed (m/s).")]
        private float maxSpeed = 2.4f;
        [SerializeField, Min(0.1f), Tooltip("How quickly walking speed follows the swing (higher = snappier).")]
        private float response = 6f;

        /// <summary>Current arm-swing walking speed in m/s (0 when not swinging). Read by ComfortVignette.</summary>
        public float Speed { get; private set; }

        CharacterController body;

        void Awake() => body = GetComponent<CharacterController>();

        void Update()
        {
            if (head == null)
            {
                var rig = GetComponentInChildren<OVRCameraRig>();
                if (rig != null) head = rig.centerEyeAnchor;
            }

            float target = 0f;
            if (head != null && OVRManager.hasInputFocus && BothGripsFree())
            {
                // Hand speed in tracking space, so the player's own movement doesn't feed back into the swing.
                float l = OVRInput.GetLocalControllerVelocity(OVRInput.Controller.LTouch).magnitude;
                float r = OVRInput.GetLocalControllerVelocity(OVRInput.Controller.RTouch).magnitude;
                float swing = Mathf.Max(0f, (l + r) * 0.5f - deadZone);
                target = Mathf.Min(maxSpeed, swing * gain);
            }
            Speed = Mathf.Lerp(Speed, target, 1f - Mathf.Exp(-response * Time.deltaTime));
            if (Speed < 0.02f || head == null || !body.enabled) return;

            var dir = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            if (dir.sqrMagnitude < 1e-4f) return;
            body.Move(dir.normalized * (Speed * Time.deltaTime));
        }

        /// <summary>True when both grips are held and neither hand is carrying or turning something.</summary>
        static bool BothGripsFree()
        {
            if (!OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch)) return false;
            if (!OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch)) return false;
            var hands = MansionHand.All;
            for (int i = 0; i < hands.Count; i++)
                if (hands[i] != null && (hands[i].Held != null || hands[i].Turning != null)) return false;
            return true;
        }
    }
}
