using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RealtimePatient
{
    /// <summary>
    /// One button in the chapter menu. Labels itself from the chapter asset,
    /// so the menu text never drifts from the chapter it launches.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class ChapterSelectButton : MonoBehaviour
    {
        [SerializeField] private ChapterDefinition chapter;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text descriptionLabel;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();

            if (chapter == null)
            {
                Debug.LogError(
                    $"'{name}' has no chapter assigned and will be disabled.",
                    this);

                button.interactable = false;
                return;
            }

            if (titleLabel != null)
                titleLabel.text = chapter.DisplayName;

            if (descriptionLabel != null)
                descriptionLabel.text = chapter.MenuDescription;

            button.onClick.AddListener(StartChapter);
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(StartChapter);
        }

        private void StartChapter()
        {
            // Guard against a second click while the load is in flight.
            button.interactable = false;

            ChapterFlow.Begin(chapter);
        }
    }
}
