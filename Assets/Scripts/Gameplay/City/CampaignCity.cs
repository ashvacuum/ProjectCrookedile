using Crookedile.Data;
using Crookedile.Utilities;
using UnityEngine;

namespace Crookedile.Gameplay.City
{
    // Shows the run's city on the campaign map (city-generator.md §1): generated from RunState.Seed, so the same run
    // always draws the same city and nothing extra is saved, and a sky that follows the campaign clock. Scenery only:
    // it reads the run and never changes it.
    [Debuggable("City", LogLevel.Info)]
    public sealed class CampaignCity : MonoBehaviour
    {
        [Tooltip("The view that draws the generated city. Its Randomizer settings shape the city.")]
        [SerializeField]
        private CityMapView _view;

        [Tooltip("The sky, driven by the run's clock.")]
        [SerializeField]
        private CitySky _sky;

        [Tooltip("Camera framed on the whole city when it is built.")]
        [SerializeField]
        private Camera _camera;

        [Tooltip("Extra room around the city when framing it: 1 fits it exactly.")]
        [SerializeField, Min(0.5f)]
        private float _framePadding = 1.05f;

        private RunState _builtFor;
        private CityMapData _map;

        #region Lifecycle

        private void Update()
        {
            // The run can appear after this starts (CampaignFlow creates or loads it) or be replaced by a new run.
            RunState run = RunState.Current;
            if (run == null)
            {
                return;
            }

            if (run != _builtFor)
            {
                Build(run);
            }

            if (_sky != null)
            {
                _sky.MinuteOfDay = run.ClockMinute;
            }
        }

        private void OnDestroy()
        {
            if (_map != null)
            {
                Destroy(_map);
            }
        }

        #endregion

        #region Private API

        private void Build(RunState run)
        {
            _builtFor = run;
            if (_view == null)
            {
                return;
            }

            if (_map == null)
            {
                _map = ScriptableObject.CreateInstance<CityMapData>();
                _map.name = "Run City";
                _map.hideFlags = HideFlags.DontSave;
            }

            // The randomizer stands in for the §4 generator; its own System.Random keeps run and battle draws untouched.
            _view.Randomizer.Fill(_map, run.Seed);
            _view.Show(_map);
            if (_sky != null)
            {
                _sky.Rain.PuddleSeed = run.Seed;
            }

            Frame();
            GameLogger.LogInfo<CampaignCity>(
                $"Built the run's city from seed {run.Seed}: {_map.Buildings.Count} buildings.",
                this
            );
        }

        // ponytail: fits the whole map once; replace with pan and zoom around the current district when the map gets
        // markers (city-generator.md, campaign map step 3).
        private void Frame()
        {
            if (_camera == null)
            {
                return;
            }

            Vector2Int size = _map.Size;
            Vector3 origin = _view.transform.position;
            Vector3 center = origin + IsoGrid.CellToLocal(size.x * 0.5f - 0.5f, size.y * 0.5f - 0.5f);
            float width = IsoGrid.CellToLocal(size.x, size.y).x - IsoGrid.CellToLocal(0f, 0f).x;
            float height = IsoGrid.CellToLocal(size.x, 0f).y - IsoGrid.CellToLocal(0f, size.y).y;
            _camera.orthographic = true;
            _camera.orthographicSize =
                Mathf.Max(height * 0.5f, width * 0.5f / Mathf.Max(_camera.aspect, 0.01f)) * _framePadding;
            _camera.transform.position = new Vector3(center.x, center.y, _camera.transform.position.z);
        }

        #endregion
    }
}
