using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Crookedile.Art.Iso
{
    // Builds the half-cube mesh (roof diamond plus left and right walls) from a footprint in iso tiles.
    // The front-bottom corner stays fixed, so the building grows back and up from the tile it stands on.
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class HalfCubeBuilding : MonoBehaviour
    {
        private static readonly Vector3 RightAxis = new Vector3(0.4f, 0.2f, 0f);
        private static readonly Vector3 LeftAxis = new Vector3(-0.4f, 0.2f, 0f);
        private static readonly Vector3 RoofNormal = Vector3.back;
        private static readonly Vector3 LeftNormal = new Vector3(-0.6f, 0f, -0.8f).normalized;
        private static readonly Vector3 RightNormal = new Vector3(0.6f, 0f, -0.8f).normalized;
        private static readonly Vector3 LeftTangent = new Vector3(0.8f, 0f, -0.6f).normalized;
        private static readonly Vector3 RightTangent = new Vector3(0.8f, 0f, 0.6f).normalized;
        // Keeps a 1x1 building at the placement the previews were tuned with.
        private const float FRONT_GROUND_Y = -0.48f;

        [Tooltip("Footprint length in tiles along the right wall.")]
        [SerializeField, Min(0.25f)] private float _rightTiles = 1f;
        [Tooltip("Footprint length in tiles along the left wall.")]
        [SerializeField, Min(0.25f)] private float _leftTiles = 1f;
        [Tooltip("Wall height in world units. 0.4 matches one tile's wall in the original preview.")]
        [SerializeField, Min(0.05f)] private float _wallHeight = 0.4f;

        private Mesh _mesh;

        private void OnEnable()
        {
            if (_mesh == null)
            {
                _mesh = new Mesh { name = "Half Cube Building", hideFlags = HideFlags.DontSave };
            }

            Rebuild(false);
            GetComponent<MeshFilter>().sharedMesh = _mesh;
        }

        private void OnValidate()
        {
            // OnEnable owns the first build; assigning the MeshFilter from OnValidate makes Unity warn.
            if (_mesh != null)
            {
                Rebuild(true);
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

        // Editor shadow writes are deferred from OnValidate, where touching another component makes Unity warn.
        private void Rebuild(bool deferShadow)
        {
            Vector3 front = new Vector3(0f, FRONT_GROUND_Y + _wallHeight, 0f);
            Vector3 right = front + (RightAxis * _rightTiles);
            Vector3 left = front + (LeftAxis * _leftTiles);
            Vector3 peak = right + (LeftAxis * _leftTiles);
            Vector3 down = Vector3.down * _wallHeight;

            Vector3[] vertices =
            {
                left, peak, right, front,
                left, front, front + down, left + down,
                front, right, right + down, front + down,
            };

            Vector3[] normals = new Vector3[vertices.Length];
            Vector4[] tangents = new Vector4[vertices.Length];
            Vector2[] uvs = new Vector2[vertices.Length];
            Vector2[] faceIds = new Vector2[vertices.Length];
            Color[] colors = new Color[vertices.Length];
            // ponytail: each face stretches its whole atlas cell; tile UVs with frac() in the shader when facades need repeating storeys.
            SetFace(normals, tangents, uvs, faceIds, 0, RoofNormal, Vector3.right, 0f, false);
            SetFace(normals, tangents, uvs, faceIds, 4, LeftNormal, LeftTangent, 1f, true);
            SetFace(normals, tangents, uvs, faceIds, 8, RightNormal, RightTangent, 2f, true);
            for (int index = 0; index < colors.Length; index++)
            {
                colors[index] = Color.white;
            }

            _mesh.Clear();
            _mesh.vertices = vertices;
            _mesh.normals = normals;
            _mesh.tangents = tangents;
            _mesh.uv = uvs;
            _mesh.SetUVs(1, faceIds);
            _mesh.colors = colors;
            _mesh.triangles = new[] { 0, 1, 2, 0, 2, 3, 4, 5, 6, 4, 6, 7, 8, 9, 10, 8, 10, 11 };
            _mesh.RecalculateBounds();

#if UNITY_EDITOR
            Vector3[] outline = { peak, right, right + down, front + down, left + down, left };
            if (deferShadow)
            {
                UnityEditor.EditorApplication.delayCall += () => ApplyShadowOutline(outline);
            }
            else
            {
                ApplyShadowOutline(outline);
            }
#endif
        }

        private static void SetFace(Vector3[] normals, Vector4[] tangents, Vector2[] uvs, Vector2[] faceIds, int start, Vector3 normal, Vector3 tangent, float faceId, bool invertV)
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
                tangents[start + index] = new Vector4(tangent.x, tangent.y, tangent.z, 1f);
                uvs[start + index] = faceUvs[index];
                faceIds[start + index] = new Vector2(faceId, 0f);
            }
        }

#if UNITY_EDITOR
        // ponytail: ShadowCaster2D has no public shape-path setter, so the outline is written through its serialized
        // fields in the editor and saved with the scene or prefab; resizing at runtime leaves the old shadow.
        private void ApplyShadowOutline(Vector3[] outline)
        {
            if (this == null || !TryGetComponent(out ShadowCaster2D shadowCaster))
            {
                return;
            }

            UnityEditor.SerializedObject serializedCaster = new UnityEditor.SerializedObject(shadowCaster);
            UnityEditor.SerializedProperty shapePath = serializedCaster.FindProperty("m_ShapePath");
            bool changed = shapePath.arraySize != outline.Length;
            shapePath.arraySize = outline.Length;
            for (int index = 0; index < outline.Length; index++)
            {
                UnityEditor.SerializedProperty point = shapePath.GetArrayElementAtIndex(index);
                changed |= point.vector3Value != outline[index];
                point.vector3Value = outline[index];
            }

            if (!changed)
            {
                return;
            }

            serializedCaster.FindProperty("m_ShadowCastingSource").intValue = 1;
            // A hash that cannot match the new path makes the caster rebuild its shadow mesh.
            serializedCaster.FindProperty("m_ShapePathHash").intValue = 1;
            serializedCaster.ApplyModifiedPropertiesWithoutUndo();
            shadowCaster.castsShadows = true;
            shadowCaster.selfShadows = false;
            shadowCaster.Update();
        }
#endif
    }
}
