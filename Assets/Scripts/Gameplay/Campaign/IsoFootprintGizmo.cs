using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Crookedile.Gameplay.Campaign
{
    /// <summary>
    /// Draws a building's 2:1 footprint diamond in the scene view — the authoring answer to
    /// "is this thing 1×1 or 2×2?", and the scene-view twin of
    /// <c>docs/reference/iso-grid-guide-256x128.png</c>.
    ///
    /// <para>It doubles as a pivot check. The diamond is drawn at the transform's position, and
    /// a correctly authored sprite pivots at its base diamond's centre (`art-bible.md` §9) — so
    /// if the diamond does not sit squarely under the building's base, the pivot is wrong, not
    /// the art. That is worth catching early: pivots decide sort order, so a wrong one draws in
    /// front of things it should be behind.</para>
    ///
    /// Drop it on a candidate sprite, cycle <see cref="_footprint"/> until the diamond matches
    /// the base, and that is the number to put in the zone list.
    /// </summary>
    [ExecuteAlways]
    public class IsoFootprintGizmo : MonoBehaviour
    {
        [Tooltip(
            "Footprint in cells — width along the grid's X, length along its Y. "
                + "The value that goes in the zone list."
        )]
        [SerializeField]
        private Vector2Int _footprint = Vector2Int.one;

        [Tooltip("Cell size, used when this object is not under a Grid. Spec is (1, 0.5).")]
        [SerializeField]
        private Vector2 _fallbackCellSize = new(1f, 0.5f);

        [Tooltip("While selected, also outline nearby footprint sizes for comparison.")]
        [SerializeField]
        private bool _showComparisons = true;

        public Vector2Int Footprint => _footprint;

        /// <summary>Cell size from the owning <see cref="Grid"/>, or the fallback when there is none.</summary>
        private Vector2 CellSize
        {
            get
            {
                var grid = GetComponentInParent<Grid>();
                return grid != null
                    ? new Vector2(grid.cellSize.x, grid.cellSize.y)
                    : _fallbackCellSize;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f, 0.9f, 1f, 0.9f);
            DrawDiamond(_footprint);
        }

        private void OnDrawGizmosSelected()
        {
            var width = Mathf.Max(1, _footprint.x);
            var length = Mathf.Max(1, _footprint.y);

            if (_showComparisons)
            {
                Gizmos.color = new Color(1f, 0.67f, 0f, 0.3f);
                DrawDiamond(new Vector2Int(width + 1, length));
                DrawDiamond(new Vector2Int(width, length + 1));
                if (width > 1)
                    DrawDiamond(new Vector2Int(width - 1, length));
                if (length > 1)
                    DrawDiamond(new Vector2Int(width, length - 1));
            }

            // A W×L footprint's bounding box, from the spec's 256×128 cell: the two axes each
            // contribute half a cell to both dimensions, so both scale with (W + L).
            var span = width + length;
            Handles.color = new Color(0f, 0.9f, 1f, 1f);
            Handles.Label(
                transform.position - new Vector3(0f, CellSize.y * span * 0.5f + 0.15f, 0f),
                $"{width}x{length}  —  {span * 128} x {span * 64} px"
            );
        }

        /// <summary>
        /// Outlines the cells a W×L footprint covers. Square footprints come out as the familiar
        /// diamond; rectangles are parallelograms, which is why this walks the grid's own basis
        /// vectors rather than drawing four points around a centre.
        /// </summary>
        private void DrawDiamond(Vector2Int footprint)
        {
            var cell = CellSize;

            // Unity's isometric grid: +1 cell in X moves (w/2, h/2), +1 in Y moves (-w/2, h/2).
            var stepX = new Vector3(cell.x * 0.5f, cell.y * 0.5f, 0f);
            var stepY = new Vector3(-cell.x * 0.5f, cell.y * 0.5f, 0f);

            var halfX = stepX * (Mathf.Max(1, footprint.x) * 0.5f);
            var halfY = stepY * (Mathf.Max(1, footprint.y) * 0.5f);
            var origin = transform.position;

            var a = origin - halfX - halfY;
            var b = origin + halfX - halfY;
            var c = origin + halfX + halfY;
            var d = origin - halfX + halfY;

            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(b, c);
            Gizmos.DrawLine(c, d);
            Gizmos.DrawLine(d, a);
        }
#endif
    }
}
