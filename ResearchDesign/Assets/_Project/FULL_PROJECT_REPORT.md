# Agentic Browser - Full Technical Report

Date: 2026-04-20
Unity version: 6000.3.6f1
Project root: Assets/_Project
Supporting raw inventories: `scene_prefab_names.txt`, `scene_prefab_scripts.txt`

## 1) Executive Summary

This project is a Unity-based serious game / guided training experience built around a browser simulation. The player is taught how to search, inspect, navigate, ask the in-game assistant for help, and complete structured digital tasks across multiple levels. The project combines scene-based flow, data-driven browser states, event-driven objective tracking, score calculation, achievement unlocking, and Firebase/WebGL telemetry.

At the current final-project stage, the codebase has six main pillars:

- Browser simulation: `ScreenState`, `SearchIntentRegistry`, `SearchResultSet`, `ScreenController`, `SearchUIManager`, and `ButtonUIManager` together drive what the fake browser looks like.
- Mission logic: `LevelObjectiveTracker` validates player actions against level steps, handles mistakes, hint logic, progress, and win flow.
- Tutorial scaffolding: the original `Tutorial` scene still exists, and `Tutorial V2` now provides a guided, in-context interactive tutorial using `TutorialSequenceManager`.
- Robot guidance: `RobotTextManager`, `HintButton`, `MissionButton`, idle prompting, jump animation, and helper objects create the guided/gamified teaching layer.
- Progression and motivation: `LevelStatsManager`, `AchievementManager`, `TitleScreenLevelLockManager`, `LevelUnlock`, and the progress bar systems create score, unlock, and reward feedback.
- Telemetry and persistence: `FirebaseManager`, `FirebaseTelemetryManager`, `UserID`, and `GameSessionData` capture player identity, attempts, progress, and replay data.

This document is the updated final-pass report. It replaces outdated assumptions from the previous report and reflects the project as it currently exists in the repository.

## 2) Methodology Used To Build This Report

This report was refreshed using the following sources:

- Direct inspection of the current custom scripts in `Assets/_Project/Scripts`.
- Direct inspection of the current report file and comparison against current code.
- Scene inventory under `Assets/_Project/Scenes`.
- Prefab inventory under `Assets/_Project/Prefabs` and `_Project/animation`.
- Scene/prefab YAML support files already present in the project:
  - `Assets/_Project/scene_prefab_names.txt`
  - `Assets/_Project/scene_prefab_scripts.txt`
- Scene attachment verification for the scripts that changed most recently, especially:
  - `TutorialSequenceManager`
  - `MissionButton`
  - `BetweenLevels`
  - `AchievementManager`
  - `TitleScreenLevelLockManager`
  - `LevelUnlock`

Important scope note:

- This report focuses on the gameplay scenes and the custom project scripts under `Assets/_Project/Scripts`.
- Third-party demo scenes and art-package examples are excluded except where they materially affect gameplay, such as the progress bar package.
- The raw object-name and script-reference inventories still remain the best source for every single YAML object entry. This report interprets those inventories into a readable architecture document.

Important build note:

- The repository contains the full gameplay scene set listed below.
- `ProjectSettings/EditorBuildSettings.asset` currently enables only `Assets/_Project/Scenes/Foundation sandbox.unity` in Build Settings.
- That means the report covers the actual project content in the repo, not just the one scene currently enabled for build.

## 3) Current Project Layout

## 3.1 Gameplay Scene Inventory

Primary project scenes found under `Assets/_Project/Scenes`:

- `Foundation sandbox.unity`
- `level 1.unity`
- `level2.unity`
- `level3.unity`
- `level4.unity`
- `between levels/Title Screen.unity`
- `between levels/Tutorial.unity`
- `between levels/Tutorial V2.unity`
- `between levels/Select Level.unity`
- `between levels/level 1 Assistant.unity`
- `between levels/level 2 Assistant.unity`
- `between levels/level 3 Assistant.unity`
- `between levels/level 4 Assistant.unity`
- `between levels/ENDING.unity`

Not included as gameplay flow scenes:

- Unity template/demo scenes under `Assets/Scenes` and asset-package demo folders.
- Recovery scenes under `Assets/_Recovery`.

## 3.2 Scene Flow Intent

The intended user-facing flow is now clearly split into three layers:

- Entry and onboarding: `Title Screen`, `Tutorial`, `Tutorial V2`
- Hub and progression: `Select Level`, assistant interstitial scenes, unlock visuals
- Interactive tasks: `level 1`, `level2`, `level3`, `level4`, with `ENDING` as the closing scene

## 3.3 Custom Prefab Inventory

Custom project-prefab set currently found:

