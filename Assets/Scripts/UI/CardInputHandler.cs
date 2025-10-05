using UnityEngine;
using UnityEngine.InputSystem;

namespace Crookedile.UI
{
    /// <summary>
    /// Handles input for card interactions using Unity's New Input System.
    /// Supports mouse, touch, and gamepad input.
    /// </summary>
    public class CardInputHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CardHandManager handManager;
        [SerializeField] private Camera mainCamera;

        [Header("Settings")]
        [SerializeField] private LayerMask cardLayerMask = ~0; // Default: all layers

        private Card3DView hoveredCard;
        private Card3DView draggedCard;
        private Vector3 dragOffset;

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
        }

        private void OnEnable()
        {
            // Subscribe to input events when enabled
        }

        private void OnDisable()
        {
            // Unsubscribe from input events when disabled
        }

        private void Update()
        {
            // Use new input system for pointer position
            Vector2 pointerPosition = Pointer.current?.position.ReadValue() ?? Vector2.zero;
            HandlePointerHover(pointerPosition);

            // Check for pointer click
            if (Mouse.current?.leftButton.wasPressedThisFrame ?? false)
            {
                HandlePointerDown(pointerPosition);
            }

            if (Mouse.current?.leftButton.wasReleasedThisFrame ?? false)
            {
                HandlePointerUp(pointerPosition);
            }

            // TODO: Add gamepad support for card selection
        }

        private void HandlePointerHover(Vector2 screenPosition)
        {
            Card3DView card = GetCardAtPosition(screenPosition);

            if (card != hoveredCard)
            {
                // Exit previous hover
                if (hoveredCard != null)
                {
                    hoveredCard.OnHoverExit();
                }

                // Enter new hover
                hoveredCard = card;
                if (hoveredCard != null)
                {
                    hoveredCard.OnHoverEnter();
                }
            }
        }

        private void HandlePointerDown(Vector2 screenPosition)
        {
            Card3DView card = GetCardAtPosition(screenPosition);

            if (card != null && handManager.CardsInHand.Contains(card))
            {
                handManager.SelectCard(card);

                // TODO: Implement drag functionality
                // draggedCard = card;
                // dragOffset = CalculateDragOffset(card, screenPosition);
            }
        }

        private void HandlePointerUp(Vector2 screenPosition)
        {
            // TODO: Implement drag release functionality
            draggedCard = null;
        }

        private Card3DView GetCardAtPosition(Vector2 screenPosition)
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, cardLayerMask))
            {
                return hit.collider.GetComponent<Card3DView>();
            }

            return null;
        }

        // TODO: Implement drag offset calculation for smooth dragging
        private Vector3 CalculateDragOffset(Card3DView card, Vector2 screenPosition)
        {
            return Vector3.zero;
        }
    }
}
