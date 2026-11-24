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

        public void Apply()
        {
            float windRadians = _windAzimuth * Mathf.Deg2Rad;
            Vector2 wind =
                new Vector2(Mathf.Cos(windRadians), Mathf.Sin(windRadians)) * _windCellsPerSecond;
            Shader.SetGlobalVector(
                CloudsId,
                new Vector4(_coverage, _softness, _density, _sizeCells)
            );
            Shader.SetGlobalVector(CloudWindId, new Vector4(wind.x, wind.y, 0f, 0f));
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

        // The forecast hook: assign a whole cloud setting, and the sun applies it from the next frame.
        public CityClouds Clouds
        {
            get => _clouds;
            set => _clouds = value ?? new CityClouds();
        }

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
            Color sunColor =
                _sunColor.Evaluate(Mathf.Clamp01(dayProgress))
                * Mathf.SmoothStep(0f, 1f, elevation / 8f);
            Shader.SetGlobalVector(SunDirectionId, IsoGrid.GridToView(toSunGrid).normalized);
            Shader.SetGlobalVector(SunDirectionGridId, toSunGrid);
            Shader.SetGlobalColor(SunColorId, sunColor);
            Shader.SetGlobalColor(AmbientColorId, _ambientColor.Evaluate(daylight));
            Shader.SetGlobalFloat(EmissionScaleId, Mathf.Lerp(_emission.x, _emission.y, daylight));
            Shader.SetGlobalVector(
                ShadowId,
                new Vector4(_shadowStrength, _shadowStepCells, _shadowReachCells, _shadowBiasCells)
            );
            _clouds.Apply();
        }

        #endregion
    }
}
