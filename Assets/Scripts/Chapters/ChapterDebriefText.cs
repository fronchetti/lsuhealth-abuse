using TMPro;
using UnityEngine;

namespace RealtimePatient
{
    /// <summary>
    /// Fills the debrief screen from the active chapter.
    /// <para>
    /// Note that this only swaps in per-chapter copy: the text is still
    /// authored, not scored. Nothing in the project currently records what the
    /// learner said, so real feedback needs input-audio transcription enabled
    /// on the Realtime session first.
    /// </para>
    /// </summary>
    public sealed class ChapterDebriefText : MonoBehaviour
    {
        [SerializeField] private TMP_Text headlineText;
        [SerializeField] private TMP_Text bodyText;

        private void Start()
        {
            ChapterDefinition chapter = GameSession.Current;

            if (chapter == null)
                return;

            if (headlineText != null &&
                !string.IsNullOrWhiteSpace(chapter.DebriefHeadline))
            {
                headlineText.text = chapter.DebriefHeadline;
            }

            if (bodyText != null &&
                !string.IsNullOrWhiteSpace(chapter.DebriefBody))
            {
                bodyText.text = chapter.DebriefBody;
            }
        }
    }
}
