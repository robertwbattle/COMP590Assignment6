using System.Collections;
using UnityEngine;

namespace MansionGame
{
    /// <summary>
    /// Nudges stuck players toward the Puzzle 2 clue. Once the hidden room is open, if the books aren't solved after a
    /// while (and right after a wrong order), the book glyphs in the haunted mirror flash so they can be seen from
    /// across the room, a whisper plays at the mirror, and a hint message appears.
    /// </summary>
    public sealed class ClueHint : MonoBehaviour
    {
        [SerializeField, Tooltip("Renderer of the Mansion/MirrorWhisper clue quad.")]
        private Renderer clue;
        [SerializeField, Tooltip("The library puzzle. Found automatically when empty.")]
        private LibraryPuzzle library;
        [SerializeField, Min(5f), Tooltip("Seconds after the hidden room opens before the first hint.")]
        private float firstHintDelay = 75f;
        [SerializeField, Min(5f), Tooltip("Seconds between later hints while the puzzle is unsolved.")]
        private float repeatEvery = 60f;
        [SerializeField, Min(0.5f), Tooltip("How long the flashing lasts.")]
        private float flashSeconds = 4f;

        static readonly int RevealNearId = Shader.PropertyToID("_RevealNear");
        static readonly int RevealFarId = Shader.PropertyToID("_RevealFar");
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        MaterialPropertyBlock block;
        GameMessages messages;
        float nextHint = -1f;
        bool flashing;

        void Awake()
        {
            if (clue == null) clue = GetComponent<Renderer>();
            if (library == null) library = FindAnyObjectByType<LibraryPuzzle>();
            messages = FindAnyObjectByType<GameMessages>();
            block = new MaterialPropertyBlock();
        }

        void OnEnable()
        {
            if (library != null) library.onWrongOrder.AddListener(OnWrongOrder);
        }

        void OnDisable()
        {
            if (library != null) library.onWrongOrder.RemoveListener(OnWrongOrder);
        }

        void OnWrongOrder()
        {
            // Let the wrong-order rattle land first, then point at the clue.
            if (!flashing) StartCoroutine(HintAfter(1.2f));
        }

        IEnumerator HintAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            Hint();
        }

        void Update()
        {
            var g = EscapeGame.Instance;
            if (g == null || clue == null || g.State != GameState.Playing) return;
            if (!g.IsSolved(1) || g.IsSolved(2)) { nextHint = -1f; return; }
            if (nextHint < 0f) nextHint = Time.time + firstHintDelay;
            if (Time.time >= nextHint && !flashing) Hint();
        }

        /// <summary>Flashes the clue and shows the hint message now.</summary>
        public void Hint()
        {
            var g = EscapeGame.Instance;
            if (g != null && g.IsSolved(2)) return;
            nextHint = Time.time + repeatEvery;
            if (flashing) return;
            StartCoroutine(Flash());
            GameAudio.Play(Cue.Whisper, clue.transform.position, 0.9f);
            if (messages != null)
                messages.Show("A whisper from the fireplace…",
                    "The haunted mirror above the mantel flickers.\nLean in close to it and read what it shows.", 6f);
        }

        IEnumerator Flash()
        {
            flashing = true;
            for (float t = 0f; t < flashSeconds; t += Time.deltaTime)
            {
                // Three slow pulses, visible from anywhere in the parlour.
                float pulse = 0.5f - 0.5f * Mathf.Cos(t / flashSeconds * Mathf.PI * 6f);
                clue.GetPropertyBlock(block);
                block.SetFloat(RevealNearId, 30f);
                block.SetFloat(RevealFarId, 40f);
                block.SetFloat(IntensityId, Mathf.Lerp(0.15f, 1f, pulse));
                clue.SetPropertyBlock(block);
                yield return null;
            }
            clue.SetPropertyBlock(null); // back to the lean-in-close behaviour
            flashing = false;
        }

        void OnDestroy()
        {
            if (clue != null) clue.SetPropertyBlock(null);
        }
    }
}
