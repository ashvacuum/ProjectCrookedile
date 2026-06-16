using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Data.Audio
{
    /// <summary>
    /// ScriptableObject wrapping an AudioClip with playback settings.
    /// Referenced by SoundLibrary; looked up by string ID from VFXAnimatedImage
    /// AnimationEvents for frame-accurate SFX timing.
    ///
    /// Create via:  Assets → Create → Crookedile → Audio → Audio Clip Data
    /// </summary>
    [CreateAssetMenu(fileName = "NewAudioClipData", menuName = "Crookedile/Audio/Audio Clip Data")]
    public class AudioClipData : ScriptableObject
    {
        [HorizontalGroup("ID")]
        [ReadOnly]
        [HideLabel]
        [SerializeField]
        private string _id;

        /// <summary>Copies the clip ID to the clipboard for pasting into AnimationEvent string fields.</summary>
        [Button("Copy ID", ButtonSizes.Small)]
        [HorizontalGroup("ID", Width = 80)]
        private void CopyIDToClipboard()
        {
            GUIUtility.systemCopyBuffer = _id;
            Debug.Log($"Copied clip ID to clipboard: {_id}");
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_id))
            {
                _id = System.Guid.NewGuid().ToString();
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        private void Reset()
        {
            _id = System.Guid.NewGuid().ToString();
        }
#endif

        [HorizontalGroup("Name")]
        [HideLabel]
        [Tooltip(
            "Human-readable name for code/editor lookups (e.g. \"crack_hit\"). Use AudioManager.PlaySoundByName()."
        )]
        [SerializeField]
        private string _clipName;

        /// <summary>Copies the clip name to the clipboard.</summary>
        [Button("Copy Name", ButtonSizes.Small)]
        [HorizontalGroup("Name", Width = 90)]
        private void CopyNameToClipboard()
        {
            GUIUtility.systemCopyBuffer = _clipName;
            Debug.Log($"Copied clip name to clipboard: {_clipName}");
        }

        [SerializeField]
        private AudioClip _clip;

        [SerializeField, Range(0f, 1f)]
        private float _volume = 1f;

        [Tooltip("Pitch multiplier (1 = normal speed).")]
        [SerializeField]
        private float _pitch = 1f;

        [Tooltip(
            "Category tag for filtering in the Sound Library (e.g. \"combat\", \"ui\", \"vfx\", \"music\")."
        )]
        [SerializeField]
        private string _category = "";

        public string ID => _id;
        public string ClipName => _clipName;
        public AudioClip Clip => _clip;
        public float Volume => _volume;
        public float Pitch => _pitch;
        public string Category => _category;
    }
}
