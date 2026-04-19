# Basic Items Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship three trigger-activatable placeholder gadget prefabs (`GrappleGun`, `ColdSpray`, `Piton`) that slot into the existing gadget belt, with HIC-routed activation so belt swaps never double-fire with item activation.

**Architecture:** Each item is a MonoBehaviour implementing `IHoldable` (transfer log) and a new `IActivatable` interface (placeholder visual). The HIC gains a third priority rung that dispatches to `IActivatable.Activate()` when hand holds an item and no slot is hovered. Items never subscribe to input; all trigger handling stays in the HIC.

**Tech Stack:** Unity 6.3 LTS (6000.3.13f1), URP 17.3, XR Interaction Toolkit 3.3.1 (Input System 1.19), C# 10.

**Project conventions that shape this plan:**
- No test framework. "Passing" means Unity compiles without errors and the play-mode checklist in the spec §6 succeeds.
- Unity generates `.meta` files on editor focus; both `.cs` and `.cs.meta` must land in the same commit.
- `.unity` / `.prefab` / `.mat` are YAML — prefer Unity Editor authoring; hand-edit only when editor access is blocked.
- Commits follow the existing conventional style (`feat(scope): …`, `docs(scope): …`). No AI attribution, no `Co-Authored-By: Claude` trailers.

**Reference:** Design spec lives at `doc/Plans/basic-items.md`. Read §3 (decisions) and §5 (components) before starting.

---

## Task 0: Create branch

**Files:**
- None created/modified; this is a git-only step.

- [ ] **Step 1: Confirm starting state**

```bash
git status --short
git log -1 --oneline
```

Expected: working tree clean (ignore the `M ProjectSettings/ShaderGraphSettings.asset` CRLF noise — do not stage it), current branch `main`, HEAD at `913fdc0 docs(basic-items): clarify Activate() delegates …` or newer.

- [ ] **Step 2: Create and switch to `BasicItems`**

```bash
git checkout -b BasicItems
```

- [ ] **Step 3: Verify branch**

```bash
git branch --show-current
```

Expected output: `BasicItems`.

---

## Task 1: Add `IActivatable` interface

**Files:**
- Create: `Assets/IcePEAK/Scripts/Gadgets/IActivatable.cs`
- Create: `Assets/IcePEAK/Scripts/Gadgets/IActivatable.cs.meta` (or let Unity generate it)

- [ ] **Step 1: Create the interface file**

Write `Assets/IcePEAK/Scripts/Gadgets/IActivatable.cs`:

```csharp
namespace IcePEAK.Gadgets
{
    /// <summary>
    /// Implemented by items that respond to a trigger press while held.
    /// Called by <see cref="HandInteractionController"/> on rung 3 of its
    /// priority ladder (hand holds item, no slot hovered).
    /// </summary>
    public interface IActivatable
    {
        void Activate();
    }
}
```

- [ ] **Step 2: Generate the `.meta` file**

Preferred: switch focus to the Unity Editor window. Unity imports the file and writes `.meta` automatically. Switch back.

Fallback (if Unity Editor is not running): generate a GUID and write the `.meta` manually. Generate a GUID:

```bash
powershell -NoProfile -Command "[guid]::NewGuid().ToString('N')"
```

Then write `Assets/IcePEAK/Scripts/Gadgets/IActivatable.cs.meta` with:

```yaml
fileFormatVersion: 2
guid: <PASTE-GUID-HERE>
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
```

- [ ] **Step 3: Compile check**

In Unity, wait for the import to complete. Open `Window → General → Console`. Expected: no compile errors, no new warnings.

- [ ] **Step 4: Commit**

```bash
git add Assets/IcePEAK/Scripts/Gadgets/IActivatable.cs Assets/IcePEAK/Scripts/Gadgets/IActivatable.cs.meta
git commit -m "feat(items): add IActivatable interface for trigger-activated held items"
```

---

## Task 2: Add `GrappleGun` script

