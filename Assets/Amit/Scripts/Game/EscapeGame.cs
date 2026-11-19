using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace MansionGame
{
    /// <summary>The phases of one run.</summary>
    public enum GameState { Intro, Playing, Won, Lost }

    /// <summary>
    /// Game loop: intro, countdown, three seals, win/lose, best time and restart.
    /// Other scripts report progress with MarkSolved(n), Win() and Lose().
    /// </summary>
    public sealed class EscapeGame : MonoBehaviour
    {
        public const int PuzzleCount = 3;
        const string BestTimeKey = "MansionBestTime";
        const float RestartDelay = 2.5f;

        [Tooltip("Seconds on the clock once the intro ends.")]
        [SerializeField] private float timeLimit = 720f;
        [Tooltip("Seconds of intro before the timer starts.")]
        [SerializeField] private float introSeconds = 6f;
        [Tooltip("Puzzle 1 (the candle). Found automatically if empty.")]
        [SerializeField] private Puzzle1_1 puzzle1;
        [Tooltip("Keyboard shortcuts while holding Shift: 1/2/3 solve, T 20 s left, R restart, L lose, W win.")]
        [SerializeField] private bool debugKeys = true;

        readonly bool[] solved = new bool[PuzzleCount + 1];
        float introTimer;
        float endTimer;
        bool restarting;

        public static EscapeGame Instance { get; private set; }

        public GameState State { get; private set; } = GameState.Intro;
        public float TimeLimit => timeLimit;
        public float IntroSeconds => introSeconds;
        public float Elapsed { get; private set; }
        public float TimeRemaining => Mathf.Max(0f, timeLimit - Elapsed);
        public int PuzzlesSolved { get; private set; }
        /// <summary>Best winning time in seconds, or -1 if none.</summary>
        public float BestTime { get; private set; } = -1f;
        /// <summary>True if the last win set a new best time.</summary>
        public bool NewBest { get; private set; }

        public event Action<int> PuzzleSolved;
        public event Action<GameState> StateChanged;

        void Awake()
        {
            Instance = this;
            BestTime = PlayerPrefs.GetFloat(BestTimeKey, -1f);
            LoadTopTimes();
            if (puzzle1 == null) puzzle1 = FindAnyObjectByType<Puzzle1_1>();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool IsSolved(int puzzle) => puzzle >= 1 && puzzle <= PuzzleCount && solved[puzzle];

        public void MarkSolved(int puzzle)
        {
            if (puzzle < 1 || puzzle > PuzzleCount || solved[puzzle]) return;
            if (State != GameState.Playing && State != GameState.Intro) return;
            solved[puzzle] = true;
            PuzzlesSolved++;
            PuzzleSolved?.Invoke(puzzle);
        }

        /// <summary>Fastest escapes on this device, best first (up to LeaderboardSize).</summary>
        public System.Collections.Generic.IReadOnlyList<float> TopTimes => topTimes;
        /// <summary>Position (0-based) of the latest win in TopTimes, or -1 if it didn't place.</summary>
        public int LastRank { get; private set; } = -1;
        public const int LeaderboardSize = 5;
        const string TopTimesKey = "MansionTopTimes";
        readonly System.Collections.Generic.List<float> topTimes = new System.Collections.Generic.List<float>();

        void LoadTopTimes()
        {
            topTimes.Clear();
            foreach (var part in PlayerPrefs.GetString(TopTimesKey, "").Split(';'))
                if (float.TryParse(part, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var t) && t > 0f)
                    topTimes.Add(t);
            topTimes.Sort();
        }

        void RecordTopTime(float t)
        {
            topTimes.Add(t);
            topTimes.Sort();
            while (topTimes.Count > LeaderboardSize) topTimes.RemoveAt(topTimes.Count - 1);
            LastRank = topTimes.IndexOf(t);
            var parts = new string[topTimes.Count];
            for (int i = 0; i < parts.Length; i++) parts[i] = topTimes[i].ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
            PlayerPrefs.SetString(TopTimesKey, string.Join(";", parts));
        }

        public void Win()
        {
            if (State != GameState.Playing) return;
            RecordTopTime(Elapsed);
            NewBest = false;
            if (BestTime < 0f || Elapsed < BestTime)
            {
                BestTime = Elapsed;
                NewBest = true;
                PlayerPrefs.SetFloat(BestTimeKey, BestTime);
                PlayerPrefs.Save();
            }
            SetState(GameState.Won);
            GameAudio.Play2D(Cue.Win);
            Haptics.PulseBoth(0.4f, 0.4f);
        }

        public void Lose()
        {
            if (State != GameState.Playing && State != GameState.Intro) return;
            SetState(GameState.Lost);
            GameAudio.Play2D(Cue.Lose);
            Haptics.PulseBoth(0.8f, 0.6f);
        }

        public void Restart()
        {
            if (restarting) return;
            restarting = true;
            var scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0) SceneManager.LoadScene(scene.buildIndex);
            else SceneManager.LoadScene(scene.name);
        }

        /// <summary>Formats seconds as m:ss.</summary>
        public static string FormatTime(float seconds)
        {
            int s = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return (s / 60) + ":" + (s % 60).ToString("00");
        }

        void SetState(GameState next)
        {
            if (State == next) return;
            State = next;
            endTimer = 0f;
            StateChanged?.Invoke(next);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            switch (State)
            {
                case GameState.Intro:
                    introTimer += dt;
                    PollPuzzle1();
                    if (introTimer >= introSeconds) SetState(GameState.Playing);
                    break;
                case GameState.Playing:
                    Elapsed += dt;
                    PollPuzzle1();
                    if (Elapsed >= timeLimit)
                    {
                        Elapsed = timeLimit;
                        Lose();
                    }
                    break;
                case GameState.Won:
                case GameState.Lost:
                    endTimer += dt;
                    if (endTimer >= RestartDelay && RestartRequested()) Restart();
                    break;
            }
            if (debugKeys) HandleDebugKeys();
        }

        void PollPuzzle1()
        {
            if (puzzle1 != null && !solved[1] && !puzzle1.IsLit) MarkSolved(1);
        }

        static bool RestartRequested()
        {
            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch)) return true;
            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch)) return true;
            var kb = Keyboard.current;
            return kb != null && kb.spaceKey.wasPressedThisFrame;
        }

        void HandleDebugKeys()
        {
            var kb = Keyboard.current;
            // Shift must be held so WASD walking in the Editor can't trigger a debug win/lose.
            if (kb == null || !kb.shiftKey.isPressed) return;
            if (kb.digit1Key.wasPressedThisFrame)
            {
                if (puzzle1 != null) puzzle1.Extinguish();
                MarkSolved(1);
            }
            if (kb.digit2Key.wasPressedThisFrame)
            {
                var library = FindAnyObjectByType<LibraryPuzzle>();
                if (library != null) library.DebugSolve(); else MarkSolved(2);
            }
            if (kb.digit3Key.wasPressedThisFrame)
            {
                var seal = FindAnyObjectByType<MoonSeal>();
                if (seal != null) seal.DebugSolve(); else MarkSolved(3);
            }
            if (kb.tKey.wasPressedThisFrame && State == GameState.Playing) Elapsed = Mathf.Max(0f, timeLimit - 20f);
            if (kb.rKey.wasPressedThisFrame) Restart();
            if (kb.lKey.wasPressedThisFrame) Lose();
            if (kb.wKey.wasPressedThisFrame) Win();
        }
    }
}
