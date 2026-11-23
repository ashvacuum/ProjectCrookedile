using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Crookedile.Art.Iso
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class HalfCubeLightLink : MonoBehaviour
    {
        private static readonly int KeyLightDirectionId = Shader.PropertyToID("_KeyLightDirection");
        private static readonly int NormalFlipXId = Shader.PropertyToID("_NormalFlipX");
        private static readonly int NormalFlipYId = Shader.PropertyToID("_NormalFlipY");

        [Tooltip("The 2D point light that drives the mesh shading and casts its silhouette shadow.")]
        [SerializeField] private Light2D _light;
        [Tooltip("Keep the spotlight aimed at this object as the light moves.")]
        [SerializeField] private bool _aimLightAtObject = true;
        [Tooltip("Invert the tangent-space normal X channel when this tile is mirrored horizontally.")]
        [SerializeField] private bool _invertNormalX;
        [Tooltip("Invert the tangent-space normal Y channel when this tile is mirrored vertically.")]
        [SerializeField] private bool _invertNormalY;

        private Renderer _renderer;
        private MaterialPropertyBlock _propertyBlock;
        private Vector3 _lastLightPosition;
        private Vector3 _lastObjectPosition;
        private bool _lastInvertNormalX;
        private bool _lastInvertNormalY;
        private bool _hasCachedValues;

        private void OnEnable()
        {
            CacheRenderer();
            ApplyLightAndNormalSettings();
        }

        private void Update()
        {
            if (_renderer == null)
            {
                CacheRenderer();
            }

            if (_renderer == null)
            {
                return;
            }

            Vector3 lightPosition = _light != null ? _light.transform.position : Vector3.zero;
            Vector3 objectPosition = transform.position;
            if (
                _hasCachedValues
                && lightPosition == _lastLightPosition
                && objectPosition == _lastObjectPosition
                && _invertNormalX == _lastInvertNormalX
                && _invertNormalY == _lastInvertNormalY
            )
            {
                return;
            }

            ApplyLightAndNormalSettings();
        }

        public void SetLight(Light2D light)
        {
            _light = light;
            _hasCachedValues = false;
            ApplyLightAndNormalSettings();
        }

        private void CacheRenderer()
        {
            TryGetComponent(out _renderer);
            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }
        }

        private void ApplyLightAndNormalSettings()
        {
            if (_renderer == null)
            {
                return;
            }

            _renderer.GetPropertyBlock(_propertyBlock);
            if (_light != null)
            {
                Vector3 lightPosition = _light.transform.position;
                Vector3 objectPosition = transform.position;
                Vector2 aimDirection = (Vector2)objectPosition - (Vector2)lightPosition;
                if (_aimLightAtObject && aimDirection.sqrMagnitude > 0f)
                {
                    _light.transform.up = aimDirection.normalized;
                }

                Vector3 direction = lightPosition - objectPosition;
                if (direction.sqrMagnitude > 0f)
                {
                    direction.Normalize();
                }
                else
                {
                    direction = Vector3.back;
                }

                _propertyBlock.SetVector(KeyLightDirectionId, direction);
            }

            _propertyBlock.SetFloat(NormalFlipXId, _invertNormalX ? -1f : 1f);
            _propertyBlock.SetFloat(NormalFlipYId, _invertNormalY ? -1f : 1f);
            _renderer.SetPropertyBlock(_propertyBlock);

            _lastLightPosition = _light != null ? _light.transform.position : Vector3.zero;
            _lastObjectPosition = transform.position;
            _lastInvertNormalX = _invertNormalX;
            _lastInvertNormalY = _invertNormalY;
            _hasCachedValues = true;
        }
    }
}