**Files:**
- Create: `Assets/IcePEAK/Scripts/Gadgets/Items/GrappleGun.cs`
- Create: `Assets/IcePEAK/Scripts/Gadgets/Items/GrappleGun.cs.meta`

- [ ] **Step 1: Create the script**

Write `Assets/IcePEAK/Scripts/Gadgets/Items/GrappleGun.cs`:

```csharp
using System.Collections;
using UnityEngine;

namespace IcePEAK.Gadgets.Items
{
    /// <summary>
    /// Placeholder grappling hook. Activate() fires a short LineRenderer streak
    /// from the barrel tip. No gameplay effect yet; the named method <see cref="Fire"/>
    /// mirrors the design-doc vocabulary so future callers can bind to it.
    /// </summary>
    public class GrappleGun : MonoBehaviour, IHoldable, IActivatable
    {
        [Header("Visual refs (wired on the prefab)")]
        [SerializeField] private LineRenderer streak;
        [SerializeField] private Transform barrelTip;

        [Header("Tunables")]
        [SerializeField] private float streakLength = 1f;
        [SerializeField] private float streakDuration = 0.2f;

        private bool _isPlaying;

        public void OnTransfer(CellKind from, CellKind to)
        {
            Debug.Log($"[GrappleGun] {from} -> {to}");
        }

        public void Activate() => Fire();

        public void Fire()
        {
            if (_isPlaying) return;
            if (streak == null || barrelTip == null) return;
            StartCoroutine(PlayStreak());
        }

        private IEnumerator PlayStreak()
        {
            _isPlaying = true;
            streak.positionCount = 2;
            streak.SetPosition(0, barrelTip.position);
            streak.SetPosition(1, barrelTip.position + barrelTip.forward * streakLength);
            streak.enabled = true;
            yield return new WaitForSeconds(streakDuration);
            streak.enabled = false;
            _isPlaying = false;
        }
    }
}
```

- [ ] **Step 2: Generate the `.meta` file**

Same pattern as Task 1 Step 2 (Unity focus OR manual GUID).

- [ ] **Step 3: Compile check**

Unity Console: no errors. `GrappleGun` appears under `Add Component → Scripts/IcePEAK.Gadgets.Items`.

- [ ] **Step 4: Commit**

```bash
git add Assets/IcePEAK/Scripts/Gadgets/Items/GrappleGun.cs Assets/IcePEAK/Scripts/Gadgets/Items/GrappleGun.cs.meta
git commit -m "feat(items): add GrappleGun placeholder with line-renderer streak on Fire"
```

---

## Task 3: Add `ColdSpray` script

**Files:**
- Create: `Assets/IcePEAK/Scripts/Gadgets/Items/ColdSpray.cs`
- Create: `Assets/IcePEAK/Scripts/Gadgets/Items/ColdSpray.cs.meta`

- [ ] **Step 1: Create the script**

Write `Assets/IcePEAK/Scripts/Gadgets/Items/ColdSpray.cs`:

```csharp
using System.Collections;
using UnityEngine;

namespace IcePEAK.Gadgets.Items
{
    /// <summary>
    /// Placeholder cold-spray canister. Activate() plays a short particle burst at
    /// the nozzle. No gameplay effect yet; <see cref="Spray"/> mirrors the design-doc
    /// vocabulary so future crack-timer extension code can bind to it.
    /// </summary>
    public class ColdSpray : MonoBehaviour, IHoldable, IActivatable
    {
        [Header("Visual refs (wired on the prefab)")]
        [SerializeField] private ParticleSystem mist;

        [Header("Tunables")]
        [SerializeField] private float burstSeconds = 0.3f;

        private bool _isPlaying;

        public void OnTransfer(CellKind from, CellKind to)
        {
            Debug.Log($"[ColdSpray] {from} -> {to}");
        }

        public void Activate() => Spray();

        public void Spray()
        {
            if (_isPlaying) return;
            if (mist == null) return;
            StartCoroutine(PlayBurst());
        }

        private IEnumerator PlayBurst()
        {
            _isPlaying = true;
            mist.Play();
            yield return new WaitForSeconds(burstSeconds);
            mist.Stop();
            _isPlaying = false;
        }
    }
}
```

