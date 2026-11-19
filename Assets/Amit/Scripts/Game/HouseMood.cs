using UnityEngine;

namespace MansionGame
{
    /// <summary>
    /// Atmosphere tied to the clock: lights dim, whispers and a heartbeat rise near the end,
    /// the house goes dark on a loss and the moon brightens on a win.
    /// </summary>
    public sealed class HouseMood : MonoBehaviour
    {
        [Header("Dimming")]
        [SerializeField] private Light[] dimLights;
        [Tooltip("Remaining-time fraction where dimming starts.")]
        [SerializeField, Range(0f, 1f)] private float dimStartFraction = 0.3f;
        [Tooltip("Intensity multiplier when time runs out.")]
        [SerializeField, Range(0f, 1f)] private float dimTo = 0.55f;

        [Header("Heartbeat and whispers")]
        [SerializeField] private float heartbeatSeconds = 60f;
        [SerializeField] private Vector2 whisperInterval = new Vector2(12f, 25f);

        [Header("Lose")]
        [SerializeField] private GameObject[] flamesToSnuffOnLose;
        [SerializeField] private Light[] lightsOffOnLose;

        [Header("Win")]
        [SerializeField] private Light moonLight;

        float[] dimBase;
        float[] loseStart;
        float moonBase;
        float stateTime;
        float nextBeat;
        float secondPulseAt = -1f;
        float nextWhisper;
        bool loseInit;

        void Start()
        {
            dimBase = new float[dimLights != null ? dimLights.Length : 0];
            for (int i = 0; i < dimBase.Length; i++) dimBase[i] = dimLights[i] != null ? dimLights[i].intensity : 0f;
            if (moonLight != null) moonBase = moonLight.intensity;
            nextWhisper = Random.Range(whisperInterval.x, whisperInterval.y);
            var g = EscapeGame.Instance;
            if (g != null) g.StateChanged += OnState;
        }

        void OnDestroy()
        {
            if (EscapeGame.Instance != null) EscapeGame.Instance.StateChanged -= OnState;
        }

        void OnState(GameState s)
        {
            stateTime = 0f;
            secondPulseAt = -1f;
            if (s != GameState.Lost) return;
            if (flamesToSnuffOnLose != null)
                foreach (var f in flamesToSnuffOnLose) if (f != null) f.SetActive(false);
            int n = lightsOffOnLose != null ? lightsOffOnLose.Length : 0;
            loseStart = new float[n];
            for (int i = 0; i < n; i++) loseStart[i] = lightsOffOnLose[i] != null ? lightsOffOnLose[i].intensity : 0f;
            loseInit = true;
        }

        void Update()
        {
            var g = EscapeGame.Instance;
            if (g == null) return;
            float dt = Time.deltaTime;
            stateTime += dt;

            switch (g.State)
            {
                case GameState.Playing:
                    UpdateDim(g);
                    UpdateHeartbeat(g, dt);
                    UpdateWhispers(g, dt);
                    break;
                case GameState.Lost:
                    if (loseInit && stateTime <= 3.1f)
                    {
                        float k = Mathf.Lerp(1f, 0.1f, Mathf.Clamp01(stateTime / 3f));
                        for (int i = 0; i < loseStart.Length; i++)
                            if (lightsOffOnLose[i] != null) lightsOffOnLose[i].intensity = loseStart[i] * k;
                    }
                    break;
                case GameState.Won:
                    if (moonLight != null && stateTime <= 2.1f)
                        moonLight.intensity = moonBase * Mathf.Lerp(1f, 1.5f, Mathf.Clamp01(stateTime / 2f));
                    break;
            }
        }

        void UpdateDim(EscapeGame g)
        {
            if (dimBase == null || dimStartFraction <= 0f) return;
            float frac = g.TimeLimit > 0f ? g.TimeRemaining / g.TimeLimit : 1f;
            float t = Mathf.Clamp01(1f - frac / dimStartFraction);
            float mult = Mathf.Lerp(1f, dimTo, t);
            for (int i = 0; i < dimBase.Length; i++)
                if (dimLights[i] != null) dimLights[i].intensity = dimBase[i] * mult;
        }

        void UpdateHeartbeat(EscapeGame g, float dt)
        {
            float rem = g.TimeRemaining;
            if (rem > heartbeatSeconds || heartbeatSeconds <= 0f)
            {
                nextBeat = 0f;
                return;
            }
            float p = 1f - rem / heartbeatSeconds;
            float now = Time.time;
            if (secondPulseAt > 0f && now >= secondPulseAt)
            {
                secondPulseAt = -1f;
                Haptics.PulseBoth(0.3f, 0.06f);
            }
            if (now >= nextBeat)
            {
                GameAudio.Play2D(Cue.Heartbeat, Mathf.Lerp(0.3f, 1f, p));
                Haptics.PulseBoth(0.3f, 0.06f);
                secondPulseAt = now + 0.18f;
                nextBeat = now + Mathf.Lerp(1.3f, 0.6f, p);
            }
        }

        void UpdateWhispers(EscapeGame g, float dt)
        {
            float frac = g.TimeLimit > 0f ? g.TimeRemaining / g.TimeLimit : 1f;
            if (frac > dimStartFraction) return;
            nextWhisper -= dt;
            if (nextWhisper > 0f) return;
            GameAudio.Play2D(Cue.Whisper, 0.7f);
            nextWhisper = Random.Range(whisperInterval.x, whisperInterval.y);
        }
    }
}
