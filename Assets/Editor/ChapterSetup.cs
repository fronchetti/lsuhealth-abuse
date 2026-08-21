using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RealtimePatient.EditorTools
{
    /// <summary>
    /// One-time setup for the chapter system. Each step is idempotent and
    /// logs what it touched, so they can be run individually and re-run
    /// safely. Scenes are under source control, so anything here can be
    /// undone with git.
    /// </summary>
    public static class ChapterSetup
    {
        private const string ChapterFolder = "Assets/Chapters";
        private const string PromptFolder = "Assets/Chapters/Prompts";
        private const string SceneFolder = "Assets/Scenes";

        private const string MenuScenePath =
            SceneFolder + "/ChapterSelectScene.unity";

        private const string InstructorScenePath =
            SceneFolder + "/InstructorScene.unity";

        private const string PatientScenePath =
            SceneFolder + "/PatientScene.unity";

        private const string DebriefScenePath =
            SceneFolder + "/LearnerScores.unity";

        // ------------------------------------------------------------------
        // Menu entries
        // ------------------------------------------------------------------

        [MenuItem("LSU Health/Chapter Setup/Run All Steps", false, 0)]
        public static void RunAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            CreateChapterAssets();
            CreateMenuScene();
            WireExistingScenes();
            UpdateBuildSettings();

            Debug.Log("[ChapterSetup] All steps finished. " +
                      "Open ChapterSelectScene and press Play.");
        }

        [MenuItem("LSU Health/Chapter Setup/1. Create Chapter Assets", false, 20)]
        public static void CreateChapterAssets()
        {
            EnsureFolder(ChapterFolder);

            MakeChapter(
                assetName: "Chapter_Tutorial",
                displayName: "Tutorial",
                description: "Learn the microphone controls before starting a " +
                             "patient encounter.",
                order: 0,
                instructorPrompt: "Tutorial_Instructor",
                patientPrompt: null,
                instructorVoice: "marin",
                patientVoice: "cedar",
                instructorTitle: "Tutorial",
                instructorBody: "Hold Ctrl to speak and release it to send your " +
                                "turn. Follow the guide's instructions.",
                patientTitle: string.Empty,
                patientBody: string.Empty,
                debriefHeadline: string.Empty,
                debriefBody: string.Empty,
                sceneSequence: new[] { "InstructorScene" });

            MakeChapter(
                assetName: "Chapter_Alcohol",
                displayName: "Alcohol",
                description: "Screen a guarded patient for alcohol use using " +
                             "respectful, nonjudgmental communication.",
                order: 1,
                instructorPrompt: "Alcohol_Instructor",
                patientPrompt: "Alcohol_Patient",
                instructorVoice: "marin",
                patientVoice: "cedar",
                instructorTitle: "Task #1:",
                instructorBody: "Meet with your instructor to learn about the " +
                                "upcoming patient encounter, review the " +
                                "communication objectives, and prepare for the " +
                                "alcohol screening exercise.",
                patientTitle: "Task #2:",
                patientBody: "Speak with Wayne Boudreaux about his alcohol use, " +
                             "explore how it may be affecting his health, and " +
                             "encourage reflection through respectful, " +
                             "nonjudgmental communication.",
                debriefHeadline: "You did a great job!",
                debriefBody:
                    "Well done! You established rapport, maintained a respectful " +
                    "and nonjudgmental tone, and encouraged the patient to " +
                    "discuss his alcohol use. Continue strengthening your " +
                    "reflective listening and exploring the patient's readiness " +
                    "to change before offering recommendations.\n\n" +
                    "To improve, use more reflective statements, avoid moving " +
                    "too quickly to advice, and explore the patient's motivation " +
                    "and readiness for change in greater depth.",
                sceneSequence: new[]
                {
                    "InstructorScene", "PatientScene", "LearnerScores",
                });

            MakeChapter(
                assetName: "Chapter_Drugs",
                displayName: "Drugs",
                description: "Explore a patient's prescription opioid use " +
                             "without stigma, suspicion, or premature advice.",
                order: 2,
                instructorPrompt: "Drugs_Instructor",
                patientPrompt: "Drugs_Patient",
                instructorVoice: "marin",
                patientVoice: "shimmer",
                instructorTitle: "Task #1:",
                instructorBody: "Meet with your instructor to learn about the " +
                                "upcoming patient encounter and review the " +
                                "communication objectives for discussing " +
                                "prescription medication use.",
                patientTitle: "Task #2:",
                patientBody: "Speak with Renee Landry about how she is using her " +
                             "pain medication. Stay curious rather than " +
                             "accusatory, and separate concern for her from " +
                             "suspicion of her.",
                debriefHeadline: "Encounter complete",
                debriefBody:
                    "You completed the encounter. Consider whether you " +
                    "acknowledged that her pain was real, whether you asked " +
                    "permission before raising concerns, and whether you " +
                    "noticed deflection that arrived as helpfulness.\n\n" +
                    "This text is placeholder copy. Scored feedback requires " +
                    "capturing the conversation transcript, which the Realtime " +
                    "session does not yet request.",
                sceneSequence: new[]
                {
                    "InstructorScene", "PatientScene", "LearnerScores",
                });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("LSU Health/Chapter Setup/2. Create Chapter Select Scene", false, 21)]
        public static void CreateMenuScene()
        {
            if (System.IO.File.Exists(MenuScenePath))
            {
                Debug.Log("[ChapterSetup] ChapterSelectScene already exists; " +
                          "leaving it alone. Delete it first to regenerate.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (LoadChapters().Count == 0)
            {
                Debug.LogError("[ChapterSetup] No chapter assets found. " +
                               "Run step 1 first.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Load the chapters only AFTER the new scene exists. NewScene runs
            // an unload-unused-assets pass, which leaves any references taken
            // beforehand with stale instance IDs: reads still work (Unity
            // reloads the asset by GUID) but assigning them to a
            // SerializedProperty silently serializes as null.
            List<ChapterDefinition> chapters = LoadChapters();

            // Camera: without one Unity renders nothing but the "No cameras
            // rendering" message, even for a Screen Space Overlay canvas.
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.09f, 0.10f, 0.13f);
            cameraObject.AddComponent<AudioListener>();

            CreateEventSystem();

            // Canvas
            var canvasObject = new GameObject("Canvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode =
                CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();

            CreateLabel(
                parent: canvasObject.transform,
                name: "Title",
                text: "Select a Chapter",
                fontSize: 64f,
                alignment: TextAlignmentOptions.Center,
                anchoredPosition: new Vector2(0f, 320f),
                size: new Vector2(1000f, 90f),
                color: Color.white);

            CreateLabel(
                parent: canvasObject.transform,
                name: "Subtitle",
                text: "Hold Ctrl to speak during an encounter.",
                fontSize: 26f,
                alignment: TextAlignmentOptions.Center,
                anchoredPosition: new Vector2(0f, 250f),
                size: new Vector2(1000f, 50f),
                color: new Color(0.68f, 0.72f, 0.78f));

            float y = 110f;

            foreach (ChapterDefinition chapter in chapters)
            {
                CreateChapterButton(canvasObject.transform, chapter, y);
                y -= 160f;
            }

            EditorSceneManager.SaveScene(scene, MenuScenePath);

            Debug.Log($"[ChapterSetup] Created {MenuScenePath} with " +
                      $"{chapters.Count} chapter button(s).");
        }

        [MenuItem("LSU Health/Chapter Setup/3. Wire Existing Scenes", false, 22)]
        public static void WireExistingScenes()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            ChapterDefinition alcohol =
                LoadChapter("Chapter_Alcohol");

            if (alcohol == null)
            {
                Debug.LogError("[ChapterSetup] Chapter_Alcohol not found. " +
                               "Run step 1 first.");
                return;
            }

            WireInstructorScene(alcohol);
            WirePatientScene(alcohol);
            WireDebriefScene();
        }

        [MenuItem("LSU Health/Chapter Setup/4. Update Build Settings", false, 23)]
        public static void UpdateBuildSettings()
        {
            string[] ordered =
            {
                MenuScenePath,
                InstructorScenePath,
                PatientScenePath,
                DebriefScenePath,
            };

            var scenes = new List<EditorBuildSettingsScene>();

            foreach (string path in ordered)
            {
                if (!System.IO.File.Exists(path))
                {
                    Debug.LogWarning(
                        $"[ChapterSetup] Skipping missing scene {path}.");
                    continue;
                }

                scenes.Add(new EditorBuildSettingsScene(path, true));
            }

            // Keep any other scenes the project already listed, after ours.
            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            {
                if (ordered.Contains(existing.path))
                    continue;

                scenes.Add(existing);
            }

            EditorBuildSettings.scenes = scenes.ToArray();

            Debug.Log("[ChapterSetup] Build settings order: " +
                      string.Join(", ",
                          scenes.Select(s =>
                              System.IO.Path.GetFileNameWithoutExtension(s.path))));
        }

        [MenuItem("LSU Health/Chapter Setup/Repair Chapter Button References",
            false, 40)]
        public static void RepairButtonReferences()
        {
            if (!System.IO.File.Exists(MenuScenePath))
            {
                Debug.LogError($"[ChapterSetup] {MenuScenePath} does not exist. " +
                               "Run step 2 first.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Scene scene = EditorSceneManager.OpenScene(
                MenuScenePath, OpenSceneMode.Single);

            List<ChapterDefinition> chapters = LoadChapters();

            if (chapters.Count == 0)
            {
                Debug.LogError("[ChapterSetup] No chapter assets found.");
                return;
            }

            ChapterSelectButton[] buttons = Object.FindObjectsByType<ChapterSelectButton>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (buttons.Length == 0)
            {
                Debug.LogError("[ChapterSetup] No ChapterSelectButton found in " +
                               "the menu scene.");
                return;
            }

            int repaired = 0;

            foreach (ChapterSelectButton button in buttons)
            {
                var so = new SerializedObject(button);
                SerializedProperty property = so.FindProperty("chapter");

                if (property.objectReferenceValue != null)
                    continue;

                // Buttons are named "Button - <DisplayName>".
                ChapterDefinition match = chapters.FirstOrDefault(c =>
                    button.name.EndsWith(c.DisplayName,
                        System.StringComparison.OrdinalIgnoreCase));

                if (match == null)
                {
                    Debug.LogWarning(
                        $"[ChapterSetup] Could not tell which chapter " +
                        $"'{button.name}' belongs to. Assign it by hand.",
                        button);
                    continue;
                }

                property.objectReferenceValue = match;
                so.ApplyModifiedPropertiesWithoutUndo();

                so.Update();

                if (so.FindProperty("chapter").objectReferenceValue == null)
                {
                    Debug.LogError(
                        $"[ChapterSetup] Still could not bind " +
                        $"'{match.DisplayName}' to '{button.name}'.",
                        button);
                    continue;
                }

                repaired++;
                Debug.Log($"[ChapterSetup] Bound '{match.DisplayName}' to " +
                          $"'{button.name}'.");
            }

            if (repaired == 0)
            {
                Debug.Log("[ChapterSetup] Nothing needed repair.");
                return;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"[ChapterSetup] Repaired {repaired} button " +
                      $"reference(s) and saved the scene.");
        }

        // ------------------------------------------------------------------
        // Chapter assets
        // ------------------------------------------------------------------

        private static void MakeChapter(
            string assetName,
            string displayName,
            string description,
            int order,
            string instructorPrompt,
            string patientPrompt,
            string instructorVoice,
            string patientVoice,
            string instructorTitle,
            string instructorBody,
            string patientTitle,
            string patientBody,
            string debriefHeadline,
            string debriefBody,
            string[] sceneSequence)
        {
            string path = $"{ChapterFolder}/{assetName}.asset";

            if (AssetDatabase.LoadAssetAtPath<ChapterDefinition>(path) != null)
            {
                Debug.Log($"[ChapterSetup] {assetName} already exists; " +
                          $"leaving its values alone.");
                return;
            }

            var chapter = ScriptableObject.CreateInstance<ChapterDefinition>();
            AssetDatabase.CreateAsset(chapter, path);

            var so = new SerializedObject(chapter);

            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("menuDescription").stringValue = description;
            so.FindProperty("menuOrder").intValue = order;
            so.FindProperty("instructorVoice").stringValue = instructorVoice;
            so.FindProperty("patientVoice").stringValue = patientVoice;
            so.FindProperty("instructorTaskTitle").stringValue = instructorTitle;
            so.FindProperty("instructorTaskBody").stringValue = instructorBody;
            so.FindProperty("patientTaskTitle").stringValue = patientTitle;
            so.FindProperty("patientTaskBody").stringValue = patientBody;
            so.FindProperty("debriefHeadline").stringValue = debriefHeadline;
            so.FindProperty("debriefBody").stringValue = debriefBody;

            so.FindProperty("instructorPrompt").objectReferenceValue =
                LoadPrompt(instructorPrompt);

            so.FindProperty("patientPrompt").objectReferenceValue =
                LoadPrompt(patientPrompt);

            SerializedProperty sequence = so.FindProperty("sceneSequence");
            sequence.arraySize = sceneSequence.Length;

            for (int i = 0; i < sceneSequence.Length; i++)
                sequence.GetArrayElementAtIndex(i).stringValue = sceneSequence[i];

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(chapter);

            Debug.Log($"[ChapterSetup] Created {path}.");
        }

        private static TextAsset LoadPrompt(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;

            string path = $"{PromptFolder}/{fileName}.txt";
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);

            if (asset == null)
                Debug.LogWarning($"[ChapterSetup] Prompt not found: {path}");

            return asset;
        }

        private static List<ChapterDefinition> LoadChapters()
        {
            return AssetDatabase
                .FindAssets("t:ChapterDefinition", new[] { ChapterFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ChapterDefinition>)
                .Where(c => c != null)
                .OrderBy(c => c.MenuOrder)
                .ToList();
        }

        private static ChapterDefinition LoadChapter(string assetName)
        {
            return AssetDatabase.LoadAssetAtPath<ChapterDefinition>(
                $"{ChapterFolder}/{assetName}.asset");
        }

        // ------------------------------------------------------------------
        // Scene wiring
        // ------------------------------------------------------------------

        private static void WireInstructorScene(ChapterDefinition fallback)
        {
            Scene scene = EditorSceneManager.OpenScene(
                InstructorScenePath, OpenSceneMode.Single);

            bool changed = false;

            changed |= SetClientRole(ChapterRole.Instructor, fallback);
            changed |= EnsureEventSystem();
            changed |= AddTaskText(ChapterRole.Instructor);
            changed |= AddAdvanceButton("Continue", "Back to menu");

            SaveIfChanged(scene, changed, "InstructorScene");
        }

        private static void WirePatientScene(ChapterDefinition fallback)
        {
            Scene scene = EditorSceneManager.OpenScene(
                PatientScenePath, OpenSceneMode.Single);

            bool changed = false;

            changed |= SetClientRole(ChapterRole.Patient, fallback);
            changed |= EnsureEventSystem();
            changed |= AddTaskText(ChapterRole.Patient);
            changed |= AddAdvanceButton("Continue", "Back to menu");

            SaveIfChanged(scene, changed, "PatientScene");
        }

        private static void WireDebriefScene()
        {
            Scene scene = EditorSceneManager.OpenScene(
                DebriefScenePath, OpenSceneMode.Single);

            bool changed = false;

            changed |= EnsureEventSystem();
            changed |= AddDebriefText();
            changed |= AddAdvanceButton("Back to menu", "Back to menu");

            SaveIfChanged(scene, changed, "LearnerScores");
        }

        private static void SaveIfChanged(Scene scene, bool changed, string label)
        {
            if (!changed)
            {
                Debug.Log($"[ChapterSetup] {label} already wired; no changes.");
                return;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[ChapterSetup] Wired and saved {label}.");
        }

        private static bool SetClientRole(
            ChapterRole role, ChapterDefinition fallback)
        {
            OpenAIRealtimeClient client =
                Object.FindFirstObjectByType<OpenAIRealtimeClient>();

            if (client == null)
            {
                Debug.LogWarning(
                    "[ChapterSetup] No OpenAIRealtimeClient in this scene.");
                return false;
            }

            var so = new SerializedObject(client);
            SerializedProperty roleProperty = so.FindProperty("role");
            SerializedProperty fallbackProperty = so.FindProperty("fallbackChapter");

            if (roleProperty == null || fallbackProperty == null)
            {
                Debug.LogError(
                    "[ChapterSetup] OpenAIRealtimeClient is missing the 'role' " +
                    "or 'fallbackChapter' field. Did the script recompile?");
                return false;
            }

            bool alreadySet =
                roleProperty.intValue == (int)role &&
                fallbackProperty.objectReferenceValue == fallback;

            if (alreadySet)
                return false;

            roleProperty.intValue = (int)role;
            fallbackProperty.objectReferenceValue = fallback;
            so.ApplyModifiedPropertiesWithoutUndo();

            return true;
        }

        private static bool AddTaskText(ChapterRole role)
        {
            if (Object.FindFirstObjectByType<ChapterTaskText>() != null)
                return false;

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();

            if (canvas == null)
            {
                Debug.LogWarning("[ChapterSetup] No Canvas in this scene.");
                return false;
            }

            List<TMP_Text> labels = SortedLabels(canvas);

            if (labels.Count < 2)
            {
                Debug.LogWarning(
                    "[ChapterSetup] Expected two TMP labels on the canvas, " +
                    $"found {labels.Count}. Wire ChapterTaskText by hand.");
                return false;
            }

            var binder = canvas.gameObject.AddComponent<ChapterTaskText>();
            var so = new SerializedObject(binder);

            so.FindProperty("role").intValue = (int)role;
            so.FindProperty("titleText").objectReferenceValue = labels[0];
            so.FindProperty("bodyText").objectReferenceValue = labels[1];
            so.ApplyModifiedPropertiesWithoutUndo();

            return true;
        }

        private static bool AddDebriefText()
        {
            if (Object.FindFirstObjectByType<ChapterDebriefText>() != null)
                return false;

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();

            if (canvas == null)
                return false;

            // The debrief scene has two canvases; collect labels from all.
            List<TMP_Text> labels = SortContentLabels(
                Object.FindObjectsByType<TMP_Text>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None));

            if (labels.Count < 2)
            {
                Debug.LogWarning(
                    "[ChapterSetup] Expected two TMP labels in LearnerScores, " +
                    $"found {labels.Count}.");
                return false;
            }

            var binder = canvas.gameObject.AddComponent<ChapterDebriefText>();
            var so = new SerializedObject(binder);

            so.FindProperty("headlineText").objectReferenceValue = labels[0];
            so.FindProperty("bodyText").objectReferenceValue = labels[1];
            so.ApplyModifiedPropertiesWithoutUndo();

            return true;
        }

        /// <summary>
        /// Content labels under a canvas, shortest text first, so the title is
        /// index 0 and the body is index 1. Labels belonging to a button are
        /// excluded: "Continue" is the same length as "Task #1:" and would
        /// otherwise make the classification depend on wiring order.
        /// </summary>
        private static List<TMP_Text> SortedLabels(Canvas canvas)
        {
            return SortContentLabels(
                canvas.GetComponentsInChildren<TMP_Text>(true));
        }

        private static List<TMP_Text> SortContentLabels(
            IEnumerable<TMP_Text> labels)
        {
            return labels
                .Where(t => t.GetComponentInParent<Button>() == null)
                .OrderBy(t => (t.text ?? string.Empty).Length)
                .ToList();
        }

        private static bool AddAdvanceButton(
            string continueText, string finishText)
        {
            if (Object.FindFirstObjectByType<ChapterAdvanceButton>() != null)
                return false;

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();

            if (canvas == null)
                return false;

            var buttonObject = new GameObject("Advance Button");
            buttonObject.transform.SetParent(canvas.transform, false);

            var rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-40f, 40f);
            rect.sizeDelta = new Vector2(260f, 70f);

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.20f, 0.38f, 0.62f);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            TMP_Text label = CreateLabel(
                parent: buttonObject.transform,
                name: "Label",
                text: continueText,
                fontSize: 28f,
                alignment: TextAlignmentOptions.Center,
                anchoredPosition: Vector2.zero,
                size: Vector2.zero,
                color: Color.white,
                stretch: true);

            var advance = buttonObject.AddComponent<ChapterAdvanceButton>();
            var so = new SerializedObject(advance);
            so.FindProperty("label").objectReferenceValue = label;
            so.FindProperty("continueText").stringValue = continueText;
            so.FindProperty("finishText").stringValue = finishText;
            so.ApplyModifiedPropertiesWithoutUndo();

            return true;
        }

        // ------------------------------------------------------------------
        // UI construction helpers
        // ------------------------------------------------------------------

        private static bool EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
                return false;

            CreateEventSystem();

            Debug.Log("[ChapterSetup] Added a missing EventSystem " +
                      "(UI clicks would not have worked without it).");
            return true;
        }

        private static void CreateEventSystem()
        {
            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();

            // This project is set to "Input System Package (New)" only, so the
            // legacy StandaloneInputModule would silently do nothing.
            var module =
                eventSystemObject.AddComponent<InputSystemUIInputModule>();

            MethodInfo assignDefaults = typeof(InputSystemUIInputModule)
                .GetMethod("AssignDefaultActions",
                    BindingFlags.Public | BindingFlags.Instance);

            if (assignDefaults != null)
            {
                assignDefaults.Invoke(module, null);
            }
            else
            {
                var actions = AssetDatabase
                    .LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
                        "Assets/InputSystem_Actions.inputactions");

                if (actions != null)
                    module.actionsAsset = actions;
                else
                    Debug.LogWarning(
                        "[ChapterSetup] Could not assign UI input actions. " +
                        "Assign them on the EventSystem by hand if clicks " +
                        "do not register.");
            }
        }

        private static void CreateChapterButton(
            Transform parent, ChapterDefinition chapter, float y)
        {
            var buttonObject = new GameObject($"Button - {chapter.DisplayName}");
            buttonObject.transform.SetParent(parent, false);

            var rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(760f, 130f);

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.16f, 0.19f, 0.25f);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.24f, 0.30f, 0.40f);
            colors.pressedColor = new Color(0.12f, 0.15f, 0.20f);
            button.colors = colors;

            TMP_Text title = CreateLabel(
                parent: buttonObject.transform,
                name: "Title",
                text: chapter.DisplayName,
                fontSize: 38f,
                alignment: TextAlignmentOptions.Left,
                anchoredPosition: new Vector2(24f, 32f),
                size: new Vector2(700f, 46f),
                color: Color.white,
                leftAligned: true);

            TMP_Text description = CreateLabel(
                parent: buttonObject.transform,
                name: "Description",
                text: chapter.MenuDescription,
                fontSize: 22f,
                alignment: TextAlignmentOptions.TopLeft,
                anchoredPosition: new Vector2(24f, -22f),
                size: new Vector2(700f, 56f),
                color: new Color(0.70f, 0.75f, 0.82f),
                leftAligned: true);

            var selectButton = buttonObject.AddComponent<ChapterSelectButton>();
            var so = new SerializedObject(selectButton);

            so.FindProperty("chapter").objectReferenceValue = Reacquire(chapter);
            so.FindProperty("titleLabel").objectReferenceValue = title;
            so.FindProperty("descriptionLabel").objectReferenceValue = description;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Verify rather than trust: a silently null reference here is the
            // difference between a working menu and three dead buttons.
            so.Update();

            if (so.FindProperty("chapter").objectReferenceValue == null)
            {
                Debug.LogError(
                    $"[ChapterSetup] Failed to bind '{chapter.DisplayName}' to " +
                    $"its button. Run 'Repair Chapter Button References'.",
                    buttonObject);
            }
        }

        /// <summary>
        /// Re-reads an asset from its path so the reference is guaranteed to
        /// carry a live instance ID at the moment it is assigned.
        /// </summary>
        private static ChapterDefinition Reacquire(ChapterDefinition chapter)
        {
            if (chapter == null)
                return null;

            string path = AssetDatabase.GetAssetPath(chapter);

            if (string.IsNullOrEmpty(path))
                return chapter;

            return AssetDatabase.LoadAssetAtPath<ChapterDefinition>(path)
                   ?? chapter;
        }

        private static TMP_Text CreateLabel(
            Transform parent,
            string name,
            string text,
            float fontSize,
            TextAlignmentOptions alignment,
            Vector2 anchoredPosition,
            Vector2 size,
            Color color,
            bool stretch = false,
            bool leftAligned = false)
        {
            var labelObject = new GameObject(name);
            labelObject.transform.SetParent(parent, false);

            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;      // never swallow the button's click

            if (label.font == null)
            {
                var font = TMP_Settings.defaultFontAsset;

                if (font == null)
                {
                    font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                        "Assets/TextMesh Pro/Resources/Fonts & Materials/" +
                        "LiberationSans SDF.asset");
                }

                if (font != null)
                    label.font = font;
            }

            var rect = labelObject.GetComponent<RectTransform>();

            if (stretch)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                return label;
            }

            if (leftAligned)
            {
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0f, 0.5f);
            }
            else
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
            }

            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            return label;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = System.IO.Path.GetDirectoryName(path)
                .Replace('\\', '/');

            string leaf = System.IO.Path.GetFileName(path);

            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
