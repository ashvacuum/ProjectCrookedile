using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Crookedile.Core;

namespace Crookedile.Utilities
{
    public class SceneLoader : Singleton<SceneLoader>
    {
        private bool _isLoading = false;
        public bool IsLoading => _isLoading;

        public event Action<string> OnSceneLoadStarted;
        public event Action<string, float> OnSceneLoadProgress;
        public event Action<string> OnSceneLoadCompleted;

        public void LoadScene(string sceneName, bool async = true)
        {
            if (_isLoading)
            {
                GameLogger.LogWarning("Core", "Scene load already in progress!");
                return;
            }

            if (async)
            {
                StartCoroutine(LoadSceneAsync(sceneName));
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }

        public void LoadScene(int sceneIndex, bool async = true)
        {
            if (_isLoading)
            {
                GameLogger.LogWarning("Core", "Scene load already in progress!");
                return;
            }

            if (async)
            {
                StartCoroutine(LoadSceneAsync(sceneIndex));
            }
            else
            {
                SceneManager.LoadScene(sceneIndex);
            }
        }

        public void ReloadCurrentScene()
        {
            Scene currentScene = SceneManager.GetActiveScene();
            LoadScene(currentScene.name);
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            _isLoading = true;
            OnSceneLoadStarted?.Invoke(sceneName);

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            operation.allowSceneActivation = false;

            while (!operation.isDone)
            {
                float progress = Mathf.Clamp01(operation.progress / 0.9f);
                OnSceneLoadProgress?.Invoke(sceneName, progress);

                if (operation.progress >= 0.9f)
                {
                    operation.allowSceneActivation = true;
                }

                yield return null;
            }

            _isLoading = false;
            OnSceneLoadCompleted?.Invoke(sceneName);
            GameLogger.LogInfo("Core", $"Scene loaded: {sceneName}");
        }

        private IEnumerator LoadSceneAsync(int sceneIndex)
        {
            _isLoading = true;
            string sceneName = SceneManager.GetSceneByBuildIndex(sceneIndex).name;
            OnSceneLoadStarted?.Invoke(sceneName);

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIndex);
            operation.allowSceneActivation = false;

            while (!operation.isDone)
            {
                float progress = Mathf.Clamp01(operation.progress / 0.9f);
                OnSceneLoadProgress?.Invoke(sceneName, progress);

                if (operation.progress >= 0.9f)
                {
                    operation.allowSceneActivation = true;
                }

                yield return null;
            }

            _isLoading = false;
            OnSceneLoadCompleted?.Invoke(sceneName);
            GameLogger.LogInfo("Core", $"Scene loaded: {sceneName}");
        }
    }
}
