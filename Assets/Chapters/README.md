# Chapters

A chapter is data, not a scene. `InstructorScene`, `PatientScene`, and
`LearnerScores` are shared by every chapter; what differs is the
`ChapterDefinition` asset the menu selects before loading them.

## Adding a chapter

1. Write the prompts as `.txt` files in `Prompts/`.
2. Right-click in `Assets/Chapters` → Create → LSU Health → Chapter Definition.
3. Fill in the fields and assign the prompt text assets.
4. Set `sceneSequence` to the scenes the chapter visits, in order.
5. Add a button for it in `ChapterSelectScene` (copy an existing one and change
   its `chapter` reference), or delete the scene and re-run
   **LSU Health → Chapter Setup → 2. Create Chapter Select Scene**.

No new scenes and no code changes are required.

## Why prompts are text assets

Prompts used to live inside the scene `.unity` YAML, where they were escaped
onto folded lines and impossible to review. As `.txt` files they diff normally
and can be edited by clinical faculty without opening Unity.

**Prompt files are sent to the model verbatim.** Do not put notes, TODOs, or
review comments inside them — the model will read them as instructions. Notes
belong in this file.

## Content status

| Chapter | Instructor prompt | Patient prompt |
|---|---|---|
| Tutorial | Written for this feature — teaches push-to-talk only | n/a |
| Alcohol | Migrated unchanged from `InstructorScene.unity` | Migrated unchanged from `PatientScene.unity` (Wayne Boudreaux) |
| Drugs | **Drafted, needs clinical review** | **Drafted, needs clinical review** (Renée Landry) |

The Drugs chapter is a prescription-opioid scenario modelled on the structure
of the alcohol persona: graduated disclosure, defined triggers for opening up
and closing down, and no breakthrough at the end. It has not been reviewed by
a clinician. Treat it as a starting draft, not teaching material.

`Chapter_Drugs` uses the voice `shimmer` to distinguish it from the alcohol
patient. Confirm that voice id is valid for the configured Realtime model.

## Known limitation: the debrief is not scored

`ChapterDebriefText` swaps in per-chapter copy, but the text is authored, not
earned — every learner sees the same feedback for a given chapter.

Real scoring needs a transcript, and the project does not capture one:

- `OpenAIRealtimeClient.OutputTranscriptDelta` fires for the patient's speech
  but has no subscribers, so those words are discarded.
- The `session.update` payload never requests input audio transcription, so
  the learner's own speech is never transcribed at all.

Both halves of the conversation have to be captured before any rubric can be
applied.
