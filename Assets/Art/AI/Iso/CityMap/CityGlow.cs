using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace Crookedile.Art.Iso
{
    // Window light spilling onto the city at night (city-generator.md §6.5). Bakes a soft RGB texture over the grid from
    // every lit building's walls, spreading only through open ground so light never passes through a building; the
    // shader adds it scaled by the sun's night emission. Rebaked with the buildings, never per frame.
    [Serializable]
    public sealed class CityGlow
    {
        private static readonly int GlowMapId = Shader.PropertyToID("_CityGlowMap");
        private static readonly int GlowId = Shader.PropertyToID("_CityGlow");
        private const float DIAGONAL = 1.4142135f;

        [Tooltip("Glow texels per cell side. More is smoother and slower to bake.")]
        [SerializeField, Range(1, 8), SuffixLabel("per cell", true)]
        private int _texelsPerCell = 4;

        [Tooltip("How far window light reaches from a wall, in cells.")]
        [SerializeField, Range(0.5f, 8f), SuffixLabel("cells", true)]
        private float _reachCells = 2.5f;

        [Tooltip("Brightness of the light at the wall, before the sun's night emission scale.")]
        [SerializeField, Min(0f)]
        private float _strength = 0.6f;

        [Tooltip(
            "Chance a building has its lights on at night; the rest stay dark so streets aren't evenly lit."
        )]
        [SerializeField, Range(0f, 1f)]
        private float _litChance = 0.7f;

        [Tooltip("Usual window light colour (tungsten).")]
        [SerializeField]
        private Color _warmColor = new Color(1f, 0.72f, 0.42f);

        [Tooltip("Occasional window light colour (fluorescent).")]
        [SerializeField]
        private Color _coolColor = new Color(0.75f, 0.85f, 1f);

        [Tooltip("Chance a lit building uses the cool colour.")]
        [SerializeField, Range(0f, 1f)]
        private float _coolChance = 0.25f;

        [Tooltip(
            "Height above the ground, in cells, over which the glow fades out, so it lights streets and low walls, not roofs."
        )]
        [SerializeField, Min(0.05f), SuffixLabel("cells", true)]
        private float _heightFadeCells = 1.5f;

        [NonSerialized]
        private Texture2D _texture;

        public void Publish(CityMapData map)
        {
            if (map == null || _strength <= 0f)
            {
                Shader.SetGlobalVector(GlowId, Vector4.zero);
                return;
            }

            int texels = _texelsPerCell;
            int width = map.Size.x * texels;
            int height = map.Size.y * texels;
            bool[] blocked = new bool[width * height];
            foreach (CityMapData.Building building in map.Buildings)
            {
                foreach (Vector2Int texel in TexelRect(building.Lot, texels).allPositionsWithin)
                {
                    blocked[(texel.y * width) + texel.x] = true;
                }
            }

            Color[] light = new Color[width * height];
            float[] distance = new float[width * height];
            for (int index = 0; index < distance.Length; index++)
            {
                distance[index] = float.MaxValue;
            }

            List<int> touched = new List<int>();
            foreach (CityMapData.Building building in map.Buildings)
            {
                // Seeded by the lot, so a building keeps its lights and colour when others change.
                Random rng = new Random((building.Lot.x * 73856093) ^ (building.Lot.y * 19349663));
                if (rng.NextDouble() >= _litChance)
                {
                    continue;
                }

                // More storeys, more windows; 0.5 world units is one storey.
                Color color =
                    (rng.NextDouble() < _coolChance ? _coolColor : _warmColor)
                    * _strength
                    * Mathf.Sqrt(Mathf.Max(1f, building.WallHeight * 2f));
                Spread(
                    TexelRect(building.Lot, texels),
                    color,
                    _reachCells * texels,
                    width,
                    height,
                    blocked,
                    distance,
                    light,
                    touched
                );
            }

            if (_texture == null || _texture.width != width || _texture.height != height)
            {
                Release();
                _texture = new Texture2D(width, height, TextureFormat.RGBAHalf, false, true)
                {
                    name = "City Glow Map",
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                };
            }

            _texture.SetPixels(light);
            _texture.Apply(false);
            Shader.SetGlobalTexture(GlowMapId, _texture);
            Shader.SetGlobalVector(GlowId, new Vector4(_heightFadeCells, 1f, 0f, 0f));
        }

        public void Release()
        {
            Shader.SetGlobalVector(GlowId, Vector4.zero);
            if (_texture == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(_texture);
            }
            else
            {
                Object.DestroyImmediate(_texture);
            }

            _texture = null;
        }

        // Texel x is the row and y the column, like the height map.
        private static RectInt TexelRect(RectInt lot, int texels)
        {
            return new RectInt(lot.position * texels, lot.size * texels);
        }

        // Light leaves the source's border and walks open texels (8 neighbours) out to `reach`, fading with distance.
        // A lamp post is the same call with a one-texel source.
        private static void Spread(
            RectInt source,
            Color color,
            float reach,
            int width,
            int height,
            bool[] blocked,
            float[] distance,
            Color[] light,
            List<int> touched
        )
        {
            Queue<Vector2Int> frontier = new Queue<Vector2Int>();
            for (int x = source.xMin - 1; x <= source.xMax; x++)
            {
                for (int y = source.yMin - 1; y <= source.yMax; y++)
                {
                    bool border =
                        x < source.xMin || x >= source.xMax || y < source.yMin || y >= source.yMax;
                    if (
                        border
                        && x >= 0
                        && y >= 0
                        && x < width
                        && y < height
                        && !blocked[(y * width) + x]
                    )
                    {
                        distance[(y * width) + x] = 0f;
                        touched.Add((y * width) + x);
                        frontier.Enqueue(new Vector2Int(x, y));
                    }
                }
            }

            while (frontier.Count > 0)
            {
                Vector2Int texel = frontier.Dequeue();
                float here = distance[(texel.y * width) + texel.x];
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int x = texel.x + dx;
                        int y = texel.y + dy;
                        if (
                            (dx == 0 && dy == 0)
                            || x < 0
                            || y < 0
                            || x >= width
                            || y >= height
                            || blocked[(y * width) + x]
                        )
                        {
                            continue;
                        }

                        float next = here + (dx != 0 && dy != 0 ? DIAGONAL : 1f);
                        int index = (y * width) + x;
                        if (next > reach || next >= distance[index])
                        {
                            continue;
                        }

                        if (distance[index] == float.MaxValue)
                        {
                            touched.Add(index);
                        }

                        distance[index] = next;
                        frontier.Enqueue(new Vector2Int(x, y));
                    }
                }
            }

            foreach (int index in touched)
            {
                float fade = 1f - (distance[index] / reach);
                light[index] += color * (fade * fade);
                distance[index] = float.MaxValue;
            }

            touched.Clear();
        }
    }
}