- `Prefabs/Sandbox.prefab`
- `Prefabs/UI/assistant.prefab`
- `Prefabs/Core/HotspotButtonPrefab.prefab`
- `Prefabs/Menu.prefab`
- `Prefabs/MissionButton.prefab`
- `Prefabs/lock manager.prefab`
- `Prefabs/Background animation.prefab`
- `Prefabs/Robot finder Animation .prefab`
- `Prefabs/Step1 Animation.prefab`
- `Prefabs/FireWorkWin.prefab`
- `animation/FireWork.prefab`

Key usage observations:

- `Sandbox.prefab` is the browser-shell foundation and is referenced heavily in sandbox/level scenes.
- `MissionButton.prefab` is now reused in `Tutorial V2` and level scenes.
- `lock manager.prefab` wraps the `LevelUnlock` sequence and is used in later assistant scenes.
- `FireWork.prefab` and `FireWorkWin.prefab` represent reusable effect holders for completion feedback.

## 3.4 Data and Content Assets

Most important content/data assets:

- `Data/Sandbox/SIR_Sandbox.asset`: free-text search intent registry
- `Data/Sandbox/APR_New.asset`: assistant prompt registry, including fallback response text
- `Data/Sandbox/States/*.asset`: browser states for generic search, level-specific pages, and assistant/tab contexts
- `Data/Sandbox/Images/*`: source screenshots/sprites backing the browser simulation

State families identified in current data:

- Generic browser/result states: `Assistant`, `Images`, `Links`, `SB_NewTab`
- Search-driven states for sandbox examples: laptops, flights, forms, AI safety, etc.
- Level 2 states: start, utility/help, OS-specific download states
- Level 3 states: start, booking, booked
- Level 4 states: page chain, done-page chain, CV/submission states

## 4) Scene-by-Scene Report

## 4.1 Title Screen.unity

Purpose:

- Lightweight entry scene that starts the experience.

Observed structure:

- Main Camera
- Canvas
- Image
- Button
- EventSystem

How it works:

- This is a minimal splash / launch scene.
- Its job is to present a branded first screen and move the player into the onboarding flow.
- Compared with later scenes, it contains very little game logic and acts mainly as a gateway scene.

Why this scene exists:

- It cleanly separates the first impression of the game from the more complex tutorial and level-selection scenes.

## 4.2 Tutorial.unity

Purpose:

- Original tutorial/interstitial scene.

Observed structure:

- Main Camera
- Canvas
- Image
- Button
- EventSystem

How it works:

- This scene is structurally similar to `Title Screen` and behaves more like a simple explainer slide than a full interactive tutorial.
- It represents the earlier tutorial approach before `Tutorial V2` was added.

Why this scene still matters:

- It documents the project's earlier onboarding approach and may still be useful as a fallback or simplified introduction scene.

## 4.3 Tutorial V2.unity

Purpose:

- Final interactive tutorial scene embedded inside a level-like UI.

Observed key objects from the current YAML inventory:

- `TutorialManager`
- `HintButton`
- `RobotTextManager`
- `GamificationManager`
- `LevelProgressBarBinder`
- `Level_1_Manager`
- `TOP_Search`
- `Chat`
- `Robot`
- `small Robot`
- `assistant tutorial`
- `search tutorial`
- `hint button tutorial`
- `restart button tutorial`
- `select level Tutorial tutorial (1)`
- `MIssion button tutorial`
- `Progress bar tutorial`
- `gamification button tutorial`
- `Exit`
- `Restart`

How it works:

- This scene intentionally mirrors the level layout so the tutorial teaches in the real context instead of in a disconnected slide scene.
- `TutorialSequenceManager` is attached to `TutorialManager` and advances through tutorial helper objects when the player presses any key or clicks the mouse.
- Each tutorial step can independently force the vertical progress bar to be visible using `GamificationManager.SetProgressBarVisible(...)`.
- `TutorialSequenceManager` now uses the new Input System, not the old `UnityEngine.Input` path.
- The manager can optionally load another scene when the tutorial sequence completes.
- The scene also contains the same robot/hint/progress-bar patterns used in the gameplay scenes, which makes the tutorial pedagogically consistent with the rest of the project.

Why this scene is important:

- `Tutorial V2` is the best expression of the project's final design direction: teaching the user inside the actual interface they will later use.

## 4.4 Select Level.unity

Purpose:

- Main hub for level entry, user identity capture, lock/unlock logic, and achievements.

Observed key objects from the current inventory:

- `Manager`
- `Level 1`
- `Level 3`
- `Level 4`
- Tutorial/end buttons
- achievement entries and achievement controls
- input field / placeholder / submit UI
- status and score text objects

Verified custom script attachments in this scene:

- `AchievementManager`
- `TitleScreenLevelLockManager`

How it works:

- `UserID` handles participant ID capture and runtime/Firebase syncing.
- `AchievementManager` fetches saved attempt data and decides which achievement objects should show and what stats text should be displayed.
- `TitleScreenLevelLockManager` uses saved progress to enable or disable level buttons and swap lock/unlock visuals.
- `ToggleObjectButton` can be used here to open and close extra UI panels, such as achievements.

