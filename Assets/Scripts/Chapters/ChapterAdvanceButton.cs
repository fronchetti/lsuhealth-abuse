using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RealtimePatient
{
    /// <summary>
    /// Ends the current phase and moves the chapter forward. On the last scene
    /// of a chapter this returns to the menu, so one component covers both the
    /// "Continue" and "Finish" cases.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class ChapterAdvanceButton : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        [Tooltip("Shown while this is not the final scene of the chapter.")]
        [SerializeField] private string continueText = "Continue";

        [Tooltip("Shown on the final scene of the chapter.")]
        [SerializeField] private string finishText = "Back to menu";

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(Advance);
        }

        private void Start()
        {
            if (label == null)
                return;

            ChapterDefinition chapter = GameSession.Current;
            string[] sequence = chapter != null ? chapter.SceneSequence : null;

            bool isFinalScene =
                sequence == null ||
                sequence.Length == 0 ||
                ChapterFlow.CurrentPhase >= sequence.Length - 1;

            label.text = isFinalScene ? finishText : continueText;
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(Advance);
        }

        private void Advance()
        {
            button.interactable = false;

            // Outside a chapter (scene opened directly in the editor) this
            // still does the sensible thing and heads for the menu.
            ChapterFlow.Advance();
        }
    }
}
