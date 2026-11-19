using UnityEngine;

namespace MansionGame
{
    /// <summary>
    /// Plays the sounds for the parlour's opening puzzle by watching existing scripts: the candle snuff when the flame
    /// goes out, and a looping grind while the secret bookcase moves. Polls once per frame without allocating.
    /// </summary>
    public sealed class AudioCues : MonoBehaviour
    {
        [SerializeField, Tooltip("Puzzle 1 candle. Found automatically when empty.")] private Puzzle1_1 candle;
        [SerializeField, Tooltip("Secret bookcase. Found automatically when empty.")] private MansionKit.SecretBookcase bookcase;
        [SerializeField, Min(0f), Tooltip("Fade-out time of the grind when the bookcase stops.")] private float grindFade = 0.3f;

        bool candleWasLit = true;
        AudioSource grindSource;

        void Start()
        {
            if (candle == null) candle = FindAnyObjectByType<Puzzle1_1>();
            if (bookcase == null) bookcase = FindAnyObjectByType<MansionKit.SecretBookcase>();
            if (candle != null) candleWasLit = candle.IsLit;
        }

        void Update()
        {
            if (candle != null)
            {
                bool lit = candle.IsLit;
                if (candleWasLit && !lit) GameAudio.Play(Cue.CandleSnuff, candle.transform.position);
                candleWasLit = lit;
            }

            if (bookcase != null)
            {
                bool moving = bookcase.IsAnimating;
                if (moving && grindSource == null)
                {
                    var t = bookcase.bookcase != null ? bookcase.bookcase : bookcase.transform;
                    grindSource = GameAudio.StartLoop(Cue.BookcaseGrind, t.position);
                }
                else if (!moving && grindSource != null)
                {
                    GameAudio.StopLoop(grindSource, grindFade);
                    grindSource = null;
                }
                else if (moving && grindSource != null && bookcase.bookcase != null)
                {
                    grindSource.transform.position = bookcase.bookcase.position;
                }
            }
        }

        void OnDestroy()
        {
            if (grindSource != null) Destroy(grindSource.gameObject);
        }
    }
}
