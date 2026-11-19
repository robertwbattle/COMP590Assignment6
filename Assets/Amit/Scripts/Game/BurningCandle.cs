using UnityEngine;

namespace MansionGame
{
    /// <summary>
    /// The timer candle: the wax shrinks as the clock runs down and the flame rides on top.
    /// The wax must be a Unity cylinder (height = 2 x scale.y) with its pivot at its centre.
    /// </summary>
    public sealed class BurningCandle : MonoBehaviour
    {
        [Tooltip("Cylinder whose Y scale shrinks. Base stays fixed.")]
        [SerializeField] private Transform wax;
        [Tooltip("Smallest height as a fraction of the full height.")]
        [SerializeField] private float minFraction = 0.08f;
        [Tooltip("Flame object kept sitting on top of the wax.")]
        [SerializeField] private Transform flame;
        [Tooltip("Optional flame light; intensity flickers +-10%.")]
        [SerializeField] private Light flameLight;
        [Tooltip("Hidden when the player loses.")]
        [SerializeField] private GameObject flameRoot;

        float fullScaleY = 1f;
        float baseY;
        float flameOffset;
        Vector3 flameBaseScale = Vector3.one;
        float lightBase;
        float seed;
        bool ready;

        void Start()
        {
            seed = Random.value * 100f;
            if (wax != null)
            {
                fullScaleY = wax.localScale.y;
                baseY = wax.localPosition.y - fullScaleY;
                if (flame != null)
                    flameOffset = flame.position.y - wax.TransformPoint(0f, 1f, 0f).y;
                ready = true;
            }
            if (flame != null) flameBaseScale = flame.localScale;
            if (flameLight != null) lightBase = flameLight.intensity;
            var game = EscapeGame.Instance;
            if (game != null) game.StateChanged += OnState;
        }

        void OnDestroy()
        {
            if (EscapeGame.Instance != null) EscapeGame.Instance.StateChanged -= OnState;
        }

        void OnState(GameState state)
        {
            if (state == GameState.Lost && flameRoot != null) flameRoot.SetActive(false);
        }

        void Update()
        {
            var game = EscapeGame.Instance;
            float t = game != null && game.TimeLimit > 0f ? Mathf.Clamp01(game.TimeRemaining / game.TimeLimit) : 1f;

            if (ready)
            {
                float h = Mathf.Lerp(minFraction, 1f, t);
                var s = wax.localScale;
                s.y = fullScaleY * h;
                wax.localScale = s;
                var p = wax.localPosition;
                p.y = baseY + s.y;
                wax.localPosition = p;
                if (flame != null)
                {
                    var fp = flame.position;
                    fp.y = wax.TransformPoint(0f, 1f, 0f).y + flameOffset;
                    flame.position = fp;
                }
            }

            float n = Mathf.PerlinNoise(seed, Time.time * 9f);
            if (flame != null)
            {
                float sc = 0.9f + 0.2f * n;
                flame.localScale = new Vector3(flameBaseScale.x * (1.05f - 0.1f * n), flameBaseScale.y * sc, flameBaseScale.z * (1.05f - 0.1f * n));
            }
            if (flameLight != null) flameLight.intensity = lightBase * (0.9f + 0.2f * Mathf.PerlinNoise(seed + 40f, Time.time * 7f));
        }
    }
}
