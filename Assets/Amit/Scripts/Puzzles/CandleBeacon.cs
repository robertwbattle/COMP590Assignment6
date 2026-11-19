using UnityEngine;

namespace MansionGame
{
    /// <summary>
    /// Makes the Puzzle 1 candle stand out: every few seconds its flame gutters and flickers between its warm colour and
    /// a ghostly teal, then settles back. Uses a MaterialPropertyBlock so the shared kit material is never changed.
    /// Stops on its own once the candle is put out (the flame object is deactivated).
    /// </summary>
    public sealed class CandleBeacon : MonoBehaviour
    {
        [SerializeField, Tooltip("Flame renderer to tint. Defaults to Puzzle1_1.flame on this object.")]
        private Renderer flame;
        [SerializeField, Min(0.5f), Tooltip("Seconds between ghostly flickers (randomised by ±25%).")]
        private float interval = 2.5f;
        [SerializeField, Min(0.1f), Tooltip("How long each flicker burst lasts, in seconds.")]
        private float burstSeconds = 0.7f;
        [SerializeField, Tooltip("Ghost colour the flame flickers to (base colour).")]
        private Color ghostBase = new Color(0.45f, 0.78f, 0.74f, 1f);
        [SerializeField, ColorUsage(false, true), Tooltip("Ghost emission (HDR).")]
        private Color ghostEmission = new Color(0.45f, 1.6f, 1.45f, 1f);
        [SerializeField, Min(1f), Tooltip("Flame grows by up to this factor during a flicker.")]
        private float flareScale = 1.35f;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        MaterialPropertyBlock block;
        Color warmBase, warmEmission;
        Vector3 baseScale;
        float nextBurst, burstStart = -1f;

        void Awake()
        {
            if (flame == null)
            {
                var puzzle = GetComponent<Puzzle1_1>();
                if (puzzle != null && puzzle.flame != null) flame = puzzle.flame.GetComponentInChildren<Renderer>(true);
            }
            if (flame == null) { enabled = false; return; }
            var mat = flame.sharedMaterial;
            warmBase = mat != null && mat.HasProperty(BaseColorId) ? mat.GetColor(BaseColorId) : Color.white;
            warmEmission = mat != null && mat.HasProperty(EmissionId) ? mat.GetColor(EmissionId) : Color.white;
            baseScale = flame.transform.localScale;
            block = new MaterialPropertyBlock();
            nextBurst = Time.time + interval * 0.6f;
        }

        void Update()
        {
            if (!flame.gameObject.activeInHierarchy) return;
            float now = Time.time;
            if (burstStart < 0f && now >= nextBurst) burstStart = now;
            if (burstStart < 0f) return;

            float t = (now - burstStart) / burstSeconds;
            if (t >= 1f)
            {
                Apply(0f);
                burstStart = -1f;
                nextBurst = now + interval * Random.Range(0.75f, 1.25f);
                return;
            }
            // Fast stuttering flicker inside a smooth envelope, so it reads as a gutter, not a blink.
            float envelope = Mathf.Sin(t * Mathf.PI);
            float stutter = Mathf.PerlinNoise(now * 18f, 0.37f);
            Apply(Mathf.Clamp01(envelope * (0.55f + 0.65f * stutter)));
        }

        void Apply(float ghost)
        {
            flame.GetPropertyBlock(block);
            block.SetColor(BaseColorId, Color.Lerp(warmBase, ghostBase, ghost));
            block.SetColor(EmissionId, Color.Lerp(warmEmission, ghostEmission, ghost));
            flame.SetPropertyBlock(block);
            flame.transform.localScale = baseScale * Mathf.Lerp(1f, flareScale, ghost);
        }

        void OnDisable()
        {
            if (flame != null && block != null) Apply(0f);
        }
    }
}