Why this scene exists:

- It is the main meta-progression hub. It connects identity, persistence, achievements, and scene navigation in one place.
## 4.5 level 1 Assistant.unity

Purpose:

- Interstitial assistant/narration scene before or after Level 1 content.

Verified attachment:

- `BetweenLevels`

Observed structure:

- Main Camera
- Canvas
- `Manager`
- Button
- Text objects
- EventSystem

How it works:

- `BetweenLevels` types out a configured narration/instruction message with optional typing sound.
- The scene behaves like a teaching or transition beat between gameplay scenes.

Why this scene exists:

- It gives the player narrative context and pacing between interactive tasks.

## 4.6 level 2 Assistant.unity

Purpose:

- Interstitial scene for the second mission, now enhanced with unlock visual support.

Verified attachments/usages:

- `BetweenLevels`
- `lock manager.prefab` usage

How it works:

- In addition to the typed narration flow, this scene can run `LevelUnlock` through the `lock manager` prefab.
- That means the earlier report's statement that unlock visuals were missing is no longer accurate for the current codebase.

Why this scene exists:

- It links narrative explanation, reward feedback, and the level-to-level unlock sequence.

## 4.7 level 3 Assistant.unity

Purpose:

- Interstitial scene for the third mission.

Verified attachments/usages:

- `BetweenLevels`
- `lock manager.prefab` usage

How it works:

- Same overall interstitial pattern as Level 2 Assistant.
- The scene blends level transition narration with unlock/presentation logic.

Why this scene exists:

- It reinforces pacing and progression between increasingly complex levels.

## 4.8 level 4 Assistant.unity

Purpose:

- Interstitial scene for the fourth mission.

Verified attachments/usages:

- `BetweenLevels`
- `lock manager.prefab` usage

How it works:

- Continues the same between-level pattern of narration plus transition presentation.

Why this scene exists:

- It makes the level flow feel curated rather than abrupt.

## 4.9 level 1.unity

Purpose:

- First full playable mission scene.

Observed key objects:

- `Level_1_Manager`
- `TOP_Search`
- `HintButton`
- `RobotTextManager`
- `GamificationManager`
- `LevelProgressBarBinder`
- `MissionButton` prefab usage
- `Robot`
- `small Robot` / `small robot text`
- `Chat`
- `Exit`
- `Restart`
- `gamification button`

How it works:

- The player performs browser/UI actions which are recorded as action IDs through `GameManager`.
- `LevelObjectiveTracker` compares those actions against configured level steps.
- Correct actions can update progress and optionally load a new `ScreenState`.
- Wrong actions increase mistakes and can trigger corrective robot messaging.
- `HintButton` pulls the current step's hint from `LevelObjectiveTracker` and displays it in the shared hint box.
- `MissionButton` displays the mission text in that same hint box, which keeps mission and hint communication in one place.
- `LevelProgressBarBinder` reflects objective progress into the visual progress bar.

Why this scene is important:

- It establishes the full gameplay loop used in the rest of the levels.

## 4.10 level2.unity

Purpose:

- Second playable mission scene, centered around state-driven downloads/OS-specific choices.

Observed key objects:

- `Level_2_Manager`
- `TOP_Search`
- `State Button Manager`
- `WindowsDrivers`
- `Utility`
- `Help`
- `QuickDownloads`
- `HintButton`
- `MissionButton` prefab usage
- `RobotTextManager`
- `GamificationManager`
- `LevelProgressBarBinder`
- `You win Without AI`
- `Exit`
- `Restart`

How it works:

- This scene relies heavily on explicit state changes and button visibility to guide the player through the correct flow.
- The browser background and UI variants are controlled through `ScreenController`, `SearchUIManager`, `ButtonUIManager`, and `StateButtonManager`.
- The level objective logic still comes from `LevelObjectiveTracker`, but the visible page progression is more explicit than in Level 1.

Why this scene exists:

- It introduces the user to a more constrained task flow where page state and chosen OS matter.

## 4.11 level3.unity

Purpose:

- Third playable mission scene, centered around booking/search style interactions.

Observed key objects:

- `Level_3_Manager`
- `TOP_Search`
- `State Button Manager`
- `Booking button`
- `HintButton`
- `MissionButton` prefab usage
- `RobotTextManager`
- `GamificationManager`
- `LevelProgressBarBinder`
- `You win Without AI`
- `Exit`
- `Restart`

How it works:

- Player search and action IDs drive a state chain from start page to booking flow to booked result.
- The same goal-checking loop is reused, but the themed content and visible page states are different.

Why this scene exists:

- It proves that the same engine can support a different digital task without rewriting the core architecture.

## 4.12 level4.unity

Purpose:

- Fourth playable mission scene, built around a multi-page form/CV submission flow.

