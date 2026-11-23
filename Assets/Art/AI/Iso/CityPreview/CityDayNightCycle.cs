using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Crookedile.Art.Iso
{
    // Runs in edit mode so scrubbing _timeOfDay in the Inspector relights the scene; time only advances in Play mode.
    [ExecuteAlways]
    public sealed class CityDayNightCycle : MonoBehaviour
    {
        private static readonly int KeyLightColorId = Shader.PropertyToID("_KeyLightColor");
        private static readonly int AmbientStrengthId = Shader.PropertyToID("_AmbientStrength");
        private static readonly int DiffuseStrengthId = Shader.PropertyToID("_DiffuseStrength");
        private static readonly int EmissionStrengthId = Shader.PropertyToID("_EmissionStrength");
        private const float FULL_CYCLE_RADIANS = Mathf.PI * 2f;

        [Tooltip("Seconds for one complete day and night cycle.")]
        [SerializeField, Min(4f)] private float _cycleDurationSeconds = 30f;
        [Tooltip("0 is midday; 0.5 is midnight. Scrub it in edit mode to preview any hour.")]
        [SerializeField, Range(0f, 1f)] private float _timeOfDay;
        [Tooltip("The orbit radius of the moving key light in X and Y.")]
        [SerializeField] private Vector2 _lightOrbitRadius = new Vector2(2.2f, 1.2f);

        [Header("Look (left of each gradient is midnight, right is midday)")]
        [Tooltip("Key Light2D color across the day.")]
        [SerializeField] private Gradient _keyLightColor = NightToDay(new Color(0.28f, 0.42f, 0.82f), new Color(1f, 0.8f, 0.58f));
        [Tooltip("Global ambient Light2D color across the day.")]
        [SerializeField] private Gradient _ambientLightColor = NightToDay(new Color(0.38f, 0.46f, 0.72f), new Color(1f, 0.97f, 0.88f));
        [Tooltip("Camera background color across the day.")]
        [SerializeField] private Gradient _skyColor = NightToDay(new Color(0.025f, 0.04f, 0.12f), new Color(0.53f, 0.72f, 0.86f));
        [Tooltip("Key Light2D intensity: X at midnight, Y at midday. Also scales the shader's key color, so it acts roughly squared.")]
        [SerializeField] private Vector2 _keyLightIntensity = new Vector2(0.12f, 0.9f);
        [Tooltip("Global ambient Light2D intensity: X at midnight, Y at midday.")]
        [SerializeField] private Vector2 _ambientLightIntensity = new Vector2(0.55f, 1f);
        [Tooltip("Shader _AmbientStrength: X at midnight, Y at midday.")]
        [SerializeField] private Vector2 _shaderAmbient = new Vector2(0.1f, 0.42f);
        [Tooltip("Shader _DiffuseStrength: X at midnight, Y at midday.")]
        [SerializeField] private Vector2 _shaderDiffuse = new Vector2(0.18f, 0.8f);
        [Tooltip("Shader _EmissionStrength (lit windows): X at midnight, Y at midday.")]
        [SerializeField] private Vector2 _shaderEmission = new Vector2(1.6f, 0.05f);

        [Header("Scene")]
        [SerializeField] private Light2D _keyLight;
        [SerializeField] private Light2D _ambientLight;
        [SerializeField] private Camera _previewCamera;
        [SerializeField] private Renderer[] _litRenderers;
        [SerializeField] private Vector3 _cityCenter;

        private MaterialPropertyBlock _propertyBlock;

        #region Lifecycle

        private void OnEnable()
        {
            ApplyLighting();
        }

        private void OnValidate()
        {
            ApplyLighting();
        }

        private void Update()
        {
            if (!Application.isPlaying || !IsConfigured())
            {
                return;
            }

            _timeOfDay = Mathf.Repeat(_timeOfDay + (Time.deltaTime / Mathf.Max(1f, _cycleDurationSeconds)), 1f);
            ApplyLighting();
        }

        #endregion

        #region Public API

        public void Configure(Light2D keyLight, Light2D ambientLight, Camera previewCamera, Renderer[] litRenderers, Vector3 cityCenter)
        {
            _keyLight = keyLight;
            _ambientLight = ambientLight;
            _previewCamera = previewCamera;
            _litRenderers = litRenderers ?? System.Array.Empty<Renderer>();
            _cityCenter = cityCenter;
            ApplyLighting();
        }

        #endregion

        #region Private API

        private static Gradient NightToDay(Color night, Color day)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(night, 0f), new GradientColorKey(day, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
            );
            return gradient;
        }

        private bool IsConfigured()
        {
            return _keyLight != null && _ambientLight != null && _previewCamera != null && _litRenderers != null;
        }

        private void ApplyLighting()
        {
            if (!IsConfigured())
            {
                return;
            }

            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }

            float phase = _timeOfDay * FULL_CYCLE_RADIANS;
            float daylight = Mathf.SmoothStep(0f, 1f, (Mathf.Cos(phase) + 1f) * 0.5f);
            Vector3 lightPosition = _keyLight.transform.position;
            _keyLight.transform.position = new Vector3(
                _cityCenter.x + (Mathf.Cos(phase) * _lightOrbitRadius.x),
                _cityCenter.y + (Mathf.Sin(phase) * _lightOrbitRadius.y),
                lightPosition.z
            );
            _keyLight.color = _keyLightColor.Evaluate(daylight);
            _keyLight.intensity = Mathf.Lerp(_keyLightIntensity.x, _keyLightIntensity.y, daylight);
            _ambientLight.color = _ambientLightColor.Evaluate(daylight);
            _ambientLight.intensity = Mathf.Lerp(_ambientLightIntensity.x, _ambientLightIntensity.y, daylight);
            _previewCamera.backgroundColor = _skyColor.Evaluate(daylight);

            Color keyColor = _keyLight.color * _keyLight.intensity;
            float ambient = Mathf.Lerp(_shaderAmbient.x, _shaderAmbient.y, daylight);
            float diffuse = Mathf.Lerp(_shaderDiffuse.x, _shaderDiffuse.y, daylight);
            float emission = Mathf.Lerp(_shaderEmission.x, _shaderEmission.y, daylight);
            for (int index = 0; index < _litRenderers.Length; index++)
            {
                Renderer litRenderer = _litRenderers[index];
                if (litRenderer == null)
                {
                    continue;
                }

                litRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(KeyLightColorId, keyColor);
                _propertyBlock.SetFloat(AmbientStrengthId, ambient);
                _propertyBlock.SetFloat(DiffuseStrengthId, diffuse);
                _propertyBlock.SetFloat(EmissionStrengthId, emission);
                litRenderer.SetPropertyBlock(_propertyBlock);
            }
        }

        #endregion
    }
}
