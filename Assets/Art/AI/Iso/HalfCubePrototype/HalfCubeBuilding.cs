using UnityEngine;

namespace Crookedile.Art.Iso
{
    // Builds the half-cube mesh (roof diamond plus left and right walls) from a footprint in iso tiles.
    // The front-bottom corner stays fixed, so the building grows back and up from the tile it stands on.
    // Normals are the faces' true 3D directions and every vertex carries its height, so CitySky lighting and the
    // height-aware shadows treat it as the box it stands for (city-generator.md §6).
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class HalfCubeBuilding : MonoBehaviour
    {
        // The right wall runs along +row and the left wall along -column, so a footprint grows back from its front cell.
        private static readonly Vector3 RightAxis = IsoGrid.RowAxis;
        private static readonly Vector3 LeftAxis = -IsoGrid.ColumnAxis;

        // The pivot is the front tile's centre, so a 1x1 building's base is exactly that tile's diamond.
        private const float FRONT_GROUND_Y = -IsoGrid.HALF_HEIGHT;

        [Tooltip("Footprint length in tiles along the right wall.")]
        [SerializeField, Min(0.25f)]
        private float _rightTiles = 1f;

        [Tooltip("Footprint length in tiles along the left wall.")]
        [SerializeField, Min(0.25f)]
        private float _leftTiles = 1f;

        [Tooltip(
            "Wall height in world units, measured up from the tile surface. 0.5 is one storey of the face atlas."
        )]
        [SerializeField, Min(0.05f)]
        private float _wallHeight = 0.5f;

        private Mesh _mesh;

        public void Configure(float rightTiles, float leftTiles, float wallHeight)
        {
            _rightTiles = Mathf.Max(0.25f, rightTiles);
            _leftTiles = Mathf.Max(0.25f, leftTiles);
            _wallHeight = Mathf.Max(0.05f, wallHeight);
            if (_mesh != null)
            {
                Rebuild();
            }
        }

        private void OnEnable()
        {
            if (_mesh == null)
            {
                _mesh = new Mesh { name = "Half Cube Building", hideFlags = HideFlags.DontSave };
            }

            Rebuild();
            GetComponent<MeshFilter>().sharedMesh = _mesh;
        }

        private void OnValidate()
        {
            // OnEnable owns the first build; assigning the MeshFilter from OnValidate makes Unity warn.
            if (_mesh != null)
            {
                Rebuild();
            }
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
            {
                Destroy(_mesh);
            }
            else
            {
                DestroyImmediate(_mesh);
            }

            _mesh = null;
        }

        private void Rebuild()
        {
            Vector3 front = new Vector3(0f, FRONT_GROUND_Y + _wallHeight, 0f);
            Vector3 right = front + (RightAxis * _rightTiles);
            Vector3 left = front + (LeftAxis * _leftTiles);
            Vector3 peak = right + (LeftAxis * _leftTiles);
            Vector3 down = Vector3.down * _wallHeight;
            float top = _wallHeight;

            Vector3[] vertices =
            {
                left,
                peak,
                right,
                front,
                left,
                front,
                front + down,
                left + down,
                front,
                right,
                right + down,
                front + down,
            };

            Vector3[] normals = new Vector3[vertices.Length];
            Vector4[] tangents = new Vector4[vertices.Length];
            Vector2[] uvs = new Vector2[vertices.Length];
            Vector2[] faceData = new Vector2[vertices.Length];
            Color[] colors = new Color[vertices.Length];
            // ponytail: each face stretches its whole atlas cell; tile UVs with frac() in the shader when facades need repeating storeys.
            // Tangents follow each face's uv.x; the walls' uv.y runs up, which is -cross(normal, tangent), hence w = -1.
            SetFace(
                normals,
                tangents,
                uvs,
                faceData,
                0,
                IsoGrid.UpView,
                IsoGrid.RowView,
                1f,
                0f,
                false,
                top,
                top,
                top,
                top
            );
            SetFace(
                normals,
                tangents,
                uvs,
                faceData,
                4,
                -IsoGrid.RowView,
                IsoGrid.ColumnView,
                -1f,
                1f,
                true,
                top,
                top,
                0f,
                0f
            );
            SetFace(
                normals,
                tangents,
                uvs,
                faceData,
                8,
                IsoGrid.ColumnView,
                IsoGrid.RowView,
                -1f,
                2f,
                true,
                top,
                top,
                0f,
                0f
            );
            for (int index = 0; index < colors.Length; index++)
            {
                colors[index] = Color.white;
            }

            _mesh.Clear();
            _mesh.vertices = vertices;
            _mesh.normals = normals;
            _mesh.tangents = tangents;
            _mesh.uv = uvs;
            _mesh.SetUVs(1, faceData);
            _mesh.colors = colors;
            _mesh.triangles = new[] { 0, 1, 2, 0, 2, 3, 4, 5, 6, 4, 6, 7, 8, 9, 10, 8, 10, 11 };
            _mesh.RecalculateBounds();
        }

        // faceData is (face id, height above the ground in world units) per vertex, read by the shader.
        private static void SetFace(
            Vector3[] normals,
            Vector4[] tangents,
            Vector2[] uvs,
            Vector2[] faceData,
            int start,
            Vector3 normal,
            Vector3 tangent,
            float tangentSign,
            float faceId,
            bool invertV,
            params float[] heights
        )
        {
            Vector2[] faceUvs =
            {
                new Vector2(0f, invertV ? 1f : 0f),
                new Vector2(1f, invertV ? 1f : 0f),
                new Vector2(1f, invertV ? 0f : 1f),
                new Vector2(0f, invertV ? 0f : 1f),
            };

            for (int index = 0; index < 4; index++)
            {
                normals[start + index] = normal;
                tangents[start + index] = new Vector4(tangent.x, tangent.y, tangent.z, tangentSign);
                uvs[start + index] = faceUvs[index];
                faceData[start + index] = new Vector2(faceId, heights[index]);
            }
        }
    }
}