Observed key objects from the earlier and current inventories:

- `Level_4_Manager`
- `TOP_Search`
- `State Button Manager`
- page transition buttons between pages 1-4
- CV/submission state objects
- `HintButton`
- `MissionButton` prefab usage
- `RobotTextManager`
- `GamificationManager`
- `LevelProgressBarBinder`
- `Exit`
- `Restart`

How it works:

- This is the most sequential of the levels.
- Completing steps can load the next page state, and final success triggers the win pipeline.
- The current final version also supports richer helper feedback through `LevelObjectiveTracker`, including helper objects, step-complete effects, and a separate win-fireworks effect.

Why this scene exists:

- It is the strongest example of the project's gamified, guided workflow design for a multi-step digital task.

## 4.13 ENDING.unity

Purpose:

- Closing scene for the experience.

Verified attachment:

- `BetweenLevels`

How it works:

- Uses the same interstitial/typewriter pattern as the assistant scenes.
- Provides a clean narrative close to the experience.

Why this scene exists:

- It gives the project a formal finish instead of stopping abruptly after the final task.

## 4.14 Foundation sandbox.unity

Purpose:

- Sandbox / foundation scene for browser-system testing and configuration.

Observed characteristics:

- This is the only scene currently enabled in Build Settings.
- It references `Sandbox.prefab` and the browser-state system heavily.

How it works:

- This scene functions as a development and validation environment for the browser simulation layer.
- It is useful for testing `ScreenController`, search routing, result states, and assistant integration outside the full level flow.

Why this scene exists:

- It provides a stable development base for the reusable browser mechanics that the level scenes build on.

## 5) Prefab, Object, and Hierarchy Report

## 5.1 Recurring Scene Hierarchy Pattern

Across the gameplay scenes, the hierarchy is consistently organized around the following object roles:

- Camera layer: usually `Main Camera`
- Browser shell layer: browser background image, `TOP_Search`, results UI, tab UI
- Assistant/chat layer: `Chat`, assistant panel, assistant input/output text
- Guidance layer: `Robot`, `small Robot`, `RobotTextManager`, hint text area, small robot text
- Objective/manager layer: `Level_X_Manager`, `GamificationManager`, `LevelProgressBarBinder`, `LevelStatsManager`
- Navigation layer: `HintButton`, `MissionButton`, `Restart`, `Exit`, gamification toggle button
- Feedback layer: guide squares, helper animations, fireworks / win effects

This repeated pattern is one of the strengths of the project. It means each level can change the task content without changing the player-facing structure too much.

## 5.2 Raw Object Reporting Strategy

Because every scene contains many child `Image`, `Text`, and TMP entries, listing every raw YAML object in prose would make this document unreadable. The project already solves that with two support files:

- `scene_prefab_names.txt`: raw object-name extraction
- `scene_prefab_scripts.txt`: raw script/guid extraction

Those files remain the authoritative source for complete object-name coverage. This report explains the gameplay-relevant objects and how they function.

## 5.3 Custom Prefab Roles

`Sandbox.prefab`
- What it is: the reusable browser shell.
- What it does: provides the structural UI container used by sandbox/level scenes.
- Why it exists: avoids rebuilding the browser frame from scratch scene by scene.

`assistant.prefab`
- What it is: reusable assistant sidebar/chat UI.
- What it does: centralizes the assistant-panel structure.
- Why it exists: keeps assistant UI consistent across scenes.

`HotspotButtonPrefab.prefab`
- What it is: reusable clickable hotspot.
- What it does: hosts `HotspotButton` behavior for generic action areas.
- Why it exists: standardizes browser-like click zones.

`MissionButton.prefab`
- What it is: reusable mission button prefab.
- What it does: shows mission text in the same shared hint box as hints.
- Why it exists: gives the player a deliberate mission reminder without building a second text system.

`lock manager.prefab`
- What it is: unlock-visual wrapper prefab.
- What it does: hosts `LevelUnlock` logic, animation, sprite swapping, audio, and optional handoff to `BetweenLevels` text.
- Why it exists: turns level unlocking into a presentational event instead of a silent data change.

`FireWork.prefab` and `FireWorkWin.prefab`
- What they are: reusable completion-effect prefabs.
- What they do: support step-complete and win-complete presentation layers.
- Why they exist: separate celebratory feedback from core progression logic.

`Background animation.prefab`, `Robot finder Animation .prefab`, `Step1 Animation.prefab`
- What they are: helper/polish prefabs.
- What they do: support attention direction, tutorial guidance, and visual reinforcement.
- Why they exist: make the project feel more game-like and less like a flat UI mockup.

## 6) Script-by-Script Technical Catalog

## 6.1 Core Scripts

`Scripts/Core/GameManager.cs`
- What: global singleton action bus.
- How: `RecordAction` normalizes and emits action strings through `OnActionRecorded`.
- Why: decouples input sources from level logic and telemetry.

