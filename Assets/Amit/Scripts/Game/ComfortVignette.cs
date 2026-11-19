using UnityEngine;

namespace MansionGame
{
    /// <summary>
    /// Dynamic field-of-view restriction while moving artificially (Fernandes &amp; Feiner 2016): a soft dark ring closes
    /// in while the player is moved by the thumbstick or arm-swinging, and opens again when they stop. Physical
    /// (room-scale) head movement is not restricted. Builds its quad on the centre eye at runtime.
    /// </summary>
    public sealed class ComfortVignette : MonoBehaviour
    {
        [SerializeField, Tooltip("Material using Mansion/ComfortVignette (M_FX_ComfortVignette).")]
        private Material material;
        [SerializeField, Tooltip("Turn the restrictor on or off (comfort option).")]
        private bool comfortEnabled = true;
        [SerializeField, Min(0.1f), Tooltip("Artificial speed (m/s) at which the restriction reaches full strength.")]
        private float fullStrengthSpeed = 1.6f;
        [SerializeField, Range(0f, 1f), Tooltip("Strongest restriction. Kept subtle, as in the paper's subtle condition.")]
        private float maxStrength = 0.75f;
        [SerializeField, Min(0.1f), Tooltip("How fast the ring closes (per second).")]
        private float closeRate = 4f;
        [SerializeField, Min(0.1f), Tooltip("How fast the ring opens again (per second).")]
        private float openRate = 2f;

        static readonly int StrengthId = Shader.PropertyToID("_Strength");

        OVRCameraRig rig;
        CharacterController body;
        ArmSwingLocomotion armSwing;
        Renderer quad;
        MaterialPropertyBlock block;
        Vector3 lastBodyPos;
        float strength, lastYaw;

        void Update()
        {
            if (quad == null && !TryBuild()) return;

            // Artificial motion only: the player root moves for thumbstick/arm-swing, not for physical room-scale walking.
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            var bodyPos = body.transform.position;
            var moved = bodyPos - lastBodyPos;
            moved.y = 0f;
            float speed = moved.magnitude / dt;
            float yaw = body.transform.eulerAngles.y;
            bool snapped = Mathf.Abs(Mathf.DeltaAngle(lastYaw, yaw)) > 5f;
            lastBodyPos = bodyPos;
            lastYaw = yaw;
            if (armSwing != null) speed = Mathf.Max(speed, armSwing.Speed);

            float target = comfortEnabled ? Mathf.Clamp01(speed / fullStrengthSpeed) * maxStrength : 0f;
            if (snapped && comfortEnabled) target = maxStrength;
            float rate = target > strength ? closeRate : openRate;
            strength = Mathf.MoveTowards(strength, target, rate * dt);

            quad.enabled = strength > 0.005f;
            if (!quad.enabled) return;
            quad.GetPropertyBlock(block);
            block.SetFloat(StrengthId, strength);
            quad.SetPropertyBlock(block);
        }

        bool TryBuild()
        {
            if (material == null) return false;
            rig = FindAnyObjectByType<OVRCameraRig>();
            if (rig == null || rig.centerEyeAnchor == null) return false;
            var player = rig.GetComponentInParent<CharacterController>();
            if (player == null) return false;
            body = player;
            armSwing = player.GetComponent<ArmSwingLocomotion>();

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "Comfort Vignette";
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(rig.centerEyeAnchor, false);
            go.transform.localPosition = new Vector3(0f, 0f, 0.12f);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
            quad = go.GetComponent<Renderer>();
            quad.sharedMaterial = material;
            quad.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            quad.receiveShadows = false;
            quad.enabled = false;
            block = new MaterialPropertyBlock();
            lastBodyPos = body.transform.position;
            lastYaw = body.transform.eulerAngles.y;
            return true;
        }
    }
}
