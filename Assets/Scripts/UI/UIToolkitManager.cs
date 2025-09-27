using Core;
using Projects;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class UIToolkitManager : Singleton<UIToolkitManager>,
        IEventListener<ResourceChangedEvent>,
        IEventListener<CardPlayedEvent>,
        IEventListener<TurnChangedEvent>,
        IEventListener<ProjectCompletedEvent>,
        IEventListener<RelationshipChangedEvent>
    {
        [Header("UI Documents")]
        public UIDocument gameUIDocument;
        public UIDocument mainMenuUIDocument;
        public UIDocument loadingUIDocument;

        [Header("Visual Tree Assets")]
        public VisualTreeAsset cardElementTemplate;
        public VisualTreeAsset characterElementTemplate;
        public VisualTreeAsset projectElementTemplate;

        private VisualElement gameRoot;
        private VisualElement mainMenuRoot;
        private VisualElement loadingRoot;

        // Game UI Elements
        private Label budgetLabel;
        private Label approvalLabel;
        private Label wealthLabel;
        private Label heatLabel;
        private Label energyLabel;
        private Label impunityLabel;
        private ProgressBar approvalBar;
        private ProgressBar heatBar;

        private VisualElement cardContainer;
        private VisualElement projectContainer;
        private VisualElement relationshipContainer;

        private Button endTurnButton;
        private Label turnInfoLabel;
        private Label seasonLabel;

        // Loading UI
        private ProgressBar loadingProgress;
        private Label loadingText;

        protected override void OnSingletonAwake()
        {
            InitializeUI();
            SubscribeToEvents();
        }

        private void InitializeUI()
        {
            SetupGameUI();
            SetupMainMenuUI();
            SetupLoadingUI();
        }

        private void SetupGameUI()
        {
            if (gameUIDocument == null) return;

            gameRoot = gameUIDocument.rootVisualElement;

            // Resource elements
            budgetLabel = gameRoot.Q<Label>("budget-value");
            approvalLabel = gameRoot.Q<Label>("approval-value");
            wealthLabel = gameRoot.Q<Label>("wealth-value");
            heatLabel = gameRoot.Q<Label>("heat-value");
            energyLabel = gameRoot.Q<Label>("energy-value");
            impunityLabel = gameRoot.Q<Label>("impunity-value");

            approvalBar = gameRoot.Q<ProgressBar>("approval-bar");
            heatBar = gameRoot.Q<ProgressBar>("heat-bar");

            // Container elements
            cardContainer = gameRoot.Q<VisualElement>("card-hand");
            projectContainer = gameRoot.Q<VisualElement>("project-list");
            relationshipContainer = gameRoot.Q<VisualElement>("relationship-list");

            // Control elements
            endTurnButton = gameRoot.Q<Button>("end-turn-button");
            turnInfoLabel = gameRoot.Q<Label>("turn-info");
            seasonLabel = gameRoot.Q<Label>("season-info");

            // Setup button callbacks
            if (endTurnButton != null)
            {
                endTurnButton.clicked += OnEndTurnClicked;
            }

            // Setup other UI callbacks
            SetupUICallbacks();
        }

        private void SetupMainMenuUI()
        {
            if (mainMenuUIDocument == null) return;

            mainMenuRoot = mainMenuUIDocument.rootVisualElement;

            Button newGameButton = mainMenuRoot.Q<Button>("new-game-button");
            Button continueButton = mainMenuRoot.Q<Button>("continue-button");
            Button settingsButton = mainMenuRoot.Q<Button>("settings-button");
            Button quitButton = mainMenuRoot.Q<Button>("quit-button");

            newGameButton?.RegisterCallback<ClickEvent>(evt => SceneController.Instance?.StartNewGame());
            continueButton?.RegisterCallback<ClickEvent>(evt => SceneController.Instance?.LoadGame());
            settingsButton?.RegisterCallback<ClickEvent>(evt => ShowSettings());
            quitButton?.RegisterCallback<ClickEvent>(evt => Application.Quit());

            // Enable/disable continue button based on save file
            if (continueButton != null)
            {
                continueButton.SetEnabled(SaveLoadManager.Instance?.HasSaveFile() ?? false);
            }
        }

        private void SetupLoadingUI()
        {
            if (loadingUIDocument == null) return;

            loadingRoot = loadingUIDocument.rootVisualElement;
            loadingProgress = loadingRoot.Q<ProgressBar>("loading-progress");
            loadingText = loadingRoot.Q<Label>("loading-text");

            HideLoadingScreen();
        }

        public void UpdateResources()
        {
            if (GameManager.Instance?.resourceManager == null) return;

            ResourceManager rm = GameManager.Instance.resourceManager;

            if (budgetLabel != null) budgetLabel.text = $"₱{rm.budget:N0}";
            if (approvalLabel != null) approvalLabel.text = ($"{rm.approval}%");
            if (wealthLabel != null) wealthLabel.text = ($"₱{rm.wealth:N0}");
            if (heatLabel != null) heatLabel.text = ($"{rm.heat}");
            if (energyLabel != null) energyLabel.text = ($"{rm.energy}");
            if (impunityLabel != null) impunityLabel.text = ($"{rm.impunity}");

            if (approvalBar != null)
            {
                approvalBar.value = rm.approval;
                approvalBar.RemoveFromClassList("approval-low");
                approvalBar.RemoveFromClassList("approval-medium");
                approvalBar.RemoveFromClassList("approval-high");

                if (rm.approval < 40) approvalBar.AddToClassList("approval-low");
                else if (rm.approval < 70) approvalBar.AddToClassList("approval-medium");
                else approvalBar.AddToClassList("approval-high");
            }

            if (heatBar != null)
            {
                heatBar.value = rm.heat;
                heatBar.RemoveFromClassList("heat-low");
                heatBar.RemoveFromClassList("heat-medium");
                heatBar.RemoveFromClassList("heat-high");

                if (rm.heat < 30) heatBar.AddToClassList("heat-low");
                else if (rm.heat < 70) heatBar.AddToClassList("heat-medium");
                else heatBar.AddToClassList("heat-high");
            }
        }

        public void UpdateTurnInfo()
        {
            if (GameManager.Instance == null) return;

            if (turnInfoLabel != null) turnInfoLabel.text = ($"Turn {GameManager.Instance.currentTurn}/12");
            if (seasonLabel != null) seasonLabel.text = (GameManager.Instance.currentSeason.ToString());
        }

        public void UpdateCardHand()
        {
            if (cardContainer == null || GameManager.Instance?.cardManager == null) return;

            cardContainer.Clear();

            foreach (Card card in GameManager.Instance.cardManager.hand)
            {
                VisualElement cardElement = CreateCardElement(card);
                cardContainer.Add(cardElement);
            }
        }

        public void UpdateProjects()
        {
            if (projectContainer == null || GameManager.Instance?.projectManager == null) return;

            projectContainer.Clear();

            foreach (Project project in GameManager.Instance.projectManager.GetActiveProjects())
            {
                VisualElement projectElement = CreateProjectElement(project);
                projectContainer.Add(projectElement);
            }
        }

        public void UpdateRelationships()
        {
            if (relationshipContainer == null || GameManager.Instance?.relationshipManager == null) return;

            relationshipContainer.Clear();

            foreach (Character character in GameManager.Instance.relationshipManager._unlockedCharacters)
            {
                VisualElement characterElement = CreateCharacterElement(character);
                relationshipContainer.Add(characterElement);
            }
        }

        private VisualElement CreateCardElement(Card card)
        {
            VisualElement cardElement = cardElementTemplate.CloneTree();

            Label nameLabel = cardElement.Q<Label>("card-name");
            Label typeLabel = cardElement.Q<Label>("card-type");
            Label costLabel = cardElement.Q<Label>("card-cost");
            Label descriptionLabel = cardElement.Q<Label>("card-description");

            if (nameLabel != null) nameLabel.text = (card.cardName);
            if (typeLabel != null) typeLabel.text = (card.cardType.ToString());
            if (costLabel != null) costLabel.text = ($"{card.energyCost} Energy");
            if (descriptionLabel != null) descriptionLabel.text = (card.description);

            // Add card type styling
            cardElement.AddToClassList($"card-{card.cardType.ToString().ToLower()}");

            // Enable/disable based on playability
            if (!card.CanPlay())
            {
                cardElement.AddToClassList("card-disabled");
            }
            else
            {
                cardElement.RegisterCallback<ClickEvent>(evt => OnCardClicked(card));
            }

            return cardElement;
        }

        private VisualElement CreateProjectElement(Project project)
        {
            VisualElement projectElement = projectElementTemplate.CloneTree();

            Label nameLabel = projectElement.Q<Label>("project-name");
            Label progressLabel = projectElement.Q<Label>("project-progress");
            ProgressBar progressBar = projectElement.Q<ProgressBar>("project-progress-bar");

            if (nameLabel != null) nameLabel.text = (project.projectName);
            if (progressLabel != null) progressLabel.text = ($"{project.currentProgress}/{project.GetActualDuration()}");

            if (progressBar != null)
            {
                progressBar.value = project.GetProgressPercentage() * 100;
            }

            return projectElement;
        }

        private VisualElement CreateCharacterElement(Character character)
        {
            VisualElement characterElement = characterElementTemplate.CloneTree();

            Label nameLabel = characterElement.Q<Label>("character-name");
            Label statusLabel = characterElement.Q<Label>("character-status");
            ProgressBar rapportBar = characterElement.Q<ProgressBar>("rapport-bar");

            if (nameLabel != null) nameLabel.text = (character.characterName);
            if (statusLabel != null) statusLabel.text = (character.GetStatus().ToString());

            if (rapportBar != null)
            {
                rapportBar.value = character.currentRapport;
            }

            characterElement.RegisterCallback<ClickEvent>(evt => OnCharacterClicked(character));

            return characterElement;
        }

        private void SetupUICallbacks()
        {
            // Setup project button
            Button projectButton = gameRoot?.Q<Button>("projects-button");
            projectButton?.RegisterCallback<ClickEvent>(evt => ShowProjectSelection());

            // Setup map button
            Button mapButton = gameRoot?.Q<Button>("map-button");
            mapButton?.RegisterCallback<ClickEvent>(evt => ShowMapDetail());

            // Setup settings button
            Button settingsButton = gameRoot?.Q<Button>("settings-button");
            settingsButton?.RegisterCallback<ClickEvent>(evt => ShowSettings());
        }

        public void ShowLoadingScreen()
        {
            loadingUIDocument?.gameObject.SetActive(true);
        }

        public void HideLoadingScreen()
        {
            loadingUIDocument?.gameObject.SetActive(false);
        }

        public void UpdateLoadingProgress(float progress)
        {
            if (loadingProgress != null)
            {
                loadingProgress.value = progress * 100;
            }

            if (loadingText != null)
            {
                loadingText.text = $"Loading... {Mathf.RoundToInt(progress * 100)}%";
            }
        }

        public void UpdateAllUI()
        {
            UpdateResources();
            UpdateTurnInfo();
            UpdateCardHand();
            UpdateProjects();
            UpdateRelationships();
        }

        private void OnEndTurnClicked()
        {
            GameManager.Instance?.EndTurn();
        }

        private void OnCardClicked(Card card)
        {
            GameManager.Instance?.PlayCard(card);
            UpdateAllUI();
        }

        private void OnCharacterClicked(Character character)
        {
            ShowCharacterInteraction(character);
        }

        private void ShowProjectSelection()
        {
            // TODO: Implement project selection popup
            Debug.Log("Show project selection");
        }

        private void ShowMapDetail()
        {
            SceneController.Instance?.OpenMapDetail();
        }

        private void ShowCharacterInteraction(Character character)
        {
            // TODO: Implement character interaction popup
            Debug.Log($"Show interaction with {character.characterName}");
        }

        private void ShowSettings()
        {
            // TODO: Implement settings popup
            Debug.Log("Show settings");
        }

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<ResourceChangedEvent>(this);
            EventBus.Subscribe<CardPlayedEvent>(this);
            EventBus.Subscribe<TurnChangedEvent>(this);
            EventBus.Subscribe<ProjectCompletedEvent>(this);
            EventBus.Subscribe<RelationshipChangedEvent>(this);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<ResourceChangedEvent>(this);
            EventBus.Unsubscribe<CardPlayedEvent>(this);
            EventBus.Unsubscribe<TurnChangedEvent>(this);
            EventBus.Unsubscribe<ProjectCompletedEvent>(this);
            EventBus.Unsubscribe<RelationshipChangedEvent>(this);
        }

        // Event Handlers
        public void HandleEvent(ResourceChangedEvent gameEvent)
        {
            UpdateResources();
        }

        public void HandleEvent(CardPlayedEvent gameEvent)
        {
            UpdateResources();
            UpdateCardHand();
        }

        public void HandleEvent(TurnChangedEvent gameEvent)
        {
            UpdateTurnInfo();
            UpdateAllUI();
        }

        public void HandleEvent(ProjectCompletedEvent gameEvent)
        {
            UpdateProjects();
            UpdateResources();
        }

        public void HandleEvent(RelationshipChangedEvent gameEvent)
        {
            UpdateRelationships();
        }
    }
}