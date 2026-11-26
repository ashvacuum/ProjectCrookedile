using System.Collections.Generic;
using Crookedile.Data;
using Crookedile.Data.Campaign;
using Crookedile.Gameplay.City;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UIElements;

namespace Crookedile.UI.Campaign
{
    // The campaign map's camera and district markers over the run's city: drag to pan, scroll to zoom toward the
    // cursor, both clamped to the city and ignored over CampaignFlow's panels. Each district gets a name marker on its
    // building, and the current district is highlighted. Reads the run; never changes it.
    // ponytail: lives beside CampaignFlow's runtime-built UI Toolkit root. Moves into the campaign UIRouter rebuild
    // (ui-elements.md, Campaign scene); markers become clickable when visits move from the panel onto the map.
    // Shares the UIDocument CampaignFlow adds to this object at start, found on first use, never required in the scene:
    // a scene-placed one starts before CampaignFlow gives it panel settings.
    public sealed class CampaignMapView : MonoBehaviour
    {
        private enum DragButton
        {
            Left,
            Right,
            Middle,
        }

        private const string MARKER_CLASS = "district-marker";
        private const string HERE_CLASS = "district-marker--here";

        [Tooltip("The run's city: its bounds clamp the camera and its district positions place the markers.")]
        [SerializeField]
        private CampaignCity _city;

        [Tooltip("The camera that shows the city.")]
        [SerializeField]
        private Camera _camera;

        [Tooltip("Mouse button that drags the map.")]
        [SerializeField]
        private DragButton _dragButton = DragButton.Left;

        [Tooltip("Zoom when a city is shown: half the screen's height, in world units. One cell is 0.5 units tall.")]
        [SerializeField, Min(0.5f), SuffixLabel("units", true)]
        private float _startZoom = 6f;

        [Tooltip("Closest and farthest zoom the scroll wheel allows, as half the screen's height in world units.")]
        [SerializeField, MinMaxSlider(0.5f, 40f, true)]
        private Vector2 _zoomRange = new Vector2(2.5f, 18f);

        [Tooltip("How much one scroll notch zooms.")]
        [SerializeField, Range(0.01f, 0.5f)]
        private float _zoomPerNotch = 0.12f;

        [Tooltip(
            "Glow on the buildings of districts with a location still open today: rgb is the colour, alpha the strength. Alpha 0 turns it off."
        )]
        [SerializeField]
        private Color _openGlow = new Color(1f, 0.8f, 0.35f, 0.9f);

        private readonly HashSet<DistrictData> _glowing = new HashSet<DistrictData>();
        private readonly HashSet<DistrictData> _open = new HashSet<DistrictData>();

        private UIDocument _documentCache;
        private VisualElement _markerLayer;
        private readonly Dictionary<DistrictData, Label> _markers = new Dictionary<DistrictData, Label>();
        private bool _dragging;
        private Vector3 _dragAnchor;

        #region Lifecycle

        private UIDocument Document => _documentCache != null ? _documentCache : _documentCache = GetComponent<UIDocument>();

        private void OnEnable()
        {
            if (_city != null)
            {
                _city.Built += OnCityBuilt;
            }
        }

        private void OnDisable()
        {
            if (_city != null)
            {
                _city.Built -= OnCityBuilt;
            }
        }

        private void Update()
        {
            if (_camera == null || _city == null || Mouse.current == null)
            {
                return;
            }

            Mouse mouse = Mouse.current;
            Vector2 pointer = mouse.position.ReadValue();
            ButtonControl button = DragControl(mouse);
            if (button.wasPressedThisFrame && !OverPanel(pointer))
            {
                _dragging = true;
                _dragAnchor = ScreenToWorld(pointer);
            }
            else if (!button.isPressed)
            {
                _dragging = false;
            }

            // Keep the world point grabbed under the cursor.
            if (_dragging)
            {
                _camera.transform.position += _dragAnchor - ScreenToWorld(pointer);
            }

            float scroll = mouse.scroll.ReadValue().y;
            if (scroll != 0f && !_dragging && !OverPanel(pointer))
            {
                Vector3 before = ScreenToWorld(pointer);
                float factor = Mathf.Pow(1f - _zoomPerNotch, Mathf.Sign(scroll));
                _camera.orthographicSize = Mathf.Clamp(
                    _camera.orthographicSize * factor,
                    Mathf.Min(_zoomRange.x, _zoomRange.y),
                    Mathf.Max(_zoomRange.x, _zoomRange.y)
                );
                _camera.transform.position += before - ScreenToWorld(pointer);
            }

            Clamp();
        }

        // After the camera moves, so markers never trail it by a frame.
        private void LateUpdate()
        {
            UpdateMarkers();
            UpdateGlow();
        }

        #endregion

        #region Private API

