using UnityEngine;

namespace Crookedile.Art.Iso
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class CityRoadTile : MonoBehaviour
    {
        private static readonly int RoofAtlasRectId = Shader.PropertyToID("_RoofAtlasRect");
        private static readonly int LeftAtlasRectId = Shader.PropertyToID("_LeftAtlasRect");
        private static readonly int RightAtlasRectId = Shader.PropertyToID("_RightAtlasRect");
        private static readonly int AtlasQuarterTurnsId = Shader.PropertyToID("_AtlasQuarterTurns");
        private static readonly int AtlasFlipXId = Shader.PropertyToID("_AtlasFlipX");
        private static readonly int AtlasFlipYId = Shader.PropertyToID("_AtlasFlipY");
        private const float ATLAS_CELL_SIZE = 0.25f;
        private const float ATLAS_CELL_INSET = 0.01f;

        [Tooltip("Atlas row of this tile's cell in the 4x4 road atlas, counted from the top.")]
        [SerializeField, Range(0, 3)] private int _atlasRow;
        [Tooltip("Atlas column of this tile's cell in the 4x4 road atlas, counted from the left.")]
        [SerializeField, Range(0, 3)] private int _atlasColumn;
        [Tooltip("Rotate the atlas cell by this many quarter turns.")]
        [SerializeField, Range(0, 3)] private int _atlasQuarterTurns;
        [Tooltip("Mirror the atlas cell horizontally.")]
        [SerializeField] private bool _flipAtlasX;
        [Tooltip("Mirror the atlas cell vertically.")]
        [SerializeField] private bool _flipAtlasY;

        private Renderer _renderer;
        private MaterialPropertyBlock _propertyBlock;

        private void OnEnable()
        {
            ApplyAtlasCell();
        }

        private void OnValidate()
        {
            ApplyAtlasCell();
        }

        public void SetAtlasCell(int row, int column, int quarterTurns)
        {
            _atlasRow = Mathf.Clamp(row, 0, 3);
            _atlasColumn = Mathf.Clamp(column, 0, 3);
            _atlasQuarterTurns = ((quarterTurns % 4) + 4) % 4;
            ApplyAtlasCell();
        }

        private void ApplyAtlasCell()
        {
            if (_renderer == null)
            {
                TryGetComponent(out _renderer);
            }

            if (_renderer == null)
            {
                return;
            }

            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }

            float x = _atlasColumn * ATLAS_CELL_SIZE;
            float y = 0.75f - (_atlasRow * ATLAS_CELL_SIZE);
            float inset = ATLAS_CELL_SIZE * ATLAS_CELL_INSET;
            Vector4 atlasRect = new Vector4(x + inset, y + inset, ATLAS_CELL_SIZE - (inset * 2f), ATLAS_CELL_SIZE - (inset * 2f));

            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetVector(RoofAtlasRectId, atlasRect);
            _propertyBlock.SetVector(LeftAtlasRectId, atlasRect);
            _propertyBlock.SetVector(RightAtlasRectId, atlasRect);
            _propertyBlock.SetFloat(AtlasQuarterTurnsId, _atlasQuarterTurns);
            _propertyBlock.SetFloat(AtlasFlipXId, _flipAtlasX ? 1f : 0f);
            _propertyBlock.SetFloat(AtlasFlipYId, _flipAtlasY ? 1f : 0f);
            _renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
