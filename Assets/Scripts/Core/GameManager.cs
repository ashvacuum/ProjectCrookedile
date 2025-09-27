using Relationships;
using UnityEngine;

namespace Core
{
    public class GameManager : Singleton<GameManager>
    {
        [Header("Game State")]
        public GameState currentState = GameState.Planning;
        public int currentTurn = 1;
        public int turnsPerSeason = 12;
        public Season currentSeason = Season.Summer;

        [Header("Managers")]
        public ResourceManager resourceManager;
        public CardManager cardManager;
        public RelationshipManager relationshipManager;
        public ProjectManager projectManager;
        public MapManager mapManager;
        public SeasonManager seasonManager;
        public EnemyManager enemyManager;

        [Header("Game Settings")]
        public float turnDuration = 45f;
        public bool gameWon = false;
        public bool gameLost = false;

        protected override void OnSingletonAwake()
        {
            InitializeGame();
        }

        private void InitializeGame()
        {
            if (resourceManager == null) resourceManager = Object.FindFirstObjectByType<ResourceManager>();
            if (cardManager == null) cardManager = Object.FindFirstObjectByType<CardManager>();
            if (relationshipManager == null) relationshipManager = Object.FindFirstObjectByType<RelationshipManager>();
            if (projectManager == null) projectManager = Object.FindFirstObjectByType<ProjectManager>();
            if (mapManager == null) mapManager = Object.FindFirstObjectByType<MapManager>();
            if (seasonManager == null) seasonManager = Object.FindFirstObjectByType<SeasonManager>();
            if (enemyManager == null) enemyManager = Object.FindFirstObjectByType<EnemyManager>();

            Debug.Log("Corruption Tycoon - Game Initialized");
        }

        public void StartTurn()
        {
            currentState = GameState.Planning;
            int oldTurn = currentTurn;

            resourceManager?.ProcessTurnStart();
            cardManager?.DrawCards();
            projectManager?.UpdateProjects();
            relationshipManager?.ProcessRelationships();
            enemyManager?.ProcessEnemyActions();

            CheckWinConditions();

            EventBus.Publish(new TurnChangedEvent(oldTurn, currentTurn, currentSeason));
        }

        public void EndTurn()
        {
            currentState = GameState.Resolution;

            resourceManager?.ProcessTurnEnd();
            projectManager?.ProcessProjectEffects();
            seasonManager?.ProcessSeasonEffects();

            currentTurn++;

            if (currentTurn > turnsPerSeason)
            {
                AdvanceSeason();
            }

            StartTurn();
        }

        private void AdvanceSeason()
        {
            currentTurn = 1;

            switch (currentSeason)
            {
                case Season.Summer:
                    currentSeason = Season.Rainy;
                    break;
                case Season.Rainy:
                    currentSeason = Season.Christmas;
                    break;
                case Season.Christmas:
                    TriggerElection();
                    break;
            }

            seasonManager?.OnSeasonChange(currentSeason);
        }

        private void TriggerElection()
        {
            int finalApproval = resourceManager.approval;
            int communitySupport = projectManager.GetCompletedProjectsApproval();
            int relationshipBonus = relationshipManager.GetElectionBonus();

            int totalScore = finalApproval + communitySupport + relationshipBonus;

            if (totalScore >= 40)
            {
                gameWon = true;
                Debug.Log($"Election Won! Final Score: {totalScore}");
            }
            else
            {
                gameLost = true;
                Debug.Log($"Election Lost! Final Score: {totalScore}");
            }
        }

        private void CheckWinConditions()
        {
            if (resourceManager.approval < 30)
            {
                gameLost = true;
                Debug.Log("Game Over - Approval too low!");
            }

            if (resourceManager.heat >= 100)
            {
                gameLost = true;
                Debug.Log("Game Over - Too much heat!");
            }
        }

        public void PlayCard(Card card)
        {
            if (currentState != GameState.Action) return;
            if (resourceManager.energy < card.energyCost) return;

            cardManager.PlayCard(card);
            resourceManager.SpendEnergy(card.energyCost);

            EventBus.Publish(new CardPlayedEvent(card));
        }

        public void InitializeNewGame()
        {
            currentTurn = 1;
            currentSeason = Season.Summer;
            gameWon = false;
            gameLost = false;
            currentState = GameState.Planning;

            Debug.Log("New game initialized");
        }

        public void OnGameSceneLoaded()
        {
            EventBus.Publish(new TurnChangedEvent(0, currentTurn, currentSeason));
            Debug.Log("Game scene loaded and UI updated");
        }
    }

    public enum GameState
    {
        Planning,
        Action,
        Resolution
    }

    public enum Season
    {
        Summer,
        Rainy,
        Christmas
    }
}