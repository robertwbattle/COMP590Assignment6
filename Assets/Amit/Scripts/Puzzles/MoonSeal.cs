using UnityEngine;
using UnityEngine.Events;

namespace MansionGame
{
    /// <summary>
    /// The seal on the barred window. MoonBeam calls Illuminate every frame it lands here; after
    /// <c>requiredSeconds</c> of unbroken light (gaps under <c>graceSeconds</c> are forgiven) puzzle 3 is solved.
    /// </summary>
    public sealed class MoonSeal : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("Seconds the beam must stay on the seal.")]
        private float requiredSeconds = 1f;
        [SerializeField, Min(0f), Tooltip("Progress resets if the beam is off the seal for longer than this.")]
        private float graceSeconds = 0.2f;
        [SerializeField, Tooltip("Optional renderer shown only while lit.")]
        private Renderer glow;
        [SerializeField, Tooltip("Optional child object enabled only while lit.")]
        private GameObject glowObject;
        [SerializeField, Tooltip("Keep the glow on once solved.")]
        private bool glowWhenSolved = true;

        public UnityEvent onSolved = new UnityEvent();

        float charge;
        float lastLitTime = float.NegativeInfinity;
        bool glowing;

        public bool Solved { get; private set; }
        public bool IsLit => Time.time - lastLitTime <= graceSeconds;
        /// <summary>0..1 progress towards solving.</summary>
        public float Progress => Solved ? 1f : requiredSeconds <= 0f ? 0f : Mathf.Clamp01(charge / requiredSeconds);

        void Awake()
        {
            glowing = true;
            SetGlow(false);
        }

        void Update()
        {
            bool lit = IsLit;
            if (!lit && !Solved)
                charge = 0f;
            SetGlow(lit || (Solved && glowWhenSolved));
        }

        /// <summary>Adds dt seconds of moonlight.</summary>
        public void Illuminate(float dt)
        {
            if (!IsLit)
                charge = 0f;
            lastLitTime = Time.time;
            SetGlow(true);
            if (Solved)
                return;
            charge += Mathf.Max(0f, dt);
            if (charge >= requiredSeconds)
                Solve();
        }

        public void DebugSolve() => Solve();

        void Solve()
        {
            if (Solved)
                return;
            Solved = true;
            charge = requiredSeconds;
            SetGlow(glowWhenSolved);
            EscapeGame.Instance?.MarkSolved(3);
            GameAudio.Play(Cue.SealUnlock, transform.position);
            Haptics.PulseBoth(0.5f, 0.3f);
            onSolved.Invoke();
        }

        void SetGlow(bool on)
        {
            if (on == glowing)
                return;
            glowing = on;
            if (glow != null)
                glow.enabled = on;
            if (glowObject != null)
                glowObject.SetActive(on);
        }
    }
}