`Scripts/Core/ScreenController.cs`
- What: central browser-state controller.
- How: loads `ScreenState`, applies background/result sprites, toggles assistant, manages simple history.
- Why: keeps the fake browser visual state centralized.

`Scripts/Core/SearchRouter.cs`
- What: free-text search matcher for browser search.
- How: exact match, contains match, then token-level fuzzy match with priority tiebreak.
- Why: lets the player type queries instead of only clicking buttons.

`Scripts/Core/HotspotButton.cs`
- What: reusable UI hotspot action component.
- How: button click can toggle assistant, go back, or load a target state, then record an action ID.
- Why: standardizes browser-like click interactions.

`Scripts/Core/HintButton.cs`
- What: current-step hint trigger.
- How: asks `LevelObjectiveTracker` for the current hint, writes it through `RobotTextManager`, counts hint usage, and records telemetry.
- Why: controlled assistance with scoring consequences.

`Scripts/Core/MissionButton.cs`
- What: mission-text trigger.
- How: writes configured mission text into the same hint text box used by `HintButton`, then records an action ID.
- Why: mission reminders and hints share one communication surface, which simplifies UX.

`Scripts/Core/RestartGame.cs`
- What: restart/scene reload helper.
- How: records restart telemetry and reloads the configured/current scene.
- Why: standard restart flow across scenes.

`Scripts/Core/GamificationManager.cs`
- What: progress-bar visibility manager.
- How: toggles or explicitly sets progress-bar visibility, inversely shows/hides the robot, raises `OnProgressBarVisibilityChanged`, and records telemetry.
- Why: gives both UI buttons and tutorial logic a single source of truth for gamification UI visibility.

`Scripts/Core/TabManager.cs`
- What: simulated browser-tab state manager.
- How: stores a `TabSnapshot` per tab with current state, assistant state, and active results.
- Why: supports the browser metaphor beyond a single page.

`Scripts/Core/TabButton.cs`
- What: UI bridge for tab switching.
- How: switches tabs through `TabManager` and can swap tab button visuals.
- Why: connects the tab system to clickable UI.

`Scripts/Core/TabSnapshot.cs`
- What: serializable tab-state container.
- How: stores state ID, current `ScreenState`, assistant-open flag, and active `SearchResultSet`.
- Why: simple persistence model for simulated tabs.

`Scripts/Core/FirebaseManager.cs`
- What: Unity-to-JavaScript Firebase bridge.
- How: wraps WebGL save/get/attempt bridge calls and parses JS responses back into Unity.
- Why: keeps backend communication abstracted away from gameplay code.

`Scripts/Core/FirebaseTelemetryManager.cs`
- What: runtime attempt/session telemetry collector.
- How: subscribes to `GameManager`, opens an attempt per scene, tracks hints/mistakes/action stream, and submits completed or abandoned attempts.
- Why: supports analytics, replay analysis, and achievement/lock systems.

`Scripts/Core/GameSessionData.cs`
- What: participant ID runtime singleton.
- How: stores and exposes the current participant ID for other systems.
- Why: avoids passing identity manually scene to scene.

`Scripts/Core/ActionEvent.cs`
- What: placeholder / legacy script.
- How: currently unused.
- Why: likely an earlier scaffold that can now be removed or repurposed.

`Scripts/Core/StateRegistry.cs`
- What: placeholder / legacy script.
- How: currently unused.
- Why: probably intended for a more formal state system that was never finished.

`Scripts/Core/StateTransition.cs`
- What: placeholder / legacy script.
- How: currently unused.
- Why: reserved for a more formalized transition abstraction.

## 6.2 Data Scripts

`Scripts/Data/ScreenState.cs`
- What: browser visual-state ScriptableObject.
- How: stores state ID, assistant-open/closed sprites, search layout choice, and results-tab type.
- Why: makes page visuals data-driven.

`Scripts/Data/SearchIntentRegistry.cs`
- What: search intent ScriptableObject.
- How: stores keyword rules, priority, matching options, target state, and result-set mapping.
- Why: lets designers author search behavior without code edits.

`Scripts/Data/SearchResultSet.cs`
- What: result-tab sprite bundle.
- How: stores assistant/links/images sprites for assistant-open and assistant-closed conditions.
- Why: keeps result views swappable and structured.

`Scripts/Data/HotspotSpec.cs`
- What: hotspot metadata structure.
- How: stores hotspot ID, action ID, and a normalized rectangle.
- Why: supports authorable click-zone data.

`Scripts/Data/HotspotData.cs`
- What: placeholder data script.
- How: currently unused.
- Why: likely intended for a fuller hotspot dataset.

`Scripts/Data/LevelDefinition.cs`
- What: placeholder data script.
- How: currently unused.
- Why: reserved for a more formalized level-data model.

