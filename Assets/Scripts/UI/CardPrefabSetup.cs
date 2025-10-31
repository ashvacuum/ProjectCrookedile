using UnityEngine;
using TMPro;
using MoreMountains.Feedbacks;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Crookedile.UI
{
    /// <summary>
    /// Editor utility to create a properly configured card prefab
    /// </summary>
    public class CardPrefabSetup : MonoBehaviour
    {
#if UNITY_EDITOR
        [MenuItem("GameObject/Crookedile/Create Card Prefab", false, 10)]
        static void CreateCardPrefab()
        {
            // Create root card object
            GameObject cardRoot = new GameObject("Card");
            cardRoot.layer = LayerMask.NameToLayer("Default");

            // Add Card3DView component
            Card3DView cardView = cardRoot.AddComponent<Card3DView>();

            // Create quad for card background
            GameObject cardQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            cardQuad.name = "CardQuad";
            cardQuad.transform.SetParent(cardRoot.transform);
            cardQuad.transform.localPosition = Vector3.zero;
            cardQuad.transform.localRotation = Quaternion.identity;
            cardQuad.transform.localScale = new Vector3(2f, 3f, 1f); // Standard card ratio

            // Get renderer and setup material
            MeshRenderer quadRenderer = cardQuad.GetComponent<MeshRenderer>();
            Material cardMaterial = new Material(Shader.Find("Standard"));
            cardMaterial.name = "CardMaterial";
            cardMaterial.color = Color.white;
            quadRenderer.material = cardMaterial;

            // Replace default collider with box collider on root
            DestroyImmediate(cardQuad.GetComponent<MeshCollider>());
            BoxCollider boxCollider = cardRoot.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(2f, 3f, 0.1f);

            // Create TextMeshPro for card name
            GameObject nameObj = new GameObject("CardName");
            nameObj.transform.SetParent(cardRoot.transform);
            nameObj.transform.localPosition = new Vector3(0, 1.2f, -0.01f);
            nameObj.transform.localRotation = Quaternion.identity;
            nameObj.transform.localScale = Vector3.one;

            TextMeshPro nameTMP = nameObj.AddComponent<TextMeshPro>();
            nameTMP.text = "Card Name";
            nameTMP.fontSize = 0.5f;
            nameTMP.alignment = TextAlignmentOptions.Center;
            nameTMP.color = Color.black;
            nameTMP.sortingOrder = 1;

            // Create TextMeshPro for card cost
            var costObj = new GameObject("CardCost");
            costObj.transform.SetParent(cardRoot.transform);
            costObj.transform.localPosition = new Vector3(-0.8f, 1.2f, -0.01f);
            costObj.transform.localRotation = Quaternion.identity;
            costObj.transform.localScale = Vector3.one;

            var costTMP = costObj.AddComponent<TextMeshPro>();
            costTMP.text = "0";
            costTMP.fontSize = 0.6f;
            costTMP.alignment = TextAlignmentOptions.Center;
            costTMP.color = Color.blue;
            costTMP.sortingOrder = 1;

            // Create TextMeshPro for card description
            var descObj = new GameObject("CardDescription");
            descObj.transform.SetParent(cardRoot.transform);
            descObj.transform.localPosition = new Vector3(0, -0.5f, -0.01f);
            descObj.transform.localRotation = Quaternion.identity;
            descObj.transform.localScale = Vector3.one;

            var descTMP = descObj.AddComponent<TextMeshPro>();
            descTMP.text = "Card description goes here.";
            descTMP.fontSize = 0.3f;
            descTMP.alignment = TextAlignmentOptions.Center;
            descTMP.rectTransform.sizeDelta = new Vector2(1.8f, 1f);
            descTMP.textWrappingMode = TextWrappingModes.PreserveWhitespace;
            descTMP.color = Color.black;
            descTMP.sortingOrder = 1;

            // Create MMFeedbacks components
            GameObject drawFeedbackObj = new GameObject("DrawFeedback");
            drawFeedbackObj.transform.SetParent(cardRoot.transform);
            MMFeedbacks drawFeedback = drawFeedbackObj.AddComponent<MMF_Player>();
            drawFeedback.InitializationMode = MMFeedbacks.InitializationModes.Awake;

            GameObject hoverEnterFeedbackObj = new GameObject("HoverEnterFeedback");
            hoverEnterFeedbackObj.transform.SetParent(cardRoot.transform);
            MMFeedbacks hoverEnterFeedback = hoverEnterFeedbackObj.AddComponent<MMF_Player>();
            hoverEnterFeedback.InitializationMode = MMFeedbacks.InitializationModes.Awake;

            GameObject hoverExitFeedbackObj = new GameObject("HoverExitFeedback");
            hoverExitFeedbackObj.transform.SetParent(cardRoot.transform);
            MMFeedbacks hoverExitFeedback = hoverExitFeedbackObj.AddComponent<MMF_Player>();
            hoverExitFeedback.InitializationMode = MMFeedbacks.InitializationModes.Awake;

            GameObject selectFeedbackObj = new GameObject("SelectFeedback");
            selectFeedbackObj.transform.SetParent(cardRoot.transform);
            MMFeedbacks selectFeedback = selectFeedbackObj.AddComponent<MMF_Player>();
            selectFeedback.InitializationMode = MMFeedbacks.InitializationModes.Awake;

            GameObject discardFeedbackObj = new GameObject("DiscardFeedback");
            discardFeedbackObj.transform.SetParent(cardRoot.transform);
            MMFeedbacks discardFeedback = discardFeedbackObj.AddComponent<MMF_Player>();
            discardFeedback.InitializationMode = MMFeedbacks.InitializationModes.Awake;

            // Assign references via SerializedObject (proper way to set private serialized fields)
            SerializedObject serializedCard = new SerializedObject(cardView);3
            serializedCard.FindProperty("cardRenderer").objectReferenceValue = quadRenderer;
            serializedCard.FindProperty("cardNameText").objectReferenceValue = nameTMP;
            serializedCard.FindProperty("cardCostText").objectReferenceValue = costTMP;
            serializedCard.FindProperty("cardDescriptionText").objectReferenceValue = descTMP;
            serializedCard.FindProperty("drawFeedback").objectReferenceValue = drawFeedback;
            serializedCard.FindProperty("hoverEnterFeedback").objectReferenceValue = hoverEnterFeedback;
            serializedCard.FindProperty("hoverExitFeedback").objectReferenceValue = hoverExitFeedback;
            serializedCard.FindProperty("selectFeedback").objectReferenceValue = selectFeedback;
            serializedCard.FindProperty("discardFeedback").objectReferenceValue = discardFeedback;
            serializedCard.ApplyModifiedProperties();

            // Select the created object
            Selection.activeGameObject = cardRoot;

            Debug.Log("Card prefab structure created! Now:");
            Debug.Log("1. Add MMFeedback effects to the feedback GameObjects");
            Debug.Log("2. Assign a card material/sprite");
            Debug.Log("3. Save as prefab in Assets/Prefabs/");
        }
#endif
    }
}
