using UnityEngine;

namespace RealtimePatient
{
    /// <summary>
    /// Holds the chapter the player picked in the menu.
    /// <para>
    /// This is deliberately a static field rather than a persistent
    /// GameObject. A static survives <c>SceneManager.LoadScene</c>, so the
    /// project needs no bootstrap scene and no DontDestroyOnLoad singleton.
    /// It is also set before the next scene loads, which means scene
    /// components can read it in Awake without any ordering guarantees.
    /// </para>
    /// </summary>
    public static class GameSession
    {
        public static ChapterDefinition Current { get; set; }

        public static bool HasChapter => Current != null;

        /// <summary>
        /// Clears the selection when entering play mode. Without this the
        /// value would survive between play sessions whenever Unity's
        /// "Reload Domain" option is disabled, which makes editor testing
        /// depend on whatever ran last.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnEnterPlayMode()
        {
            Current = null;
        }
    }
}
