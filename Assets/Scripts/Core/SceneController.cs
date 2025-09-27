using System.Collections;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core
{
    public class SceneController : Singleton<SceneController>
    {
        [Header("Scene Names")]
        public string bootstrapScene = "Bootstrap";
        public string mainMenuScene = "MainMenu";
        public string gameScene = "Game";
        public string mapDetailScene = "MapDetail";
        public string endGameScene = "EndGame";

        [Header("Transition Settings")]
        public float transitionDuration = 1f;
        public bool useLoadingScreen = true;

        private bool isTransitioning = false;


        public void LoadMainMenu()
        {
            if (!isTransitioning)
            {
                StartCoroutine(LoadSceneAsync(mainMenuScene));
            }
        }

        public void StartNewGame()
        {
            if (!isTransitioning)
            {
                GameManager.Instance?.InitializeNewGame();
                StartCoroutine(LoadSceneAsync(gameScene));
            }
        }

        public void LoadGame()
        {
            if (!isTransitioning)
            {
                SaveLoadManager.Instance?.LoadGame();
                StartCoroutine(LoadSceneAsync(gameScene));
            }
        }

        public void OpenMapDetail()
        {
            if (!isTransitioning && SceneManager.GetActiveScene().name == gameScene)
            {
                StartCoroutine(LoadSceneAdditiveAsync(mapDetailScene));
            }
        }

        public void CloseMapDetail()
        {
            if (!isTransitioning)
            {
                StartCoroutine(UnloadSceneAsync(mapDetailScene));
            }
        }

        public void TriggerGameEnd(bool playerWon)
        {
            if (!isTransitioning)
            {
                GameManager.Instance.gameWon = playerWon;
                GameManager.Instance.gameLost = !playerWon;
                StartCoroutine(LoadSceneAsync(endGameScene));
            }
        }

        public void QuitToMainMenu()
        {
            if (!isTransitioning)
            {
                SaveLoadManager.Instance?.SaveGame();
                StartCoroutine(LoadSceneAsync(mainMenuScene));
            }
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            isTransitioning = true;

            if (useLoadingScreen)
            {
                UIToolkitManager.Instance?.ShowLoadingScreen();
            }

            yield return new WaitForSeconds(transitionDuration * 0.3f);

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            asyncLoad.allowSceneActivation = false;

            while (asyncLoad.progress < 0.9f)
            {
                if (UIToolkitManager.Instance != null)
                {
                    UIToolkitManager.Instance.UpdateLoadingProgress(asyncLoad.progress);
                }
                yield return null;
            }

            yield return new WaitForSeconds(transitionDuration * 0.4f);

            asyncLoad.allowSceneActivation = true;

            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            yield return new WaitForSeconds(transitionDuration * 0.3f);

            if (useLoadingScreen)
            {
                UIToolkitManager.Instance?.HideLoadingScreen();
            }

            isTransitioning = false;
            OnSceneLoaded(sceneName);
        }

        private IEnumerator LoadSceneAdditiveAsync(string sceneName)
        {
            isTransitioning = true;

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            isTransitioning = false;
            OnAdditiveSceneLoaded(sceneName);
        }

        private IEnumerator UnloadSceneAsync(string sceneName)
        {
            isTransitioning = true;

            AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(sceneName);

            while (!asyncUnload.isDone)
            {
                yield return null;
            }

            isTransitioning = false;
        }

        private void OnSceneLoaded(string sceneName)
        {
            Debug.Log($"Scene loaded: {sceneName}");

            switch (sceneName)
            {
                case var name when name == gameScene:
                    InitializeGameScene();
                    break;
                case var name when name == endGameScene:
                    InitializeEndGameScene();
                    break;
            }
        }

        private void OnAdditiveSceneLoaded(string sceneName)
        {
            Debug.Log($"Additive scene loaded: {sceneName}");

            if (sceneName == mapDetailScene)
            {
                InitializeMapDetailScene();
            }
        }

        private void InitializeGameScene()
        {
            UIToolkitManager.Instance?.UpdateAllUI();
            GameManager.Instance?.OnGameSceneLoaded();
        }

        private void InitializeMapDetailScene()
        {
            // Map detail initialization handled by UIToolkitManager
        }

        private void InitializeEndGameScene()
        {
            // End game UI handled by UIToolkitManager
            // TODO: Create EndGameManager or handle end game logic elsewhere
        }

        public bool IsInGame()
        {
            return SceneManager.GetActiveScene().name == gameScene;
        }

        public bool IsMapDetailOpen()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).name == mapDetailScene)
                {
                    return true;
                }
            }
            return false;
        }

        public string GetCurrentSceneName()
        {
            return SceneManager.GetActiveScene().name;
        }
    }
}