using System;
using Crookedile.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

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
                LoadSceneTask(SceneManager.LoadSceneAsync(sceneName), sceneName).Forget();
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
                string sceneName = SceneManager.GetSceneByBuildIndex(sceneIndex).name;
                LoadSceneTask(SceneManager.LoadSceneAsync(sceneIndex), sceneName).Forget();
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

        /// <summary>
        /// Drives an in-flight scene load: reports progress, releases activation at 90%,
        /// and fires the completion event. Shared by the name- and index-based overloads.
        /// </summary>
        private async UniTaskVoid LoadSceneTask(AsyncOperation operation, string sceneName)
        {
            _isLoading = true;
            OnSceneLoadStarted?.Invoke(sceneName);

            operation.allowSceneActivation = false;

            while (!operation.isDone)
            {
                float progress = Mathf.Clamp01(operation.progress / 0.9f);
                OnSceneLoadProgress?.Invoke(sceneName, progress);

                if (operation.progress >= 0.9f)
                {
                    operation.allowSceneActivation = true;
                }

                await UniTask.Yield();
            }

            _isLoading = false;
            OnSceneLoadCompleted?.Invoke(sceneName);
            GameLogger.LogInfo("Core", $"Scene loaded: {sceneName}");
        }
    }
}
