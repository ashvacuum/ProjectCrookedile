using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Art.Iso
{
    // Cloud cover for the city's light (city-generator.md §6.4): coverage, edge softness, how much sun a cloud blocks,
    // cloud size and wind. Plain data so a future forecast can author one per weather and hand it to CitySun.Clouds.
    [Serializable]
    public sealed class CityClouds
    {
        private static readonly int CloudsId = Shader.PropertyToID("_CityClouds");
        private static readonly int CloudWindId = Shader.PropertyToID("_CityCloudWind");

        [Tooltip("How much of the sky is cloud: 0 = clear, 1 = overcast.")]
        [SerializeField, Range(0f, 1f)]
        private float _coverage = 0.4f;

        [Tooltip("Width of a cloud shadow's soft edge: 0 = hard, 1 = very diffuse.")]
        [SerializeField, Range(0f, 1f)]
        private float _softness = 0.5f;

        [Tooltip(
            "Share of direct sunlight a thick cloud blocks. Keep it below the building shadow strength so buildings still read."
        )]
        [SerializeField, Range(0f, 1f)]
        private float _density = 0.35f;

        [Tooltip("Typical cloud size, in cells.")]
        [SerializeField, Min(0.5f)]
        private float _sizeCells = 12f;

        [Tooltip(
            "Direction the clouds drift toward, in degrees around the grid: 0 = +row (up-right), 90 = +column (down-right)."
        )]
        [SerializeField, Range(0f, 360f)]
        private float _windAzimuth = 30f;

        [Tooltip(
            "Drift speed, in cells per second. Shows in the Scene view with Always Refresh on, or in Play mode."
        )]
        [SerializeField, Min(0f)]
        private float _windCellsPerSecond = 0.6f;

        public CityClouds() { }

        public CityClouds(float coverage, float softness, float density, float sizeCells)
        {
            _coverage = coverage;
            _softness = softness;
            _density = density;
            _sizeCells = sizeCells;
        }

        // Applies these clouds, blended `blend` of the way toward `toward` (rain's overcast sky).
        public void Apply(CityClouds toward = null, float blend = 0f)
        {
            toward ??= this;
            float windRadians =
                Mathf.Lerp(_windAzimuth, toward._windAzimuth, blend) * Mathf.Deg2Rad;
            float windSpeed = Mathf.Lerp(_windCellsPerSecond, toward._windCellsPerSecond, blend);
            Vector2 wind = new Vector2(Mathf.Cos(windRadians), Mathf.Sin(windRadians)) * windSpeed;
            Shader.SetGlobalVector(
                CloudsId,
                Vector4.Lerp(
                    new Vector4(_coverage, _softness, _density, _sizeCells),
                    new Vector4(toward._coverage, toward._softness, toward._density, toward._sizeCells),
                    blend
                )
            );
            Shader.SetGlobalVector(CloudWindId, new Vector4(wind.x, wind.y, 0f, 0f));
        }
    }

    // Rain on the city's light and surfaces (city-generator.md §6.3). Cosmetic only: the campaign's seeded day roll
    // decides whether it rains and sets Intensity; the map shows rain everywhere when any district is rainy.
    [Serializable]
    public sealed class CityRain
    {
        private static readonly int RainId = Shader.PropertyToID("_CityRain");
        private static readonly int RainSurfaceId = Shader.PropertyToID("_CityRainSurface");

        [Tooltip("How hard it rains: 0 = dry, 1 = full rain. The campaign sets this; scrub it to preview.")]
        [SerializeField, Range(0f, 1f)]
        private float _intensity;

        [Tooltip(
            "Seconds for surfaces to soak or dry after the intensity changes, in Play mode. Edit mode follows it at once."
        )]
        [SerializeField, Min(0f)]
        private float _soakSeconds = 4f;

        [Header("Overcast light")]
        [Tooltip("Share of direct sunlight removed at full rain.")]
        [SerializeField, Range(0f, 1f)]
        private float _sunDim = 0.6f;

        [Tooltip("Ambient colour is multiplied by this at full rain: a cool grey.")]
        [SerializeField]
        private Color _ambientTint = new Color(0.78f, 0.82f, 0.9f);

        [Tooltip("Building shadow strength is multiplied by this at full rain, so shadows go faint.")]
        [SerializeField, Range(0f, 1f)]
        private float _shadowScale = 0.4f;

        [Tooltip("Window emission is multiplied by this at full rain: lights come on in the gloom.")]
        [SerializeField, Min(0f)]
        private float _emissionBoost = 2f;

        [Tooltip("Clouds at full rain. The sun's own clouds blend toward these as intensity rises.")]
        [SerializeField, InlineProperty]
        private CityClouds _overcast = new CityClouds(0.95f, 0.8f, 0.5f, 20f);

        [Header("Wet surfaces")]
        [Tooltip("How much wet roofs and ground darken: 0 = not at all, 1 = black. Walls get a quarter of it.")]
        [SerializeField, Range(0f, 1f)]
        private float _darkening = 0.35f;

        [Tooltip("Strength of the overcast sky reflected in wet roofs and roads. Puddles reflect it in full.")]
        [SerializeField, Range(0f, 2f)]
        private float _reflection = 0.5f;

        [Tooltip(
            "Share of flat ground and roofs under puddles when fully wet. Puddles start in creases (the occlusion map)."
        )]
        [SerializeField, Range(0f, 1f)]
        private float _puddleCoverage = 0.35f;

        [Tooltip("Typical puddle size, in cells.")]
        [SerializeField, Min(0.1f)]
        private float _puddleSizeCells = 1.2f;

        [Tooltip("Strength of window glow smeared down wet roads, like reflections on a wet street at night.")]
        [SerializeField, Range(0f, 2f)]
        private float _glowSmear = 0.6f;

        [Tooltip("How far the glow smear stretches down the screen, in cells.")]
        [SerializeField, Range(0f, 4f)]
        private float _glowSmearCells = 1.5f;

        [NonSerialized]
        private float _wetness;

        public float Intensity
        {
            get => _intensity;
            set => _intensity = Mathf.Clamp01(value);
        }

        public float SunDim => _sunDim;
        public Color AmbientTint => _ambientTint;
        public float ShadowScale => _shadowScale;
        public float EmissionBoost => _emissionBoost;
        public CityClouds Overcast => _overcast;

        public void Apply()
        {
            // ponytail: wetness only fades; rain lasts whole days, so no lingering puddles after it stops. Add a slower
            // dry rate if the map stays on screen when rain ends.
            _wetness =
                Application.isPlaying && _soakSeconds > 0f
                    ? Mathf.MoveTowards(_wetness, _intensity, Time.deltaTime / _soakSeconds)
                    : _intensity;
            Shader.SetGlobalVector(
                RainId,
                new Vector4(_wetness, _darkening, _reflection, _puddleCoverage)
            );
            Shader.SetGlobalVector(
                RainSurfaceId,
                new Vector4(_puddleSizeCells, _glowSmear, _glowSmearCells, 0f)
            );
        }
    }

    // The city's one light (city-generator.md §6.1): sets the sun direction, colours, window emission and shadow
    // tuning as shader globals for every half-cube surface. One per scene; the last one enabled wins.
    [ExecuteAlways]
    public sealed class CitySun : MonoBehaviour
    {
        private const float MINUTES_PER_DAY = 1440f;
        private static readonly int SunDirectionId = Shader.PropertyToID("_CitySunDirection");
        private static readonly int SunDirectionGridId = Shader.PropertyToID(
            "_CitySunDirectionGrid"
        );
        private static readonly int SunColorId = Shader.PropertyToID("_CitySunColor");
        private static readonly int AmbientColorId = Shader.PropertyToID("_CityAmbientColor");
        private static readonly int EmissionScaleId = Shader.PropertyToID("_CityEmissionScale");
        private static readonly int ShadowId = Shader.PropertyToID("_CityShadow");

        [Tooltip("Minute of the day: 480 = 08:00, 960 = 16:00. Scrub it to preview any hour.")]
        [SerializeField, Range(0f, 1439f)]
        private float _minuteOfDay = 600f;

        [Tooltip("Seconds for a whole day in Play mode, for previews. 0 holds the time still.")]
        [SerializeField, Min(0f)]
        private float _previewSecondsPerDay;

        [Header("Sun path")]
        [Tooltip("Minute the sun rises.")]
        [SerializeField, Range(0, 1439)]
        private int _sunriseMinute = 360;

        [Tooltip("Minute the sun sets.")]
        [SerializeField, Range(1, 1440)]
        private int _sunsetMinute = 1080;

        [Tooltip("Sun elevation above the ground at midday, in degrees.")]
        [SerializeField, Range(5f, 89f)]
        private float _middayElevation = 60f;

        [Tooltip(
            "Direction the sun shines from at sunrise, in degrees around the grid: 0 = +row (behind, up-right), 90 = +column (front, down-right), 180 = front-left, 270 = upper-left."
        )]
        [SerializeField, Range(0f, 360f)]
        private float _sunriseAzimuth = 200f;

        [Tooltip(
            "Direction the sun shines from at sunset, same angles as sunrise. The sun sweeps between the two through the day."
        )]
        [SerializeField, Range(0f, 360f)]
        private float _sunsetAzimuth = 340f;

        [Header("Colour (left of each gradient is sunrise or night, right is sunset or midday)")]
        [Tooltip("Sun colour from sunrise (left) to sunset (right).")]
        [SerializeField]
        private Gradient _sunColor = ThreeKeys(
            new Color(1f, 0.62f, 0.38f),
            new Color(1f, 0.95f, 0.86f),
            new Color(1f, 0.55f, 0.32f)
        );

        [Tooltip(
            "Ambient colour from night (left) to midday (right). Materials scale it by their Ambient Strength."
        )]
        [SerializeField]
        private Gradient _ambientColor = ThreeKeys(
            new Color(0.32f, 0.38f, 0.62f),
            new Color(0.8f, 0.78f, 0.82f),
            new Color(1f, 0.97f, 0.9f)
        );

        [Tooltip("Window emission multiplier: X at night, Y at midday.")]
        [SerializeField]
        private Vector2 _emission = new Vector2(1.6f, 0.05f);

        [Header("Shadows")]
        [Tooltip("How dark shadowed sunlight gets: 0 = no shadows, 1 = no sun at all in shadow.")]
        [SerializeField, Range(0f, 1f)]
        private float _shadowStrength = 0.65f;

        [Tooltip("Distance between shadow samples, in cells. Smaller is more accurate and slower.")]
        [SerializeField, Range(0.05f, 1f)]
        private float _shadowStepCells = 0.2f;

        [Tooltip(
            "How far a shadow can reach, in cells. A low sun casts long shadows and needs more."
        )]
        [SerializeField, Range(1f, 64f)]
        private float _shadowReachCells = 24f;

        [Tooltip(
            "Offset off each surface before sampling, in cells, so a surface never shadows itself."
        )]
        [SerializeField, Range(0f, 0.5f)]
        private float _shadowBiasCells = 0.05f;

        [Header("Clouds")]
        [Tooltip(
            "Cloud shadows drifting over the city. Only lighting: nothing cloud-shaped is drawn. A forecast sets these through Clouds."
        )]
        [SerializeField, InlineProperty, HideLabel]
        private CityClouds _clouds = new CityClouds();

        [Header("Rain")]
        [Tooltip(
            "Overcast light and wet surfaces. The campaign sets Rain.Intensity from the day's rain roll; scrub it here to preview."
        )]
        [SerializeField, InlineProperty, HideLabel]
        private CityRain _rain = new CityRain();

        // The forecast hook: assign a whole cloud setting, and the sun applies it from the next frame.
        public CityClouds Clouds
        {
            get => _clouds;
            set => _clouds = value ?? new CityClouds();
        }

        public CityRain Rain => _rain;

        #region Lifecycle

        private void OnEnable()
        {
            Apply();
        }

        private void OnValidate()
        {
            Apply();
        }

        private void Update()
        {
            if (Application.isPlaying && _previewSecondsPerDay > 0f)
            {
                _minuteOfDay = Mathf.Repeat(
                    _minuteOfDay + (Time.deltaTime * MINUTES_PER_DAY / _previewSecondsPerDay),
                    MINUTES_PER_DAY
                );
            }

            Apply();
        }

        #endregion

        #region Private API

        private static Gradient ThreeKeys(Color start, Color middle, Color end)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(start, 0f),
                    new GradientColorKey(middle, 0.5f),
                    new GradientColorKey(end, 1f),
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
            );
            return gradient;
        }

        private void Apply()
        {
            // 0 at sunrise, 1 at sunset; outside that range the sun is down.
            float dayProgress =
                (_minuteOfDay - _sunriseMinute) / Mathf.Max(1f, _sunsetMinute - _sunriseMinute);
            bool isUp = dayProgress > 0f && dayProgress < 1f;
            float daylight = isUp ? Mathf.Sin(dayProgress * Mathf.PI) : 0f;
            float elevation = isUp ? daylight * _middayElevation : -10f;
            float azimuth = Mathf.Lerp(_sunriseAzimuth, _sunsetAzimuth, Mathf.Clamp01(dayProgress));

            float elevationRadians = elevation * Mathf.Deg2Rad;
            float azimuthRadians = azimuth * Mathf.Deg2Rad;
            Vector3 toSunGrid = new Vector3(
                Mathf.Cos(elevationRadians) * Mathf.Cos(azimuthRadians),
                Mathf.Cos(elevationRadians) * Mathf.Sin(azimuthRadians),
                Mathf.Sin(elevationRadians)
            );

            // The sun fades out over its last few degrees above the horizon.
            // Rain turns the light overcast: dimmer sun, cool grey ambient, faint shadows, windows lit.
            float rain = _rain.Intensity;
            Color sunColor =
                _sunColor.Evaluate(Mathf.Clamp01(dayProgress))
                * Mathf.SmoothStep(0f, 1f, elevation / 8f)
                * (1f - (rain * _rain.SunDim));
            Color ambient =
                _ambientColor.Evaluate(daylight) * Color.Lerp(Color.white, _rain.AmbientTint, rain);
            float emission =
                Mathf.Lerp(_emission.x, _emission.y, daylight) * Mathf.Lerp(1f, _rain.EmissionBoost, rain);
            float shadowStrength = _shadowStrength * Mathf.Lerp(1f, _rain.ShadowScale, rain);
            Shader.SetGlobalVector(SunDirectionId, IsoGrid.GridToView(toSunGrid).normalized);
            Shader.SetGlobalVector(SunDirectionGridId, toSunGrid);
            Shader.SetGlobalColor(SunColorId, sunColor);
            Shader.SetGlobalColor(AmbientColorId, ambient);
            Shader.SetGlobalFloat(EmissionScaleId, emission);
            Shader.SetGlobalVector(
                ShadowId,
                new Vector4(shadowStrength, _shadowStepCells, _shadowReachCells, _shadowBiasCells)
            );
            _clouds.Apply(_rain.Overcast, rain);
            _rain.Apply();
        }

        #endregion
    }
}
