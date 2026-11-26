using System.Collections.Generic;
using UnityEngine;

namespace Crookedile.Gameplay.City
{
    // How a building is topped (city-generator.md §6): flat, or a gable with its ridge along the lot's longer side.
    public enum RoofStyle
    {
        Flat,
        Gable,
    }

    // Builds the half-cube mesh (a roof plus the left and right walls) from a footprint in iso tiles. The front-bottom
    // corner stays fixed, so the building grows back and up from the tile it stands on. The roof is a flat diamond or
    // a gable: two slopes meeting at a ridge along the longer side, with the gable-end triangle on the visible short
    // wall. Normals are the faces' true 3D directions and every vertex carries its height, so CitySky lighting and
    // the height-aware shadows treat it as the solid it stands for, and each face marks which of its edges are on the
    // building's outline (silhouette minus the ground edges) for the selection outline.
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class HalfCubeBuilding : MonoBehaviour
    {
        // The right wall runs along +row and the left wall along -column, so a footprint grows back from its front cell.
        private static readonly Vector3 RightAxis = IsoGrid.RowAxis;
        private static readonly Vector3 LeftAxis = -IsoGrid.ColumnAxis;

        // The pivot is the front tile's centre, so a 1x1 building's base is exactly that tile's diamond.
        private const float FRONT_GROUND_Y = -IsoGrid.HALF_HEIGHT;

        // Outline edge flags (TEXCOORD3): uv.x = 0, uv.x = 1, uv.y = 0, uv.y = 1. 2 means outlined; the shader tests
        // above 1.5 because a mesh without the channel reads (0, 0, 0, 1) on D3D.
        private static readonly Vector4 NoOutline = Vector4.zero;
        private static readonly Vector4 OutlineLowX = new Vector4(2f, 0f, 0f, 0f);
        private static readonly Vector4 OutlineHighX = new Vector4(0f, 2f, 0f, 0f);
        private static readonly Vector4 OutlineHighXLowY = new Vector4(0f, 2f, 2f, 0f);
        private static readonly Vector4 OutlineHighXHighY = new Vector4(0f, 2f, 0f, 2f);

        // Quad corners in face order, and the walls' (uv.y runs up from the ground).
        private static readonly Vector2[] QuadUvs =
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
        };
        private static readonly Vector2[] WallUvs =
        {
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f),
            new Vector2(0f, 0f),
        };

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

        [Tooltip("Flat roof, or a gable with its ridge along the longer side of the footprint.")]
        [SerializeField]
        private RoofStyle _roof = RoofStyle.Flat;

        [Tooltip(
            "Slope of a gable roof, in degrees. Keep it under 30: from the 30° camera a steeper back slope faces away and vanishes."
        )]
        [SerializeField, Range(5f, 28f)]
        private float _roofPitch = 20f;

        [Tooltip("Tint on the roof faces, multiplied with the roof art. White leaves it as painted.")]
        [SerializeField]
        private Color _roofColor = Color.white;

        private Mesh _mesh;

        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Vector3> _normals = new List<Vector3>();
        private readonly List<Vector4> _tangents = new List<Vector4>();
        private readonly List<Vector2> _uvs = new List<Vector2>();
        private readonly List<Vector2> _faceData = new List<Vector2>();
        private readonly List<Vector4> _outline = new List<Vector4>();
        private readonly List<Color> _colors = new List<Color>();
        private readonly List<int> _triangles = new List<int>();

        // How far a gable's ridge rises above the eaves, in world units: half the span (the shorter side) at the pitch,
        // turned into screen height.
        public static float RidgeRise(float spanTiles, float pitchDegrees)
        {
            return spanTiles * 0.5f * Mathf.Tan(pitchDegrees * Mathf.Deg2Rad) * IsoGrid.SCREEN_HEIGHT_PER_CELL;
        }

        public void Configure(float rightTiles, float leftTiles, float wallHeight)
        {
            Configure(rightTiles, leftTiles, wallHeight, RoofStyle.Flat, _roofPitch, Color.white);
        }

        public void Configure(
            float rightTiles,
            float leftTiles,
            float wallHeight,
            RoofStyle roof,
            float roofPitch,
            Color roofColor
        )
        {
            _rightTiles = Mathf.Max(0.25f, rightTiles);
            _leftTiles = Mathf.Max(0.25f, leftTiles);
            _wallHeight = Mathf.Max(0.05f, wallHeight);
            _roof = roof;
            _roofPitch = Mathf.Clamp(roofPitch, 5f, 28f);
            _roofColor = roofColor;
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
            _vertices.Clear();
            _normals.Clear();
            _tangents.Clear();
            _uvs.Clear();
            _faceData.Clear();
            _outline.Clear();
            _colors.Clear();
            _triangles.Clear();

            Vector3 front = new Vector3(0f, FRONT_GROUND_Y + _wallHeight, 0f);
            Vector3 right = front + (RightAxis * _rightTiles);
            Vector3 left = front + (LeftAxis * _leftTiles);
            Vector3 peak = right + (LeftAxis * _leftTiles);
            Vector3 down = Vector3.down * _wallHeight;
            float top = _wallHeight;

            // Walls, the same for every roof: the left one's outer edge is uv.x = 0, the right one's uv.x = 1.
            // ponytail: each face stretches its whole atlas cell; tile UVs with frac() in the shader when facades need
            // repeating storeys. Tangents follow each face's uv.x; the walls' uv.y runs up, which is -cross(normal,
            // tangent), hence w = -1.
            AddFace(
                new[] { left, front, front + down, left + down },
                WallUvs,
                -IsoGrid.RowView,
                IsoGrid.ColumnView,
                -1f,
                1f,
                new[] { top, top, 0f, 0f },
                OutlineLowX,
                Color.white
            );
            AddFace(
                new[] { front, right, right + down, front + down },
                WallUvs,
                IsoGrid.ColumnView,
                IsoGrid.RowView,
                -1f,
                2f,
                new[] { top, top, 0f, 0f },
                OutlineHighX,
                Color.white
            );

            if (_roof == RoofStyle.Gable)
            {
                AddGable(front, right, left, peak, top);
            }
            else
            {
                // The flat diamond: its back edges (left to peak, uv.y = 0; peak to right, uv.x = 1) are the outline.
                AddFace(
                    new[] { left, peak, right, front },
                    QuadUvs,
                    IsoGrid.UpView,
                    IsoGrid.RowView,
                    1f,
                    0f,
                    new[] { top, top, top, top },
                    OutlineHighXLowY,
                    _roofColor
                );
            }

            _mesh.Clear();
            _mesh.SetVertices(_vertices);
            _mesh.SetNormals(_normals);
            _mesh.SetTangents(_tangents);
            _mesh.SetUVs(0, _uvs);
            _mesh.SetUVs(1, _faceData);
            _mesh.SetUVs(3, _outline);
            _mesh.SetColors(_colors);
            _mesh.SetTriangles(_triangles, 0);
            _mesh.RecalculateBounds();
        }

        // Two slopes up to a ridge along the longer side and the gable-end triangle above the visible short wall. The
        // outline runs over the back eave and down both ends of the roof: the back slope's back and far-end edges and
        // the front slope's far-end edge.
        private void AddGable(Vector3 front, Vector3 right, Vector3 left, Vector3 peak, float top)
        {
            bool ridgeAlongRight = _rightTiles >= _leftTiles;
            float rise = RidgeRise(ridgeAlongRight ? _leftTiles : _rightTiles, _roofPitch);
            float ridgeHeight = top + rise;
            Vector3 lift = Vector3.up * rise;
            float pitch = _roofPitch * Mathf.Deg2Rad;
            float cos = Mathf.Cos(pitch);
            float sin = Mathf.Sin(pitch);

            // In view space: the ridge's direction, the wall the front slope rises from, and that wall's normal.
            Vector3 alongRidge = ridgeAlongRight ? IsoGrid.RowView : -IsoGrid.ColumnView;
            Vector3 frontWallNormal = ridgeAlongRight ? IsoGrid.ColumnView : -IsoGrid.RowView;
            Vector3 frontSlopeNormal = ((IsoGrid.UpView * cos) + (frontWallNormal * sin)).normalized;
            Vector3 backSlopeNormal = ((IsoGrid.UpView * cos) - (frontWallNormal * sin)).normalized;
            Vector3 upFrontSlope = ((-frontWallNormal * cos) + (IsoGrid.UpView * sin)).normalized;
            Vector3 downBackSlope = ((-frontWallNormal * cos) - (IsoGrid.UpView * sin)).normalized;

            // Eave corners in slope order: the front slope's eave (near, far) and the back slope's eave (far, near),
            // with the ridge above the midpoints of the two short ends.
            Vector3 eaveNear;
            Vector3 eaveFar;
            Vector3 backFar;
            Vector3 backNear;
            if (ridgeAlongRight)
            {
                eaveNear = front;
                eaveFar = right;
                backFar = peak;
                backNear = left;
            }
            else
            {
                eaveNear = front;
                eaveFar = left;
                backFar = peak;
                backNear = right;
            }

            Vector3 ridgeNear = ((eaveNear + backNear) * 0.5f) + lift;
            Vector3 ridgeFar = ((eaveFar + backFar) * 0.5f) + lift;

            AddFace(
                new[] { eaveNear, eaveFar, ridgeFar, ridgeNear },
                QuadUvs,
                frontSlopeNormal,
                alongRidge,
                TangentSign(frontSlopeNormal, alongRidge, upFrontSlope),
                0f,
                new[] { top, top, ridgeHeight, ridgeHeight },
                OutlineHighX,
                _roofColor
            );
            AddFace(
                new[] { ridgeNear, ridgeFar, backFar, backNear },
                QuadUvs,
                backSlopeNormal,
                alongRidge,
                TangentSign(backSlopeNormal, alongRidge, downBackSlope),
                0f,
                new[] { ridgeHeight, ridgeHeight, top, top },
                OutlineHighXHighY,
                _roofColor
            );

            // The gable end on the visible short wall, under the ridge's near end.
            // ponytail: samples the wall art's top row; give gables their own atlas cell once facades have trim art.
            bool onLeftWall = ridgeAlongRight;
            AddFace(
                onLeftWall ? new[] { backNear, eaveNear, ridgeNear } : new[] { eaveNear, backNear, ridgeNear },
                new[] { new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f) },
                onLeftWall ? -IsoGrid.RowView : IsoGrid.ColumnView,
                onLeftWall ? IsoGrid.ColumnView : IsoGrid.RowView,
                -1f,
                onLeftWall ? 1f : 2f,
                new[] { top, top, ridgeHeight },
                NoOutline,
                Color.white
            );
        }

        // The bitangent cross(normal, tangent) * w must run along the face's uv.y.
        private static float TangentSign(Vector3 normal, Vector3 tangent, Vector3 alongUvY)
        {
            return Vector3.Dot(Vector3.Cross(normal, tangent), alongUvY) >= 0f ? 1f : -1f;
        }

        // A triangle (3 corners) or quad (4, split 0-1-2 and 0-2-3). faceData is (face id, height above the ground in
        // world units) per vertex, read by the shader.
        private void AddFace(
            Vector3[] corners,
            Vector2[] uvs,
            Vector3 normal,
            Vector3 tangent,
            float tangentSign,
            float faceId,
            float[] heights,
            Vector4 outline,
            Color color
        )
        {
            int first = _vertices.Count;
            for (int index = 0; index < corners.Length; index++)
            {
                _vertices.Add(corners[index]);
                _normals.Add(normal);
                _tangents.Add(new Vector4(tangent.x, tangent.y, tangent.z, tangentSign));
                _uvs.Add(uvs[index]);
                _faceData.Add(new Vector2(faceId, heights[index]));
                _outline.Add(outline);
                _colors.Add(color);
            }

            _triangles.Add(first);
            _triangles.Add(first + 1);
            _triangles.Add(first + 2);
            if (corners.Length == 4)
            {
                _triangles.Add(first);
                _triangles.Add(first + 2);
                _triangles.Add(first + 3);
            }
        }
    }
}
