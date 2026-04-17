# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**IcePEAK** — a first-person VR ice-climbing game (Quest 3 target), inspired by PEAK but with ice-pick mechanics. The player swings two VR controllers as ice picks to embed in ice surfaces, grips to hold, and pulls to climb. Ice surfaces crack and shatter; rock surfaces are solid platforms.

- **Engine:** Unity `6000.3.13f1` (Unity 6.3 LTS) — open the project in the matching editor version.
- **Render pipeline:** URP 17.3 (`Assets/Settings/Project Configuration/`).
- **XR stack:** XR Interaction Toolkit 3.3.1 + OpenXR 1.16 + Meta XR SDK 85 + Unity AndroidXR OpenXR 1.1 + XR Hands 1.7.
- **Input:** new Input System 1.19 via `InputActionReference` (XRI default action maps live under `Assets/XRI/`).
- **Deploy target:** Android (Quest 3). Use Unity's Build Profiles for Android; there is no CLI build/test script.

## Repository layout

- `Assets/IcePEAK/` — all project-authored gameplay content. **Keep new game code here**; do not mix it into `VRTemplateAssets/`, `Samples/`, `XRI/`, `Oculus/`, or `Fab_*/` (those are upstream template/plugin assets).
  - `Scripts/IcePick/` — pick behavior (`IcePickController`, `SwingDetector`, `ClimbingLocomotion`).
  - `Scripts/Surfaces/` — `SurfaceType` enum + `SurfaceTag` MonoBehaviour placed on every climbable collider.
  - `Prefabs/IcePick.prefab` — the pick prefab (instantiated twice, one per hand).
  - `Scenes/TestScene.unity` — primary gameplay test scene.
- `Assets/Scenes/` — template scenes (`SampleScene`, `BasicScene`) from the VR template; not the active gameplay scenes.
- `doc/` — design/high-concept PDFs and the authoritative implementation plan at **`doc/Plans/icepicks.md`** (read this before touching pick/climb code — it documents intended architecture, tunables, event contracts, and integration points for unbuilt systems).
- `ProjectSettings/`, `Packages/manifest.json` — Unity config; edit cautiously and commit together.
- `Library/`, `Temp/`, `obj/`, `Logs/`, `UserSettings/` — generated; never commit.

## Core gameplay architecture

The pick system is **event-driven** and designed so ice-destruction, IK, checkpoint, and gadget systems (not yet built) plug in without modifying the pick core. Understand these contracts before changing anything:

- **`SwingDetector`** (on each pick's `TipCollider` child) samples tip position over a frame window and exposes `CurrentSpeed` / `IsSwingFastEnough`. Tracks the **tip**, not the controller, because the tip's lever-arm velocity is what gameplay keys off.
- **`IcePickController`** (on pick root, requires `Rigidbody` kinematic, trigger collider on tip) listens for `OnTriggerEnter` on the tip, reads `SurfaceTag.Type`, and either `Embed()` or `Bounce()`. On embed it **reparents the pick to world (null parent)** so it stays fixed while the controller keeps moving; on release it reparents back to the cached `_controllerParent` at the cached local transform. Exposes public events `OnEmbedded(pick, surface)` and `OnReleased(pick)` — these are the integration seams for future systems (ice destruction subscribes to start crack timers; IK reads `IsEmbedded` / `EmbedWorldPosition`).
- **`ClimbingLocomotion`** (on XR Origin) subscribes to both picks' embed events. While any pick is embedded, each frame it computes `prevControllerPos - currentControllerPos` and applies that delta to the XR Origin — moving the rig opposite to the hand motion (pull down → rig goes up). When both hands are embedded it **averages** the two deltas. After moving the origin it re-samples controller positions so the delta is strictly per-frame (prevents oscillation feedback). Also toggles the assigned `locomotionProviders[]` (move/turn/teleport components) off while climbing off-ground and back on when grounded.
- **Release input:** currently the **trigger** (activate) action — pressing trigger above threshold calls `Release()`. The plan doc describes grip-based hold; the shipped code inverted this to trigger-to-release. If the design changes, update `IcePickController.Update()` and the `triggerAction` field.
- **Surface identification:** every climbable collider needs a `SurfaceTag` component (on self or a parent — the controller calls `GetComponentInParent<SurfaceTag>`). Ice-tagged colliders can embed; rock bounces. Physics layers (`IcePickTip`, `ClimbableSurface`, `RockPlatform`) are used to keep triggers from firing on irrelevant geometry — preserve these layer assignments when building prefabs.
- **Double-embed / shatter-while-embedded** are guarded by the `_isEmbedded` flag and by the rule that the destruction system must call `IcePickController.Release()` before destroying a surface the pick is in. Honor this contract in any new destruction code.

## Working with the project

- **Run / debug:** open in the Unity editor and press Play in `Assets/IcePEAK/Scenes/TestScene.unity`. There is no headless build/test runner configured; UI and XR behavior must be verified in-editor (XR Simulation is available via `Assets/XR/Resources/XRSimulationRuntimeSettings.asset`) or on-device. Report "can't test UI" explicitly rather than claiming success after a code-only change.
- **Build:** Android via Unity's Build Profiles; no Bash/CLI build script.
- **No linter / test suite** is wired up — Unity's in-editor compile is the only check. The C# files have no `.asmdef` so they compile into `Assembly-CSharp` by default.
- **Inspector wiring matters:** most behavior requires `[SerializeField]` references to be set on the prefab/scene object. Editing a script in isolation is rarely sufficient; scene/prefab changes are often needed and will show up as `.unity`/`.prefab` diffs.
- **Assets are binary/YAML:** `.unity`, `.prefab`, `.asset`, and `.mat` use Unity's YAML — do not hand-edit. `.meta` files must always be committed alongside their asset.
- **Tunable values** (velocity threshold, embed depth, grip threshold, fall duration, etc.) are all `[SerializeField]` — tune in the Inspector, not in code, unless the default needs to change.

## Reference: where the design lives

- `doc/Plans/icepicks.md` — implementation plan; sections 6 (embed), 8 (climb math), 13 (integration contracts) are the most load-bearing.
- `doc/Design_Doc/IcePEAK_Design_Doc.pdf` — full design doc.
- `doc/High_Concept/IcePEAK_High_Concept.pdf` — pitch-level overview.
- `doc/idea.md` — original brainstorm (includes two abandoned concepts, Getaway and SliceDice — ignore those).