- [ ] **Step 2: Generate the `.meta` file**

Same pattern as Task 1 Step 2.

- [ ] **Step 3: Compile check**

Unity Console: no errors.

- [ ] **Step 4: Commit**

```bash
git add Assets/IcePEAK/Scripts/Gadgets/Items/ColdSpray.cs Assets/IcePEAK/Scripts/Gadgets/Items/ColdSpray.cs.meta
git commit -m "feat(items): add ColdSpray placeholder with particle burst on Spray"
```

---

## Task 4: Add `Piton` script

**Files:**
- Create: `Assets/IcePEAK/Scripts/Gadgets/Items/Piton.cs`
- Create: `Assets/IcePEAK/Scripts/Gadgets/Items/Piton.cs.meta`

- [ ] **Step 1: Create the script**

Write `Assets/IcePEAK/Scripts/Gadgets/Items/Piton.cs`:

```csharp
using System.Collections;
using UnityEngine;

namespace IcePEAK.Gadgets.Items
{
    /// <summary>
    /// Placeholder piton. Activate() pulses the visual forward along its local Z
    /// (as if being driven into rock) then back. No gameplay effect yet;
    /// <see cref="Plant"/> mirrors the design-doc vocabulary.
    /// </summary>
    public class Piton : MonoBehaviour, IHoldable, IActivatable
    {
        [Header("Visual refs (wired on the prefab)")]
        [SerializeField] private Transform visual;

        [Header("Tunables")]
        [SerializeField] private float plantDistance = 0.05f;
        [SerializeField] private float plantDuration = 0.2f;

        private bool _isPlaying;

        public void OnTransfer(CellKind from, CellKind to)
        {
            Debug.Log($"[Piton] {from} -> {to}");
        }

        public void Activate() => Plant();

        public void Plant()
        {
            if (_isPlaying) return;
            if (visual == null) return;
            StartCoroutine(PlayPlant());
        }

        private IEnumerator PlayPlant()
        {
            _isPlaying = true;
            var rest = visual.localPosition;
            var driven = rest + new Vector3(0f, 0f, plantDistance);
            var half = plantDuration * 0.5f;

            // Drive in
            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                visual.localPosition = Vector3.Lerp(rest, driven, t / half);
                yield return null;
            }
            visual.localPosition = driven;

            // Pull back
            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                visual.localPosition = Vector3.Lerp(driven, rest, t / half);
                yield return null;
            }
            visual.localPosition = rest;

            _isPlaying = false;
        }
    }
}
```

- [ ] **Step 2: Generate the `.meta` file**

Same pattern as Task 1 Step 2.

- [ ] **Step 3: Compile check**

Unity Console: no errors.

- [ ] **Step 4: Commit**

```bash
git add Assets/IcePEAK/Scripts/Gadgets/Items/Piton.cs Assets/IcePEAK/Scripts/Gadgets/Items/Piton.cs.meta
git commit -m "feat(items): add Piton placeholder with forward plant pulse"
```

---

## Task 5: Add HIC rung 3 — dispatch to `IActivatable`

**Files:**
- Modify: `Assets/IcePEAK/Scripts/Gadgets/HandInteractionController.cs`

The current `Update()` returns early on `CurrentHoveredSlot == null`, which is exactly the branch where rung 3 needs to fire. Restructure so swap and activate are mutually exclusive branches *after* the trigger-press check.

- [ ] **Step 1: Update the class-level summary comment**

Locate lines 6–11 in `HandInteractionController.cs`:

```csharp
    /// <summary>
    /// Per-hand belt interaction controller. Priority order each frame:
    ///   1. Pick embedded → pick owns trigger (we early-return, pick handles release).
    ///   2. Trigger rising-edge + hand over a slot → swap/stow/draw.
    ///   3. Otherwise no-op.
    /// </summary>
```

Replace with:

```csharp
    /// <summary>
    /// Per-hand belt interaction controller. Priority order each frame:
    ///   1. Pick embedded → pick owns trigger (we early-return, pick handles release).
    ///   2. Trigger rising-edge + hand over a slot → swap/stow/draw.
    ///   3. Trigger rising-edge + held item implements IActivatable → Activate().
    ///   4. Otherwise no-op.
    /// </summary>
```

- [ ] **Step 2: Restructure the trigger-handling block**

In the same file, locate the block from `// P3: nothing hovered → no-op.` (line 50) through `ResolveBeltAction(CurrentHoveredSlot);` (line 54):

```csharp
            // P3: nothing hovered → no-op.
            if (CurrentHoveredSlot == null) return;

            // P2: swap/stow/draw.
            ResolveBeltAction(CurrentHoveredSlot);
```

Replace with:

```csharp
            // P2: hovered slot → swap/stow/draw.
            if (CurrentHoveredSlot != null)
            {
                ResolveBeltAction(CurrentHoveredSlot);
                return;
            }

            // P3: held item implements IActivatable → Activate().
            if (handCell.HeldItem != null &&
                handCell.HeldItem.TryGetComponent<IActivatable>(out var activatable))
            {
                Debug.Log($"[{name}] Activate -> {handCell.HeldItem.name}");
                activatable.Activate();
            }
            // P4: otherwise no-op.
```

- [ ] **Step 3: Compile check**

Unity Console: no errors.

- [ ] **Step 4: Commit**

```bash
git add Assets/IcePEAK/Scripts/Gadgets/HandInteractionController.cs
git commit -m "feat(items): add HIC rung 3 — dispatch trigger to IActivatable held item"
```

---

## Task 6: Create `Item_Placeholder` material

**Files:**
- Create: `Assets/IcePEAK/Prefabs/Items/Materials/Item_Placeholder.mat`
- Create: `Assets/IcePEAK/Prefabs/Items/Materials/Item_Placeholder.mat.meta`

- [ ] **Step 1: Create the folder tree**

In Unity's Project window, right-click `Assets/IcePEAK/Prefabs` → `Create → Folder` → name it `Items`. Inside `Items`, create another folder `Materials`.

- [ ] **Step 2: Create the material**

Right-click `Assets/IcePEAK/Prefabs/Items/Materials` → `Create → Material` → name it `Item_Placeholder`.

- [ ] **Step 3: Configure the material**

Select `Item_Placeholder`. In the Inspector:
- Shader: `Universal Render Pipeline/Lit` (should be the default).
- Surface Options → Surface Type: `Opaque`.
- Surface Inputs → Base Map color: light neutral grey. Click the color swatch, set RGB to `(180, 180, 180)` (hex `B4B4B4`), alpha `255`.
- Leave Metallic, Smoothness at defaults.

- [ ] **Step 4: Verify on disk**

```bash
ls Assets/IcePEAK/Prefabs/Items/Materials/
```

Expected: `Item_Placeholder.mat` and `Item_Placeholder.mat.meta`.

- [ ] **Step 5: Commit**

```bash
git add Assets/IcePEAK/Prefabs/Items/Materials
git commit -m "feat(items): add neutral-grey placeholder material for item prefabs"
```

---

## Task 7: Create `Item_GrappleGun.prefab`

**Files:**
- Create: `Assets/IcePEAK/Prefabs/Items/Item_GrappleGun.prefab`
- Create: `Assets/IcePEAK/Prefabs/Items/Item_GrappleGun.prefab.meta`

Build the prefab in a temporary scene, then drag it into the project. All primitive meshes use `Item_Placeholder` material.

- [ ] **Step 1: Build in scene**

Open any scratch scene (or `TestScene`; you will not save it). In the Hierarchy:

1. `Create → Create Empty` → rename `Item_GrappleGun`. Position `(0, 0, 0)`, rotation `(0, 0, 0)`.
2. Under it, `Create Empty` named `Visual`.
3. Under `Visual`, `3D Object → Cube` named `Grip`. Transform: position `(0, 0, 0)`, rotation `(0, 0, 0)`, scale `(0.03, 0.08, 0.04)`. Remove its `Box Collider` component (right-click → Remove Component).
4. Under `Visual`, `3D Object → Cylinder` named `Barrel`. Transform: position `(0, 0.04, 0.03)` (sits atop-and-forward of the grip), rotation `(90, 0, 0)` (horizontal, pointing +Z), scale `(0.02, 0.05, 0.02)`. Remove its `Capsule Collider` component.
5. Under `Item_GrappleGun` (sibling of `Visual`), `Create Empty` named `BarrelTip`. Position `(0, 0.04, 0.08)` — at the muzzle end of the barrel, forward along +Z.
6. Under `Item_GrappleGun`, `Create Empty` named `Streak`. Position `(0, 0, 0)`. Add component `Line Renderer`. Disable the `Line Renderer` (uncheck its enabled tick). On the Line Renderer: Width = `0.005`, Positions = `0` for now (script sets them at runtime), Material = `Item_Placeholder`, Use World Space = `true`.

- [ ] **Step 2: Assign materials**

Select `Grip` and `Barrel`. Drag `Item_Placeholder` onto their `Mesh Renderer → Materials → Element 0`.

- [ ] **Step 3: Attach and wire the `GrappleGun` script**

Select the root `Item_GrappleGun`. `Add Component → GrappleGun` (from `IcePEAK.Gadgets.Items`). In the Inspector:
- `Streak` → drag the `Streak` child GameObject onto the field.
- `Barrel Tip` → drag the `BarrelTip` child Transform onto the field.
- `Streak Length` → leave at `1`.
- `Streak Duration` → leave at `0.2`.

- [ ] **Step 4: Save as prefab**

Drag the root `Item_GrappleGun` GameObject from the Hierarchy into `Assets/IcePEAK/Prefabs/Items/` in the Project window. Unity creates `Item_GrappleGun.prefab`. Delete the Hierarchy instance (no scene save).

- [ ] **Step 5: Verify on disk**

```bash
ls Assets/IcePEAK/Prefabs/Items/ | grep -i grapple
```

Expected: `Item_GrappleGun.prefab`, `Item_GrappleGun.prefab.meta`.

- [ ] **Step 6: Commit**

```bash
git add Assets/IcePEAK/Prefabs/Items/Item_GrappleGun.prefab Assets/IcePEAK/Prefabs/Items/Item_GrappleGun.prefab.meta
git commit -m "feat(items): add Item_GrappleGun placeholder prefab (L-shape pistol)"
```

---

## Task 8: Create `Item_ColdSpray.prefab`

**Files:**
- Create: `Assets/IcePEAK/Prefabs/Items/Item_ColdSpray.prefab`
- Create: `Assets/IcePEAK/Prefabs/Items/Item_ColdSpray.prefab.meta`

- [ ] **Step 1: Build in scene**

1. `Create Empty` → `Item_ColdSpray`. Transform zeroed.
2. Child `Create Empty` → `Visual`.
3. Under `Visual`, `3D Object → Cylinder` named `Canister`. Position `(0, 0, 0)`, rotation `(0, 0, 0)`, scale `(0.04, 0.06, 0.04)`. Remove its `Capsule Collider`.
4. Under `Visual`, `3D Object → Cylinder` named `Nozzle`. Position `(0, 0.065, 0)` (atop the canister), rotation `(0, 0, 0)`, scale `(0.015, 0.015, 0.015)`. Remove its `Capsule Collider`.
5. Under `Item_ColdSpray` (sibling of `Visual`), `Create Empty` named `Mist`. Position `(0, 0.08, 0.02)` (in front of the nozzle, slightly forward). Add component `Particle System`.

- [ ] **Step 2: Configure the particle system**

Select `Mist`. In the Inspector, on the `Particle System` component:
- Uncheck `Play On Awake`.
- `Duration` = `0.3`.
- `Looping` = `false`.
- `Start Lifetime` = `0.3`.
- `Start Speed` = `0.3`.
- `Start Size` = `0.01`.
- `Start Color` = light blue `(180, 220, 255)`.
- `Max Particles` = `20`.
- `Shape` section: Shape = `Cone`, Angle = `15`, Radius = `0.005`.
- `Renderer` section: Material = any default particle material (e.g., `Default-ParticleSystem`).