`Scripts/Data/LevelHint.cs`
- What: placeholder data script.
- How: currently unused.
- Why: placeholder for an unused hint-data slot in the current project structure.

`Scripts/Data/LevelStep.cs`
- What: placeholder data script.
- How: currently unused.
- Why: reserved for a more formalized step schema.

## 6.3 Level Scripts

`Scripts/Levels/Level01/LevelObjectiveTracker.cs`
- What: main mission/progression engine for the level scenes.
- How:
  - stores ordered step definitions with action IDs, hint text, success text, and optional state change.
  - supports strict step ordering through `requiresPreviousStep`.
  - shows a welcome message first, then unlocks message replacement after player action or a short intro hold time.
  - tracks inactivity and can show an idle hint prompt.
  - supports per-step helper objects that appear after configurable delay and auto-hide after configurable visibility time.
  - supports the separate `Hint Button Request Animation`, driven by time since the last completed milestone.
  - hides that helper on milestone completion, progress-bar click telemetry, or timeout.
  - triggers robot jumping for idle prompts and certain helper events.
  - plays step-complete effects and a separate win-fireworks effect.
  - supports delayed win sound and rising win-fireworks movement.
  - handles instant-win action IDs and alternative instant-win actions.
  - transitions to the next scene after a configurable delay.
- Why: this is the core gamified guidance and progression system of the project.

`Scripts/Levels/Level01/LevelProgressBarBinder.cs`
- What: bridge between `LevelObjectiveTracker` and the progress-bar package.
- How: subscribes to objective progress and calls `SetProgress(float)` through reflection.
- Why: keeps the tracker independent from the concrete third-party progress-bar type.

`Scripts/Levels/Level01/LevelStatsManager.cs`
- What: scoring/statistics manager.
- How: tracks time, hints, mistakes, computes final score, and exposes completion statistics.
- Why: converts gameplay performance into measurable outcomes.
## 6.4 UI Scripts

`Scripts/UI/RobotTextManager.cs`
- What: robot and hint text presenter.
- How: writes typewriter text into hint and robot boxes, plays mood-based audio, shakes warning text, and plays a jump animation with real vertical motion.
- Why: gives the guidance layer personality and makes feedback feel alive.

`Scripts/UI/SearchUIManager.cs`
- What: search-bar variant toggle manager.
- How: enables exactly one of the normal/results and assistant-open/closed search bars based on the current `ScreenState`.
- Why: keeps the browser shell visually coherent.

`Scripts/UI/ButtonUIManager.cs`
- What: results-page button group toggler.
- How: switches between open-state and closed-state button groups when a results page is active.
- Why: keeps context-specific browser controls manageable.

`Scripts/UI/StateButtonManager.cs`
- What: explicit state-to-button mapping manager.
- How: listens for `ScreenController` state changes and enables only the buttons mapped to the current state.
- Why: deterministic control visibility for level-specific flows.

`Scripts/UI/AskAnythingInputController.cs`
- What: main browser search input pipeline.
- How: reads submitted text, routes it through `SearchRouter`, updates active results/state, and logs telemetry.
- Why: lets typed queries drive navigation.

`Scripts/UI/AskAnythingInputController1.cs`
- What: legacy/duplicate file.
- How: effectively unused.
- Why: likely safe for cleanup if no scene depends on it.

`Scripts/UI/AssistantChatManager.cs`
- What: assistant message display helper.
- How: updates user/AI fields, panel visibility, input reset, and scroll position.
- Why: reusable UI helper for the assistant panel.

`Scripts/UI/AssistantSidebarUI.cs`
- What: primary assistant sidebar controller.
- How: opens/closes the sidebar, accepts prompts, resolves responses through `AssistantPromptRouter`, uses `APR_New` fallback text, records telemetry, and triggers robot jump on no-match.
- Why: provides the in-game assistant experience.

`Scripts/UI/BetweenLevels.cs`
- What: interstitial typewriter narrator.
- How: types a configured message with optional looping typing sound.
- Why: supports transitions, narration, and instructions between major scenes.

`Scripts/UI/AchievementManager.cs`
- What: achievement UI and best-attempt summarizer.
- How: fetches Firebase data, finds best completed attempts per level, and updates achievement objects/text.
- Why: motivates replay and performance improvement.

`Scripts/UI/UserID.cs`
- What: participant-ID input/save/upload controller.
- How: validates user input, saves locally, syncs runtime state, optionally uploads through Firebase, and updates status text.
- Why: ties progress and telemetry to a participant.

`Scripts/UI/ToggleObjectButton.cs`
- What: generic show/hide button.
- How: toggles a target object's active state and can optionally refresh achievements.
- Why: reusable utility for hub UI panels.

`Scripts/UI/ToggleProgressBarButton.cs`
- What: button bridge into `GamificationManager`.
- How: calls `ToggleProgressBar()`.
- Why: exposes the gamification toggle to UI buttons.

