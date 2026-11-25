using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.City
{
    // Minutes of the day as a 24-hour clock, for inspector labels.
    public static class CityClock
    {
        public static string Format(float minuteOfDay)
        {
            int minutes = Mathf.FloorToInt(Mathf.Repeat(minuteOfDay, 1440f));
            return $"{minutes / 60:00}:{minutes % 60:00}";
        }
    }

    // Cloud cover for the city's light (city-generator.md §6.4): coverage, edge softness, how much sun a cloud blocks,
    // cloud size and wind. Plain data so a future forecast can author one per weather and hand it to CitySky.Clouds.
    [Serializable]
    public sealed class CityClouds
    {
        private static readonly int CloudsId = Shader.PropertyToID("_CityClouds");
        private static readonly int CloudWindId = Shader.PropertyToID("_CityCloudWind");
        private static readonly int CloudLightId = Shader.PropertyToID("_CityCloudLight");

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

        [Tooltip("Typical cloud size.")]
        [SerializeField, Min(0.5f), SuffixLabel("cells", true)]
        private float _sizeCells = 12f;

        [Tooltip(
            "Direction the clouds drift toward, around the grid: 0 = +row (up-right), 90 = +column (down-right)."
        )]
        [SerializeField, Range(0f, 360f), SuffixLabel("°", true)]
        private float _windAzimuth = 30f;

        [Tooltip("Drift speed. Shows in the Scene view with Always Refresh on, or in Play mode.")]
        [SerializeField, Min(0f), SuffixLabel("cells/s", true)]
        private float _windCellsPerSecond = 0.6f;

        [Tooltip(
            "City light bouncing off cloud undersides back onto the streets, added under cloud by the night emission scale: a warm sodium glow on cloudy nights."
        )]
        [SerializeField]
        private Color _cityBounce = new Color(0.22f, 0.15f, 0.1f);

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
            Shader.SetGlobalColor(CloudLightId, Color.Lerp(_cityBounce, toward._cityBounce, blend));
        }
    }

    // The night light (city-generator.md §6.1): while the sun is down, CitySky sends the moon as its light, so
    // shadows and cloud shade work by moonlight with no shader changes. The moon's path wraps past midnight.
    [Serializable]
    public sealed class CityMoon
    {
        [Tooltip("Minute the moon rises, usually after sunset.")]
        [SerializeField, Range(0, 1439), SuffixLabel("@Clock(_riseMinute)", true)]
        private int _riseMinute = 1140;

        [Tooltip("Minute the moon sets, usually before sunrise; past midnight is fine.")]
        [SerializeField, Range(0, 1439), SuffixLabel("@Clock(_setMinute)", true)]
        private int _setMinute = 300;

        [Tooltip("Moon elevation above the ground at the middle of its night.")]
        [SerializeField, Range(5f, 89f), SuffixLabel("°", true)]
        private float _peakElevation = 50f;

        [Tooltip("Direction the moon shines from at moonrise, same angles as the sun's.")]
        [SerializeField, Range(0f, 360f), SuffixLabel("°", true)]
        private float _riseAzimuth = 160f;

        [Tooltip("Direction the moon shines from at moonset. It sweeps between the two through the night.")]
        [SerializeField, Range(0f, 360f), SuffixLabel("°", true)]
        private float _setAzimuth = 20f;

        [Tooltip("Moonlight colour at full moon: a cool blue-white, much dimmer than the sun.")]
        [SerializeField]
        private Color _color = new Color(0.32f, 0.4f, 0.6f);

        [Tooltip("Phase: 0 = new moon (no moonlight), 1 = full moon.")]
        [SerializeField, Range(0f, 1f)]
        private float _phase = 1f;

        public float Phase
        {
            get => _phase;
            set => _phase = Mathf.Clamp01(value);
        }

        private static string Clock(float minute) => CityClock.Format(minute);

        // Toward the moon in (row, column, up) and its colour; elevation below 0 when it is down.
        public (float elevation, float azimuth, Color color) At(float minuteOfDay)
        {
            float span = Mathf.Max(1f, Mathf.Repeat(_setMinute - _riseMinute, 1440f));
            float progress = Mathf.Repeat(minuteOfDay - _riseMinute, 1440f) / span;
            if (progress >= 1f)
            {
                return (-10f, _riseAzimuth, Color.black);
            }

            float elevation = Mathf.Sin(progress * Mathf.PI) * _peakElevation;
            float azimuth = Mathf.Lerp(_riseAzimuth, _setAzimuth, progress);
            return (elevation, azimuth, _color * _phase);
        }
    }

    // Rain on the city's light and surfaces (city-generator.md §6.3). Cosmetic only: the campaign's seeded day roll
    // decides whether it rains and sets Intensity; the map shows rain everywhere when any district is rainy.
    [Serializable]
    public sealed class CityRain
    {
        private static readonly int RainId = Shader.PropertyToID("_CityRain");
        private static readonly int RainSurfaceId = Shader.PropertyToID("_CityRainSurface");
        private static readonly int RainSeedId = Shader.PropertyToID("_CityRainSeed");

        [Tooltip("How hard it rains: 0 = dry, 1 = full rain. The campaign sets this; scrub it to preview.")]
        [SerializeField, Range(0f, 1f)]
        private float _intensity;

        [Tooltip("Time for surfaces to soak or dry after the intensity changes, in Play mode. Edit mode follows it at once.")]
        [SerializeField, Min(0f), SuffixLabel("s", true)]
        private float _soakSeconds = 4f;

        [Title("Overcast light")]
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
        [SerializeField, Min(0f), SuffixLabel("x", true)]
        private float _emissionBoost = 2f;

        [Tooltip("Clouds at full rain. The sky's own clouds blend toward these as intensity rises.")]
        [SerializeField, FoldoutGroup("Overcast clouds"), InlineProperty, HideLabel]
        private CityClouds _overcast = new CityClouds(0.95f, 0.8f, 0.5f, 20f);

        [Title("Wet surfaces")]
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

        [Tooltip("Typical puddle size.")]
        [SerializeField, Min(0.1f), SuffixLabel("cells", true)]
        private float _puddleSizeCells = 1.2f;

        [Tooltip(
            "Shifts where puddles form. The campaign sets it from the run seed so each run puddles differently; change it here to preview."
        )]
        [SerializeField]
        private int _puddleSeed;

        [Tooltip("Strength of window glow smeared down wet roads, like reflections on a wet street at night.")]
        [SerializeField, Range(0f, 2f)]
        private float _glowSmear = 0.6f;

        [Tooltip("How far the glow smear stretches down the screen.")]
        [SerializeField, Range(0f, 4f), SuffixLabel("cells", true)]
        private float _glowSmearCells = 1.5f;

        [NonSerialized]
        private float _wetness;

        public int PuddleSeed
        {
            get => _puddleSeed;
            set => _puddleSeed = value;
        }

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
            Shader.SetGlobalVector(RainSeedId, PuddleOffset(_puddleSeed));
        }

        // A spot in the puddle noise picked by the seed: an integer hash, so nearby seeds land far apart. Kept under
        // 1000 noise cells so the shader's floats stay precise.
        private static Vector4 PuddleOffset(int seed)
        {
            uint hash = unchecked((uint)seed * 2654435761u);
            hash ^= hash >> 16;
            hash = unchecked(hash * 2246822519u);
            hash ^= hash >> 13;
            return new Vector4((hash & 0xFFFF) / 65.536f, (hash >> 16) / 65.536f, 0f, 0f);
        }
    }

    // The city's sky (city-generator.md §6): one light (the sun by day, the moon by night), ambient colour, window
    // emission, shadow tuning, clouds and rain, set as shader globals for every half-cube surface. One per scene; the
    // last one enabled wins.
    [ExecuteAlways]
    public sealed class CitySky : MonoBehaviour
    {
        private const float MINUTES_PER_DAY = 1440f;
        private const string TABS = "Tabs";
        private static readonly int LightDirectionId = Shader.PropertyToID("_CityLightDirection");
        private static readonly int LightDirectionGridId = Shader.PropertyToID(
            "_CityLightDirectionGrid"
        );
        private static readonly int LightColorId = Shader.PropertyToID("_CityLightColor");
        private static readonly int AmbientColorId = Shader.PropertyToID("_CityAmbientColor");
        private static readonly int EmissionScaleId = Shader.PropertyToID("_CityEmissionScale");
        private static readonly int ShadowId = Shader.PropertyToID("_CityShadow");

        [Tooltip("Minute of the day: 480 = 08:00, 960 = 16:00. Scrub it to preview any hour.")]
        [SerializeField, Range(0f, 1439f), SuffixLabel("@Clock(_minuteOfDay)", true)]
        [BoxGroup("Time")]
        private float _minuteOfDay = 600f;

        [Tooltip("Seconds for a whole day in Play mode, for previews. 0 holds the time still.")]
        [SerializeField, Min(0f), SuffixLabel("s / day", true)]
        [BoxGroup("Time")]
        private float _previewSecondsPerDay;

        [Tooltip("Minute the sun rises.")]
        [SerializeField, Range(0, 1439), SuffixLabel("@Clock(_sunriseMinute)", true)]
        [TabGroup(TABS, "Sun")]
        private int _sunriseMinute = 360;

        [Tooltip("Minute the sun sets.")]
        [SerializeField, Range(1, 1440), SuffixLabel("@Clock(_sunsetMinute)", true)]
        [TabGroup(TABS, "Sun")]
        private int _sunsetMinute = 1080;

        [Tooltip("Sun elevation above the ground at midday.")]
        [SerializeField, Range(5f, 89f), SuffixLabel("°", true)]
        [TabGroup(TABS, "Sun")]
        private float _middayElevation = 60f;

        [Tooltip(
            "Direction the sun shines from at sunrise, around the grid: 0 = +row (behind, up-right), 90 = +column (front, down-right), 180 = front-left, 270 = upper-left."
        )]
        [SerializeField, Range(0f, 360f), SuffixLabel("°", true)]
        [TabGroup(TABS, "Sun")]
        private float _sunriseAzimuth = 200f;

        [Tooltip(
            "Direction the sun shines from at sunset, same angles as sunrise. The sun sweeps between the two through the day."
        )]
        [SerializeField, Range(0f, 360f), SuffixLabel("°", true)]
        [TabGroup(TABS, "Sun")]
        private float _sunsetAzimuth = 340f;

        [Tooltip("Sun colour from sunrise (left) to sunset (right).")]
        [SerializeField]
        [TabGroup(TABS, "Sun")]
        private Gradient _sunColor = ThreeKeys(
            new Color(1f, 0.62f, 0.38f),
            new Color(1f, 0.95f, 0.86f),
            new Color(1f, 0.55f, 0.32f)
        );

        [Tooltip(
            "Ambient colour from night (left) to midday (right). Materials scale it by their Ambient Strength."
        )]
        [SerializeField]
        [TabGroup(TABS, "Sun")]
        private Gradient _ambientColor = ThreeKeys(
            new Color(0.32f, 0.38f, 0.62f),
            new Color(0.8f, 0.78f, 0.82f),
            new Color(1f, 0.97f, 0.9f)
        );

        [Tooltip("Window emission multiplier: X at night, Y at midday.")]
        [SerializeField, LabelText("Window Emission (Night, Midday)")]
        [TabGroup(TABS, "Sun")]
        private Vector2 _emission = new Vector2(1.6f, 0.05f);

        [Tooltip("Light while the sun is down: path, colour and phase. Shadows and cloud shade follow it.")]
        [SerializeField, InlineProperty, HideLabel]
        [TabGroup(TABS, "Moon")]
        private CityMoon _moon = new CityMoon();

        [Tooltip("How dark shadowed light gets: 0 = no shadows, 1 = no direct light at all in shadow.")]
        [SerializeField, Range(0f, 1f)]
        [TabGroup(TABS, "Shadows")]
        private float _shadowStrength = 0.65f;

        [Tooltip("Distance between shadow samples. Smaller is more accurate and slower.")]
        [SerializeField, Range(0.05f, 1f), SuffixLabel("cells", true)]
        [TabGroup(TABS, "Shadows")]
        private float _shadowStepCells = 0.2f;

        [Tooltip("How far a shadow can reach. A low sun casts long shadows and needs more.")]
        [SerializeField, Range(1f, 64f), SuffixLabel("cells", true)]
        [TabGroup(TABS, "Shadows")]
        private float _shadowReachCells = 24f;

        [Tooltip("Offset off each surface before sampling, so a surface never shadows itself.")]
        [SerializeField, Range(0f, 0.5f), SuffixLabel("cells", true)]
        [TabGroup(TABS, "Shadows")]
        private float _shadowBiasCells = 0.05f;

        [Tooltip(
            "Cloud shadows drifting over the city. Only lighting: nothing cloud-shaped is drawn. A forecast sets these through Clouds."
        )]
        [SerializeField, InlineProperty, HideLabel]
        [TabGroup(TABS, "Clouds")]
        private CityClouds _clouds = new CityClouds();

        [Tooltip(
            "Overcast light and wet surfaces. The campaign sets Rain.Intensity from the day's rain roll; scrub it here to preview."
        )]
        [SerializeField, InlineProperty, HideLabel]
        [TabGroup(TABS, "Rain")]
        private CityRain _rain = new CityRain();

        [NonSerialized]
        private string _lightReadout = "";

        // The forecast hook: assign a whole cloud setting, and the sky applies it from the next frame.
        public CityClouds Clouds
        {
            get => _clouds;
            set => _clouds = value ?? new CityClouds();
        }

        public CityRain Rain => _rain;
        public CityMoon Moon => _moon;

        // The time the sky shows, 0 to 1439. The campaign sets it from the run's clock every frame.
        public float MinuteOfDay
        {
            get => _minuteOfDay;
            set => _minuteOfDay = Mathf.Repeat(value, MINUTES_PER_DAY);
        }

        [ShowInInspector, ReadOnly, LabelText("Light Now")]
        [BoxGroup("Time"), PropertyOrder(1)]
        private string LightReadout => _lightReadout;

        private static string Clock(float minute) => CityClock.Format(minute);

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

        #region Inspector presets

        [ButtonGroup("Time/Presets"), PropertyOrder(2)]
        private void Dawn() => Preset("Dawn", () => _minuteOfDay = _sunriseMinute + 30);

        [ButtonGroup("Time/Presets")]
        private void Morning() => Preset("Morning", () => _minuteOfDay = 480f);

        [ButtonGroup("Time/Presets")]
        private void Noon() => Preset("Noon", () => _minuteOfDay = 720f);

        [ButtonGroup("Time/Presets")]
        private void Afternoon() => Preset("Afternoon", () => _minuteOfDay = 960f);

        [ButtonGroup("Time/Presets")]
        private void Dusk() => Preset("Dusk", () => _minuteOfDay = _sunsetMinute - 30);

        [ButtonGroup("Time/Presets")]
        private void Night() => Preset("Night", () => _minuteOfDay = 1320f);

        [ButtonGroup(TABS + "/Moon/Phase"), PropertyOrder(-1), LabelText("New")]
        private void NewMoon() => Preset("New Moon", () => _moon.Phase = 0f);

        [ButtonGroup(TABS + "/Moon/Phase"), LabelText("Half")]
        private void HalfMoon() => Preset("Half Moon", () => _moon.Phase = 0.5f);

        [ButtonGroup(TABS + "/Moon/Phase"), LabelText("Full")]
        private void FullMoon() => Preset("Full Moon", () => _moon.Phase = 1f);

        [ButtonGroup(TABS + "/Rain/Weather"), PropertyOrder(-1)]
        private void Dry() => Preset("Dry", () => _rain.Intensity = 0f);

        [ButtonGroup(TABS + "/Rain/Weather")]
        private void Drizzle() => Preset("Drizzle", () => _rain.Intensity = 0.4f);

        [ButtonGroup(TABS + "/Rain/Weather"), LabelText("Rain")]
        private void Downpour() => Preset("Rain", () => _rain.Intensity = 1f);

        // A preset button's change: undoable and saved in the editor, applied at once.
        private void Preset(string name, Action change)
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(this, $"Sky: {name}");
#endif
            change();
            Apply();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
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
            Color lightColor = _sunColor.Evaluate(Mathf.Clamp01(dayProgress));
            if (!isUp)
            {
                // ponytail: one light at a time, so the moon takes over at sunset and an overlap shows only the sun;
                // send two directions and march shadows twice if dusk ever needs both.
                (elevation, azimuth, lightColor) = _moon.At(_minuteOfDay);
            }

            _lightReadout =
                elevation > 0f ? $"{(isUp ? "Sun" : "Moon")} {elevation:0}° high, from {azimuth:0}°" : "None (dark)";

            float elevationRadians = elevation * Mathf.Deg2Rad;
            float azimuthRadians = azimuth * Mathf.Deg2Rad;
            Vector3 toLightGrid = new Vector3(
                Mathf.Cos(elevationRadians) * Mathf.Cos(azimuthRadians),
                Mathf.Cos(elevationRadians) * Mathf.Sin(azimuthRadians),
                Mathf.Sin(elevationRadians)
            );

            // Sun or moon fades out over its last few degrees above the horizon.
            // Rain turns the light overcast: dimmer light, cool grey ambient, faint shadows, windows lit.
            float rain = _rain.Intensity;
            Color directColor =
                lightColor
                * Mathf.SmoothStep(0f, 1f, elevation / 8f)
                * (1f - (rain * _rain.SunDim));
            Color ambient =
                _ambientColor.Evaluate(daylight) * Color.Lerp(Color.white, _rain.AmbientTint, rain);
            float emission =
                Mathf.Lerp(_emission.x, _emission.y, daylight) * Mathf.Lerp(1f, _rain.EmissionBoost, rain);
            float shadowStrength = _shadowStrength * Mathf.Lerp(1f, _rain.ShadowScale, rain);
            Shader.SetGlobalVector(LightDirectionId, IsoGrid.GridToView(toLightGrid).normalized);
            Shader.SetGlobalVector(LightDirectionGridId, toLightGrid);
            Shader.SetGlobalColor(LightColorId, directColor);
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
