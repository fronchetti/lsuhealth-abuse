using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealtimePatient
{
    /// <summary>
    /// Walks a chapter through its scene sequence. This is the whole of the
    /// project's "scene manager": the menu calls <see cref="Begin"/>, each
    /// phase calls <see cref="Advance"/> when the learner is finished, and the
    /// chapter returns to the menu after its last scene.
    /// </summary>
    public static class ChapterFlow
    {
        public const string MenuSceneName = "ChapterSelectScene";

        private static string[] sceneSequence = new string[0];
        private static int sceneIndex = -1;

        /// <summary>Index of the current phase, or -1 outside a chapter.</summary>
        public static int CurrentPhase => sceneIndex;

        public static bool IsInChapter =>
            sceneIndex >= 0 && sceneIndex < sceneSequence.Length;

        public static void Begin(ChapterDefinition chapter)
        {
            if (chapter == null)
            {
                Debug.LogError(
                    "ChapterFlow.Begin was called with no chapter.");
                return;
            }

            string[] sequence = chapter.SceneSequence;

            if (sequence == null || sequence.Length == 0)
            {
                Debug.LogError(
                    $"Chapter '{chapter.DisplayName}' has an empty scene " +
                    $"sequence. Add at least one scene name to it.");
                return;
            }

            GameSession.Current = chapter;
            sceneSequence = sequence;
            sceneIndex = 0;

            Debug.Log(
                $"Starting chapter '{chapter.DisplayName}' " +
                $"({sequence.Length} scene(s)).");

            LoadCurrentScene();
        }

        /// <summary>
        /// Moves to the next scene in the chapter, or back to the menu once
        /// the sequence is exhausted.
        /// </summary>
        public static void Advance()
        {
            if (sceneIndex < 0)
            {
                Debug.LogWarning(
                    "ChapterFlow.Advance was called outside a chapter. " +
                    "Returning to the menu.");

                ReturnToMenu();
                return;
            }

            sceneIndex++;

            if (sceneIndex >= sceneSequence.Length)
            {
                ReturnToMenu();
                return;
            }

            LoadCurrentScene();
        }

        public static void ReturnToMenu()
        {
            sceneSequence = new string[0];
            sceneIndex = -1;
            GameSession.Current = null;

            SceneManager.LoadScene(MenuSceneName);
        }

        private static void LoadCurrentScene()
        {
            string sceneName = sceneSequence[sceneIndex];

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError(
                    $"Scene sequence entry {sceneIndex} is blank. " +
                    $"Returning to the menu.");

                ReturnToMenu();
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnEnterPlayMode()
        {
            sceneSequence = new string[0];
            sceneIndex = -1;
        }
    }
}
