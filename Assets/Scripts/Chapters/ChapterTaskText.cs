using TMPro;
using UnityEngine;

namespace RealtimePatient
{
    /// <summary>
    /// Fills a scene's task title and description from the active chapter, so
    /// the same scene can present a different task per chapter. Leaves the
    /// text alone when no chapter is active, which keeps the scene readable
    /// when it is opened directly in the editor.
    /// </summary>
    public sealed class ChapterTaskText : MonoBehaviour
    {
        [SerializeField] private ChapterRole role = ChapterRole.Patient;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;

        private void Start()
        {
            ChapterDefinition chapter = GameSession.Current;

            if (chapter == null)
                return;

            string title = chapter.GetTaskTitle(role);
            string body = chapter.GetTaskBody(role);

            if (titleText != null && !string.IsNullOrWhiteSpace(title))
                titleText.text = title;

            if (bodyText != null && !string.IsNullOrWhiteSpace(body))
                bodyText.text = body;
        }
    }
}