        private void OnCityBuilt()
        {
            if (_camera == null)
            {
                return;
            }

            // Open on the district the run is in, else downtown.
            _camera.orthographic = true;
            _camera.orthographicSize = _startZoom;
            DistrictData here = RunState.Current != null ? RunState.Current.CurrentDistrict : null;
            Vector3 focus =
                here != null && _city.DistrictPositions.TryGetValue(here, out Vector3 position)
                    ? position
                    : _city.DowntownPosition;
            _camera.transform.position = new Vector3(focus.x, focus.y, _camera.transform.position.z);
            Clamp();
            RebuildMarkers();
            // A rebuilt city has fresh buildings with no glow, so every open district is lit again.
            _glowing.Clear();
            UpdateGlow();
        }

        // Lights the districts with a location still on today's list and dims the rest. Only changes
        // touch the renderers, so a quiet frame costs one pass over today's few locations.
        private void UpdateGlow()
        {
            if (_city == null)
                return;
            RunState run = RunState.Current;
            _open.Clear();
            if (run != null && run.TodaysLocationsDay == run.Day)
            {
                foreach (EncounterData location in run.TodaysLocations)
                {
                    if (location != null && location.District != null)
                    {
                        _open.Add(location.District);
                    }
                }
            }

            if (_open.SetEquals(_glowing))
            {
                return;
            }

            foreach (DistrictData district in _glowing)
            {
                if (!_open.Contains(district))
                {
                    _city.SetDistrictGlow(district, Color.clear);
                }
            }

            foreach (DistrictData district in _open)
            {
                if (!_glowing.Contains(district))
                {
                    _city.SetDistrictGlow(district, _openGlow);
                }
            }

            _glowing.Clear();
            _glowing.UnionWith(_open);
        }

        private ButtonControl DragControl(Mouse mouse)
        {
            switch (_dragButton)
            {
                case DragButton.Right:
                    return mouse.rightButton;
                case DragButton.Middle:
                    return mouse.middleButton;
                default:
                    return mouse.leftButton;
            }
        }

        private Vector3 ScreenToWorld(Vector2 screen)
        {
            Vector3 world = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -_camera.transform.position.z));
            world.z = 0f;
            return world;
        }

        // The whole view stays on the map, never past its edges: zoom is capped at the most the map's narrow side
        // can fill, then the centre is pulled in until every corner of the view stands on a map cell.
        private void Clamp()
        {
            if (!_city.IsBuilt)
            {
                return;
            }

            _camera.orthographicSize = Mathf.Min(
                _camera.orthographicSize,
                _city.MaxViewHalfHeight(_camera.aspect)
            );
            float halfHeight = _camera.orthographicSize;
            _camera.transform.position = _city.ClampViewCenter(
                _camera.transform.position,
                halfHeight * _camera.aspect,
                halfHeight
            );
        }

        // Whether the pointer is over one of CampaignFlow's panels or its tooltip, where clicks belong to the UI.
        private bool OverPanel(Vector2 screen)
        {
            IPanel panel = Document != null && Document.rootVisualElement != null ? Document.rootVisualElement.panel : null;
            if (panel == null)
            {
                return false;
            }

            Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screen.x, Screen.height - screen.y));
            for (VisualElement element = panel.Pick(panelPosition); element != null; element = element.parent)
            {
                if (element.ClassListContains("panel") || element.ClassListContains("tooltip"))
                {
                    return true;
                }
            }

            return false;
        }

        private void RebuildMarkers()
        {
            VisualElement root = Document != null ? Document.rootVisualElement : null;
            if (root == null)
            {
                return;
            }

            if (_markerLayer == null)
            {
                // Behind CampaignFlow's content, so panels cover markers.
                _markerLayer = new VisualElement { pickingMode = PickingMode.Ignore };
                _markerLayer.AddToClassList("district-markers");
                _markerLayer.StretchToParentSize();
                root.Insert(0, _markerLayer);
            }

            _markerLayer.Clear();
            _markers.Clear();
            foreach (KeyValuePair<DistrictData, Vector3> pair in _city.DistrictPositions)
            {
                var marker = new Label(pair.Key.DisplayName) { pickingMode = PickingMode.Ignore };
                marker.AddToClassList(MARKER_CLASS);
                marker.style.position = Position.Absolute;
                marker.style.translate = new Translate(Length.Percent(-50f), Length.Percent(-100f));
                _markerLayer.Add(marker);
                _markers[pair.Key] = marker;
            }
        }

        private void UpdateMarkers()
        {
            if (_markers.Count == 0 || _camera == null || _markerLayer.panel == null)
            {
                return;
            }

            DistrictData here = RunState.Current != null ? RunState.Current.CurrentDistrict : null;
            foreach (KeyValuePair<DistrictData, Label> pair in _markers)
            {
                if (!_city.DistrictPositions.TryGetValue(pair.Key, out Vector3 world))
                {
                    continue;
                }

                Vector2 position = RuntimePanelUtils.CameraTransformWorldToPanel(_markerLayer.panel, world, _camera);
                pair.Value.style.left = position.x;
                pair.Value.style.top = position.y;
                pair.Value.EnableInClassList(HERE_CLASS, pair.Key == here);
            }
        }

        #endregion
    }
}