`Scripts/UI/TitleScreenLevelLockManager.cs`
- What: level-button lock manager for the hub scene.
- How: requests participant progress, counts cleared/completed levels, updates button interactability, sprites, overlays, and status text.
- Why: turns backend progress into visible progression gating.

`Scripts/UI/LevelUnlock.cs`
- What: animated unlock-sequence controller.
- How: handles start/completed sprites, animator or legacy animation playback, audio, timing, and optional handoff to `BetweenLevels` narration.
- Why: gives level unlocking a presentational payoff.

`Scripts/UI/TutorialSequenceManager.cs`
- What: step-by-step tutorial-object sequencer for `Tutorial V2`.
- How: hides all tutorial helper objects, shows one at a time, advances on any key/mouse click using the new Input System, can force progress-bar visibility per step, and can load a scene on completion.
- Why: turns the tutorial into a guided, in-context interaction rather than a static slide.

## 6.5 Assistant Subfolder Scripts

`Scripts/UI/Assistant/AssistantChatInputController.cs`
- What: alternate assistant-input pipeline.
- How: resolves prompts through the same registry/router approach, uses the registry fallback response, records telemetry, and triggers robot jump on no-match.
- Why: legacy or alternate assistant entry point.

`Scripts/UI/Assistant/AssistantPromptInputController.cs`
- What: placeholder / unused script.
- How: currently empty.
- Why: legacy scaffold.

`Scripts/UI/Assistant/AssistantPromptRegistry.cs`
- What: assistant-response database ScriptableObject.
- How: stores prompt entries plus a global `noMatchResponseText` fallback.
- Why: makes assistant behavior authorable without code edits.

`Scripts/UI/Assistant/AssistantPromptRouter.cs`
- What: assistant prompt matcher.
- How:
  - tokenizes and normalizes input.
  - single-word keywords match only when that exact word appears in the prompt.
  - multi-word phrases can use exact phrase contains or phrase-level fuzzy matching with `maxEditDistance`.
  - higher priority wins when multiple entries match.
- Why: reduces false positives while keeping phrase matching flexible.

## 6.6 Third-Party Progress Bar Scripts Used by Gameplay

`Art/InfinityPBR - Magic Pig Games/Progress Bar/Scripts/ProgressBar.cs`
- What: base progress-bar component.
- How: exposes `SetProgress(0..1)` and handles bar rendering/animation.
- Why: visual progress display.

`HorizontalProgressBar.cs`
- What: horizontal specialization.
- Why: semantic clarity for horizontal bars.

`VerticalProgressBar.cs`
- What: vertical specialization.
- Why: vertical progress bar used by the game's gamification UI.

`ProgressBarInspectorTest.cs`
- What: inspector/test helper.
- Why: package-side debugging utility.

`ScrollingUVs.cs`
- What: texture/UV scrolling effect helper.
- Why: visual polish inside the package.

## 7) Runtime Architecture and Event Flow

Core event chain:

- UI interaction happens.
- A script records an action string through `GameManager.RecordAction(...)`.
- `GameManager.OnActionRecorded` notifies subscribed systems.
- `LevelObjectiveTracker` decides whether the action advances the mission, counts as a mistake, resets helper timers, or ends the level.
- `FirebaseTelemetryManager` records the same action into the attempt stream.

Visual state chain:

- Search input -> `SearchRouter` -> matching `Intent` -> `ScreenController.LoadState(...)`
- `ScreenController` updates the browser background and then tells:
  - `SearchUIManager` which search bar variant to show
  - `ButtonUIManager` which results-button group to show
  - `StateButtonManager` which explicit scene buttons to show

Progress chain:

- `LevelObjectiveTracker` advances current step index
- `LevelProgressBarBinder` reflects normalized progress into the vertical progress bar
- `LevelStatsManager` computes penalties and score at completion
- `FirebaseTelemetryManager` submits completion/abandon payloads
- `AchievementManager` and `TitleScreenLevelLockManager` consume those results later in the hub

Robot/help chain:

- `HintButton` and `MissionButton` both write into the same hint box through `RobotTextManager`
- `RobotTextManager` types text, plays sounds, and can trigger a physical jump arc
- `LevelObjectiveTracker` can also show stuck-step helper objects, milestone-idle helper objects, step effects, and win fireworks

Tutorial chain:

- `TutorialSequenceManager` shows tutorial objects one at a time
- per-step checkboxes can force progress-bar visibility through `GamificationManager`
- once the last tutorial step is completed, the manager can load the next scene

## 8) Gamification and Guidance Systems

Current gamified systems now present in the codebase:

- Ordered mission steps with optional strict sequencing.
- Per-step hint text and per-step success text.
- Mistake counting and hint penalties through `LevelStatsManager`.
- Idle reminder messaging from the robot.
- Intro-message gating so the welcome line appears first before other robot messages replace it.
- Per-step helper objects that appear after configurable delay and auto-hide after configurable visibility duration.
- Separate milestone-idle helper object (`Hint Button Request Animation`) tied to time since the last completed milestone.
- Robot jumping for idle assistance and assistant no-match feedback.
- Shared mission/hint text surface via `MissionButton` + `HintButton`.
- Progress bar visibility toggling and per-step tutorial progress-bar forcing.
- Step-complete VFX and a separate win-fireworks effect with delayed sound and upward movement.
- Achievement unlocking based on best completed attempts.
- Level-button locks and unlock visuals in the hub/interstitial flow.

This means the project has moved beyond simple objective checking. It now has layered instructional feedback, motivational feedback, and reward feedback.

## 9) What Changed Since the Previous Report

The earlier report was useful, but it no longer matched the current final project in several areas. The most important updates are:

- `Tutorial V2` now exists and is a real interactive tutorial scene.
- `TutorialSequenceManager` was added to drive tutorial helper objects step by step.
- `MissionButton` was added and now shares the same hint text box as `HintButton`.
- `GamificationManager` now supports explicit `SetProgressBarVisible(...)`, not just toggle.
- `RobotTextManager` now supports jump animation plus actual up/down motion.
- `LevelObjectiveTracker` grew substantially and now includes:
  - intro gating
  - stuck-step helper objects
  - hint-button request animation logic
  - step-complete effect logic
  - separate win-fireworks effect logic
  - delayed win sound
  - rising fireworks motion
- Assistant no-match behavior now lives in `APR_New` fallback text and can trigger robot jumping.
- `AssistantPromptRouter` was tightened so exact single-word matches and phrase fuzzy matches behave more predictably.
- The old report said lock/unlock visuals were missing. That is no longer true because the current project contains `LevelUnlock` and `lock manager.prefab` usage.

## 10) Risks, Gaps, and Technical Debt

High-impact issues to keep in mind:

- Build Settings currently include only `Foundation sandbox.unity`, while the full game flow is spread across many other scenes.
- Free-form string action IDs are still powerful but fragile; typos can silently break progression.
- There are still multiple placeholder/legacy scripts in the repository.
- There are still two assistant input pipelines (`AssistantSidebarUI` and `AssistantChatInputController`) that can drift over time.
- The project depends heavily on inspector wiring across many scenes.

Medium-impact issues:

- Scene naming is slightly inconsistent (`level 1.unity` versus `level2.unity`, `level3.unity`, `level4.unity`).
- `LevelProgressBarBinder` uses reflection, which is flexible but brittle if third-party API names change.
- Large scene hierarchies with many active/inactive objects can become difficult to maintain without stricter conventions.
- Some serialized fields remain as legacy UI text fallbacks even when the registry-driven fallback is now the real source of truth.

Cleanup candidates:

- `ActionEvent.cs`
- `StateRegistry.cs`
- `StateTransition.cs`
- `HotspotData.cs`
- `LevelDefinition.cs`
- `LevelHint.cs`
- `LevelStep.cs`
- `AskAnythingInputController1.cs`
- `AssistantPromptInputController.cs`

## 11) Methodology-Ready Summary

If you need a concise methodology paragraph for academic writing, this is the current truthful description of the project:

- The project uses a data-driven UI-state approach for browser simulation, where page visuals are authored as ScriptableObjects and swapped at runtime.
- Player behavior is captured through an event-driven action bus (`GameManager`), allowing progression, telemetry, and UI feedback systems to react independently to the same interaction stream.
- Learning support is layered through guided tutorials, context-specific hints, mission reminders, idle prompts, animated helper objects, robot feedback, and reward effects.
- Performance is assessed through time, mistakes, and hint usage, then persisted through a Firebase/WebGL bridge to support replay analysis, achievements, and level unlocking.
- The overall design combines pedagogical scaffolding with gamification so that the player is not only told what to do, but is gradually guided, corrected, rewarded, and assessed within the same simulated interface.

## 12) Raw Inventory Files Used For This Report

The following files in `Assets/_Project` remain important support files for report maintenance and later audits:

- `scene_prefab_names.txt`
- `scene_prefab_scripts.txt`

They are especially useful when you need:

- exact object-name audits
- scene/prefab hierarchy verification
- attachment verification against YAML
- methodology appendices or technical appendices

## 13) Closing

The current final version of the project is stronger and more feature-complete than the earlier report described. The biggest strengths of the final codebase are:

- a reusable browser simulation architecture
- a clear mission/progression pipeline
- strong guided-feedback layering through robot, hints, missions, tutorial objects, and helper animations
- meaningful progression systems through score, achievements, locks, and unlocks
- a growing separation between authored content (states/prompts/assets) and runtime systems

For a methodology chapter or project handover, this updated report is now much closer to the truth of the final repository than the previous version.