- [ ] **Step 3: Assign meshes' material**

Select `Canister` and `Nozzle`. Drag `Item_Placeholder` onto their `Mesh Renderer → Materials → Element 0`.

- [ ] **Step 4: Attach and wire the `ColdSpray` script**

Select root `Item_ColdSpray`. `Add Component → ColdSpray`. Drag the `Mist` child GameObject's `Particle System` onto the `Mist` field (or drag the `Mist` GameObject; Unity will resolve to the component).

Set `Burst Seconds` to `0.3` (default).

- [ ] **Step 5: Save as prefab**

Drag root into `Assets/IcePEAK/Prefabs/Items/`. Delete the Hierarchy instance.

- [ ] **Step 6: Commit**

```bash
git add Assets/IcePEAK/Prefabs/Items/Item_ColdSpray.prefab Assets/IcePEAK/Prefabs/Items/Item_ColdSpray.prefab.meta
git commit -m "feat(items): add Item_ColdSpray placeholder prefab (canister + nozzle)"
```

---

## Task 9: Create `Item_Piton.prefab`

**Files:**
- Create: `Assets/IcePEAK/Prefabs/Items/Item_Piton.prefab`
- Create: `Assets/IcePEAK/Prefabs/Items/Item_Piton.prefab.meta`

- [ ] **Step 1: Build in scene**

1. `Create Empty` → `Item_Piton`. Transform zeroed.
2. Child `Create Empty` → `Visual`.
3. Under `Visual`, `3D Object → Cylinder` named `Shaft`. Position `(0.03, 0, 0)` (offset forward from the eye tab by half-shaft length), rotation `(0, 0, 90)` (horizontal, shaft axis along +X), scale `(0.012, 0.06, 0.012)`. Remove its `Capsule Collider`.
4. Under `Visual`, `3D Object → Cube` named `EyeTab`. Position `(-0.005, 0, 0)` (at the blunt end of the shaft), rotation `(0, 0, 0)`, scale `(0.025, 0.025, 0.008)`. Remove its `Box Collider`.

- [ ] **Step 2: Assign materials**

Select `Shaft` and `EyeTab`. Drag `Item_Placeholder` onto their `Mesh Renderer → Materials → Element 0`.

- [ ] **Step 3: Attach and wire the `Piton` script**

Select root `Item_Piton`. `Add Component → Piton`. Drag the `Visual` child Transform onto the `Visual` field.

Leave `Plant Distance = 0.05`, `Plant Duration = 0.2`.

- [ ] **Step 4: Save as prefab**

Drag root into `Assets/IcePEAK/Prefabs/Items/`. Delete the Hierarchy instance.

- [ ] **Step 5: Commit**

```bash
git add Assets/IcePEAK/Prefabs/Items/Item_Piton.prefab Assets/IcePEAK/Prefabs/Items/Item_Piton.prefab.meta
git commit -m "feat(items): add Item_Piton placeholder prefab (shaft + eye tab)"
```

---

## Task 10: Wire items into `TestScene` belt slots

**Files:**
- Modify: `Assets/IcePEAK/Scenes/TestScene.unity`

- [ ] **Step 1: Open `TestScene`**

In Unity's Project window, double-click `Assets/IcePEAK/Scenes/TestScene.unity`.

- [ ] **Step 2: Locate the four belt slots**

In the Hierarchy, expand `XR Origin (XR Rig) → GadgetBelt`. The children are `Slot_FrontLeft`, `Slot_FrontRight`, `Slot_SideLeft`, `Slot_SideRight`. (If naming differs after the earlier forward-arc refactor, the four BeltSlot children are still present in that order.)

- [ ] **Step 3: Wire `initialPrefab` on each slot**

For each slot, select it in the Hierarchy, then in the Inspector find the `Belt Slot (Script)` component's `Initial Prefab` field. Drag the matching prefab from the Project window:

| Slot | `Initial Prefab` |
|---|---|
| `Slot_FrontLeft` | `Item_GrappleGun` |
| `Slot_FrontRight` | `Item_ColdSpray` |
| `Slot_SideLeft` | `Item_Piton` |
| `Slot_SideRight` | (leave empty — reserved for empty-slot swap test) |

- [ ] **Step 4: Save the scene**

`File → Save` (or `Ctrl+S`).

- [ ] **Step 5: Verify on disk**

```bash
git status --short Assets/IcePEAK/Scenes/TestScene.unity
```

Expected: `M Assets/IcePEAK/Scenes/TestScene.unity`.

Spot-check the diff: exactly three `initialPrefab:` fields should change from `{fileID: 0}` to a real prefab reference with a `guid:` entry. Visually inspect:

```bash
git diff Assets/IcePEAK/Scenes/TestScene.unity
```

Expected: three hunks showing `initialPrefab: {fileID: 0}` → `initialPrefab: {fileID: 100100000, guid: <prefab-guid>, type: 3}`, one per filled slot.

- [ ] **Step 6: Commit**

```bash
git add Assets/IcePEAK/Scenes/TestScene.unity
git commit -m "feat(items): wire GrappleGun/ColdSpray/Piton into TestScene belt slots"
```

---

## Task 11: Play-mode verification

**Files:** none modified.

Run through the nine manual tests from spec §6. This is the acceptance gate — if any test fails, stop and open a task to diagnose.

- [ ] **Step 1: Unity compile check**

Open Unity Console. Expected: no red errors, no new yellow warnings attributable to the new code.

- [ ] **Step 2: Enter Play mode and look at the belt**

Expected: three filled slots showing the three item primitive shapes (pistol, canister, shaft+tab); `Slot_SideRight` shows the sphere placeholder.

- [ ] **Step 3: Draw an item (trigger at a filled slot)**

Reach a hand toward any filled slot so the slot highlights, press trigger. Expected: item parents to the hand cell; Console shows `[<ItemName>] BeltSlot -> Hand`.

- [ ] **Step 4: Activate the held item (trigger away from any slot)**

Move the hand away so no slot is highlighted, press trigger. Expected: item plays its signature visual (streak / mist / plant pulse). Console shows `[<HandInteractionController name>] Activate -> Item_<ItemName>(Clone)`.

- [ ] **Step 5: Swap without activating (the quirk-prevention test)**

While holding an item, hover a filled slot so it highlights, press trigger. Expected: items swap between hand and slot. Activation visual does NOT play during the swap frame.

- [ ] **Step 6: Empty-hand trigger**

With hand empty and no slot hovered, press trigger. Expected: no Console errors, no visual.

- [ ] **Step 7: Non-embedded ice pick in hand**

(Left hand holds a pick by default. Don't swing it into ice.) Press trigger away from any slot. Expected: no Console errors, no activation log (IcePickController is `IHoldable` but not `IActivatable`).

- [ ] **Step 8: Rapid-fire trigger**

Hold an item, away from slots, tap trigger 5 times quickly. Expected: visual plays only on the first press within its duration window; subsequent presses are silently ignored (no coroutine stacking, no visual smear).

- [ ] **Step 9: Stow + redraw the same item**

Draw an item from a slot, then return to the now-empty slot and trigger again (stow). Console: two log lines in order — `[<ItemName>] BeltSlot -> Hand` then `[<ItemName>] Hand -> BeltSlot`.

- [ ] **Step 10: Record results**

If all nine play-mode tests pass, the branch is ready for review/merge. If any test fails, note which and open an investigation task before proceeding.

---

## Completion

All 11 tasks complete = `BasicItems` branch contains:
- `IActivatable` interface + three items (`GrappleGun`, `ColdSpray`, `Piton`)
- HIC rung 3 wiring
- Placeholder material + three prefabs
- `TestScene` belt slots loaded with the three items
- Verified in play mode

Next step (separate task, not this plan): open a PR merging `BasicItems` → `main`.
