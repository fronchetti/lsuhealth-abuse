using UnityEngine;

namespace RealtimePatient
{
    /// <summary>
    /// One playable chapter. Everything that differs between chapters lives
    /// here rather than in scene YAML, so the same scenes serve every chapter
    /// and the prompts can be reviewed as ordinary text files.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Chapter",
        menuName = "LSU Health/Chapter Definition",
        order = 0)]
    public sealed class ChapterDefinition : ScriptableObject
    {
        [Header("Menu")]
        [SerializeField] private string displayName = "Untitled Chapter";

        [TextArea(2, 4)]
        [SerializeField] private string menuDescription = "";

        [Tooltip("Lower numbers appear first in the chapter menu.")]
        [SerializeField] private int menuOrder;

        [Header("Prompts")]
        [Tooltip("Text asset holding the instructor briefing prompt.")]
        [SerializeField] private TextAsset instructorPrompt;

        [Tooltip("Text asset holding the patient persona prompt. " +
                 "Leave empty for chapters with no patient encounter.")]
        [SerializeField] private TextAsset patientPrompt;

        [Header("Voices")]
        [SerializeField] private string instructorVoice = "marin";
        [SerializeField] private string patientVoice = "cedar";

        [Header("On-screen task text")]
        [SerializeField] private string instructorTaskTitle = "Task #1:";

        [TextArea(2, 5)]
        [SerializeField] private string instructorTaskBody = "";

        [SerializeField] private string patientTaskTitle = "Task #2:";

        [TextArea(2, 5)]
        [SerializeField] private string patientTaskBody = "";

        [Header("Debrief")]
        [SerializeField] private string debriefHeadline = "";

        [TextArea(4, 12)]
        [SerializeField] private string debriefBody = "";

        [Header("Flow")]
        [Tooltip("Scene names this chapter visits, in order. The chapter ends " +
                 "and returns to the menu after the last one.")]
        [SerializeField] private string[] sceneSequence = new string[0];

        public string DisplayName => displayName;
        public string MenuDescription => menuDescription;
        public int MenuOrder => menuOrder;
        public string[] SceneSequence => sceneSequence;
        public string DebriefHeadline => debriefHeadline;
        public string DebriefBody => debriefBody;

        /// <summary>
        /// Prompt text for the given role, or null when no asset is assigned.
        /// The caller is expected to fall back to its own serialized default.
        /// </summary>
        public string GetPrompt(ChapterRole role)
        {
            TextAsset asset = role == ChapterRole.Instructor
                ? instructorPrompt
                : patientPrompt;

            return asset != null ? asset.text : null;
        }

        public string GetVoice(ChapterRole role) =>
            role == ChapterRole.Instructor ? instructorVoice : patientVoice;

        public string GetTaskTitle(ChapterRole role) =>
            role == ChapterRole.Instructor ? instructorTaskTitle : patientTaskTitle;

        public string GetTaskBody(ChapterRole role) =>
            role == ChapterRole.Instructor ? instructorTaskBody : patientTaskBody;
    }
}
