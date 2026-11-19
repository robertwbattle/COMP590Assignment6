using UnityEngine;

namespace MansionGame
{
    /// <summary>Every sound the game can play. Puzzle and game scripts call GameAudio.Play(cue, position).</summary>
    public enum Cue
    {
        CandleSnuff,
        BookcaseGrind,
        Grab,
        BookThud,
        MirrorClick,
        WrongOrder,
        CupboardUnlock,
        SealUnlock,
        BarsOpen,
        Heartbeat,
        Whisper,
        Win,
        Lose,
    }

    /// <summary>One cue's clip variants and playback settings.</summary>
    [System.Serializable]
    public struct CueClips
    {
        public Cue cue;
        [Tooltip("One clip is picked at random each play.")]
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume;
        [Range(0f, 0.3f), Tooltip("Random pitch variation, +/- this fraction.")]
        public float pitchJitter;
        [Tooltip("Distance in metres at which the 3D sound becomes inaudible.")]
        public float maxDistance;

        /// <summary>Volume, treating an unset (0) value as 1 since struct defaults cannot be serialized.</summary>
        public float Vol => volume > 0f ? volume : 1f;
    }

    /// <summary>
    /// Scene singleton that maps cues to clips. Static calls are no-ops when there is no instance, so other scripts
    /// never need null checks. Uses a small pool of 3D sources, one 2D source and a looping ambience bed.
    /// </summary>
    public sealed class GameAudio : MonoBehaviour
    {
        public static GameAudio Instance { get; private set; }

        [Header("Clips")]
        [SerializeField, Tooltip("Cue to clip mapping.")] private CueClips[] cues = new CueClips[0];

        [Header("Ambience")]
        [SerializeField, Tooltip("Looping mansion bed, played 2D on Start.")] private AudioClip ambience;
        [SerializeField, Range(0f, 1f)] private float ambienceVolume = 0.35f;

        [Header("Pool")]
        [SerializeField, Min(1), Tooltip("Number of pooled 3D one-shot sources.")] private int poolSize = 10;

        AudioSource ambienceSource;
        AudioSource source2D;
        AudioSource[] pool;
        int nextSource;
        CueClips[] byCue;
        bool[] hasCue;

        void Awake()
        {
            Instance = this;
            BuildLookup();

            ambienceSource = NewSource("Ambience", false);
            ambienceSource.loop = true;
            ambienceSource.volume = ambienceVolume;
            source2D = NewSource("Audio2D", false);

            pool = new AudioSource[Mathf.Max(1, poolSize)];
            for (int i = 0; i < pool.Length; i++) pool[i] = NewSource("Audio3D_" + i, true);
        }

        void Start()
        {
            if (ambience == null || ambienceSource == null) return;
            ambienceSource.clip = ambience;
            ambienceSource.Play();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void BuildLookup()
        {
            int count = System.Enum.GetValues(typeof(Cue)).Length;
            byCue = new CueClips[count];
            hasCue = new bool[count];
            if (cues == null) return;
            for (int i = 0; i < cues.Length; i++)
            {
                int idx = (int)cues[i].cue;
                if (idx < 0 || idx >= count || cues[i].clips == null || cues[i].clips.Length == 0) continue;
                byCue[idx] = cues[i];
                hasCue[idx] = true;
            }
        }

        AudioSource NewSource(string sourceName, bool spatial)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.spatialBlend = spatial ? 1f : 0f;
            if (spatial) ConfigureSpatial(s, 12f);
            return s;
        }

        static void ConfigureSpatial(AudioSource s, float maxDistance)
        {
            s.spatialBlend = 1f;
            s.rolloffMode = AudioRolloffMode.Logarithmic;
            s.minDistance = 1f;
            s.maxDistance = maxDistance;
            s.dopplerLevel = 0f;
        }

        bool TryGet(Cue cue, out CueClips entry, out AudioClip clip)
        {
            entry = default;
            clip = null;
            int idx = (int)cue;
            if (hasCue == null || idx < 0 || idx >= hasCue.Length || !hasCue[idx]) return false;
            entry = byCue[idx];
            clip = entry.clips[Random.Range(0, entry.clips.Length)];
            return clip != null;
        }

        AudioSource NextPooled()
        {
            for (int i = 0; i < pool.Length; i++)
            {
                var s = pool[(nextSource + i) % pool.Length];
                if (!s.isPlaying)
                {
                    nextSource = (nextSource + i + 1) % pool.Length;
                    return s;
                }
            }
            var steal = pool[nextSource];
            nextSource = (nextSource + 1) % pool.Length;
            return steal;
        }

        /// <summary>Plays a one-shot 3D sound at a world position.</summary>
        public static void Play(Cue cue, Vector3 position, float volume = 1f)
        {
            var inst = Instance;
            if (inst == null || !inst.TryGet(cue, out var entry, out var clip)) return;
            var s = inst.NextPooled();
            s.Stop();
            s.transform.position = position;
            s.clip = clip;
            s.volume = Mathf.Clamp01(entry.Vol * volume);
            s.pitch = 1f + Random.Range(-entry.pitchJitter, entry.pitchJitter);
            s.maxDistance = entry.maxDistance > 1f ? entry.maxDistance : 12f;
            s.Play();
        }

        /// <summary>Plays a 2D (head-locked) sound, e.g. win/lose stings.</summary>
        public static void Play2D(Cue cue, float volume = 1f)
        {
            var inst = Instance;
            if (inst == null || !inst.TryGet(cue, out var entry, out var clip)) return;
            var s = inst.source2D;
            s.pitch = 1f + Random.Range(-entry.pitchJitter, entry.pitchJitter);
            s.PlayOneShot(clip, Mathf.Clamp01(entry.Vol * volume));
        }

        /// <summary>Starts a looping 3D sound at a position. Returns null if there is no clip. Stop it with StopLoop.</summary>
        public static AudioSource StartLoop(Cue cue, Vector3 position)
        {
            var inst = Instance;
            if (inst == null || !inst.TryGet(cue, out var entry, out var clip)) return null;
            var s = inst.NewSource("Loop_" + cue, true);
            ConfigureSpatial(s, entry.maxDistance > 1f ? entry.maxDistance : 12f);
            s.transform.position = position;
            s.clip = clip;
            s.loop = true;
            s.volume = Mathf.Clamp01(entry.Vol);
            s.pitch = 1f;
            s.Play();
            return s;
        }

        /// <summary>Fades out and destroys a loop started with StartLoop.</summary>
        public static void StopLoop(AudioSource loop, float fadeSeconds = 0.3f)
        {
            if (loop == null) return;
            var inst = Instance;
            if (inst == null || fadeSeconds <= 0f)
            {
                Destroy(loop.gameObject);
                return;
            }
            inst.StartCoroutine(inst.FadeAndKill(loop, fadeSeconds));
        }

        System.Collections.IEnumerator FadeAndKill(AudioSource s, float seconds)
        {
            float start = s != null ? s.volume : 0f;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                if (s == null) yield break;
                s.volume = start * (1f - t / seconds);
                yield return null;
            }
            if (s != null) Destroy(s.gameObject);
        }
    }
}
