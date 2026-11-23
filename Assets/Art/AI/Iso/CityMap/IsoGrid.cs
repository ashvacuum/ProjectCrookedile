using UnityEngine;

namespace Crookedile.Art.Iso
{
    // The campaign city's 2:1 iso grid (city-generator.md §2). A cell is (row, column): +row runs up-right on screen,
    // +column runs down-right, and one cell is 1 x 0.5 world units.
    public static class IsoGrid
    {
        public const float HALF_WIDTH = 0.5f;
        public const float HALF_HEIGHT = 0.25f;

        public static readonly Vector3 RowAxis = new Vector3(HALF_WIDTH, HALF_HEIGHT, 0f);
        public static readonly Vector3 ColumnAxis = new Vector3(HALF_WIDTH, -HALF_HEIGHT, 0f);

        public static Vector3 CellToLocal(Vector2Int cell)
        {
            return CellToLocal(cell.x, cell.y);
        }

        // Fractional cells are allowed, so corners and edges can be drawn at half steps.
        public static Vector3 CellToLocal(float row, float column)
        {
            return (RowAxis * row) + (ColumnAxis * column);
        }

        public static Vector2Int LocalToCell(Vector3 local)
        {
            float row = ((local.x / HALF_WIDTH) + (local.y / HALF_HEIGHT)) * 0.5f;
            float column = ((local.x / HALF_WIDTH) - (local.y / HALF_HEIGHT)) * 0.5f;
            return new Vector2Int(Mathf.RoundToInt(row), Mathf.RoundToInt(column));
        }

        // Larger is nearer the camera: a lower screen position draws in front.
        public static int Depth(Vector2Int cell)
        {
            return cell.y - cell.x;
        }
    }
}
