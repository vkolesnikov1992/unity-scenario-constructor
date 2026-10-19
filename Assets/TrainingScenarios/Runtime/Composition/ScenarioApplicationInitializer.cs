using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrainingScenarios.Runtime.Composition
{
    public static class ScenarioApplicationInitializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterSceneInitialization()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeActiveScene()
        {
            InitializeScene(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode _)
        {
            InitializeScene(scene);
        }

        private static void InitializeScene(Scene scene)
        {
            var contexts = Object.FindObjectsByType<ScenarioSceneContext>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (var context in contexts)
            {
                if (context.gameObject.scene == scene)
                {
                    context.Initialize();
                }
            }
        }
    }
}
