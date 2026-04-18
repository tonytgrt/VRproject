# Gadget Belt — P0 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the P0 gadget belt — a 4-slot waist-height holster parented to the XR rig that rotates with HMD yaw, with per-hand swap/stow/draw via trigger press and a debug visualizer for distance checks.

**Architecture:** Plain MonoBehaviours in `Assets/IcePEAK/Scripts/Gadgets/`. Belt positioning runs in `LateUpdate` so HMD pose is already written. Slot/hand cells share an `ICell` interface; items implement `IHoldable.OnTransfer`. Swap/stow/draw is one snapshot-and-replace code path. `IcePickController` gets the single integration seam (`IHoldable` + `SetStowed`). No new physics layers, no test runner — verification is manual in-editor per `doc/Plans/gadget-belt.md §7`.

**Tech Stack:** Unity 6.3 LTS (6000.3.13f1), URP 17.3, XR Interaction Toolkit 3.3.1, OpenXR 1.16, Meta XR SDK 85, Input System 1.19. Build target Android (Quest 3). Assembly: default `Assembly-CSharp` (no `.asmdef`).

**Spec:** `doc/Plans/gadget-belt.md` (approved 2026-04-17). Refer to it for rationale — this plan is the execution breakdown.

**Testing note:** There is no headless test runner. Each task's "verify" step is manual — open `Assets/IcePEAK/Scenes/TestScene.unity`, enter Play, check the Unity Console for errors, and confirm the described visible behavior (XR Simulator is at `Assets/XR/Resources/XRSimulationRuntimeSettings.asset`; on-device is also acceptable). After every script edit, let Unity recompile and read the Console before proceeding.

**Commit cadence:** Commit at the end of every task. Stage scripts + their `.meta` + any scene/prefab edits.

---

## File map

**Created (all under `Assets/IcePEAK/Scripts/Gadgets/`):**

| File | Responsibility |
|---|---|
| `CellKind.cs` | `enum CellKind { Hand, BeltSlot }` |
| `IHoldable.cs` | `interface IHoldable` — `OnTransfer(CellKind from, CellKind to)` |
| `ICell.cs` | `interface ICell` — `HeldItem`, `Anchor`, `Kind`, `Place`, `Take` |
| `HandCell.cs` | MonoBehaviour — adopts first child at Awake; optional `initialPrefab` fallback |
| `BeltSlot.cs` | MonoBehaviour — same adoption pattern + `SetHighlighted(bool)` emissive toggle |
| `GadgetBelt.cs` | MonoBehaviour — `LateUpdate` yaw sync; owns `slots[]`; `TryGetNearestSlot` |
| `HandInteractionController.cs` | Per-hand — proximity tracking, trigger priority, `ResolveBeltAction` |
| `HandInteractionDebugVisualizer.cs` | Sibling — `LineRenderer` per slot, colored by hover state |

**Modified:**

| File | Change |
|---|---|
| `Assets/IcePEAK/Scripts/IcePick/IcePickController.cs` | Implement `IHoldable`; add `SetStowed(bool)`; wire tip collider + swing detector refs if not already serialized. Embed/release logic unchanged. |

**Scene / prefab edits (all in `Assets/IcePEAK/`):**

| Asset | Change |
|---|---|
| `Prefabs/GadgetBelt.prefab` (new) | Root with `GadgetBelt` + visible placeholder; 4 slot child GameObjects with `BeltSlot` + wireframe-ring placeholder mesh. |
| `Scenes/TestScene.unity` | Add `HandCell` child under Left/Right Controller; reparent `IcePickLeft/Right` under those HandCells; add `HandInteractionController` + `HandInteractionDebugVisualizer` to each controller; instantiate `GadgetBelt` prefab as child of `XR Origin (XR Rig)` root. |

---

## Task 1: Scaffold folder + trivial interfaces

**Files:**
- Create: `Assets/IcePEAK/Scripts/Gadgets/CellKind.cs`
- Create: `Assets/IcePEAK/Scripts/Gadgets/IHoldable.cs`
- Create: `Assets/IcePEAK/Scripts/Gadgets/ICell.cs`

- [ ] **Step 1: Create the folder**

Create `Assets/IcePEAK/Scripts/Gadgets/` via the Unity Project window (right-click `Assets/IcePEAK/Scripts` → Create → Folder → `Gadgets`). This produces `Gadgets.meta` alongside.

- [ ] **Step 2: Write `CellKind.cs`**

```csharp
namespace IcePEAK.Gadgets
{
    public enum CellKind
    {
        Hand,
        BeltSlot
    }
}
```

- [ ] **Step 3: Write `IHoldable.cs`**

```csharp
namespace IcePEAK.Gadgets
{
    /// <summary>
    /// Implemented by any item that can live in a HandCell or BeltSlot.
    /// Called once per transfer, after reparenting is complete.
    /// </summary>
    public interface IHoldable
    {
        void OnTransfer(CellKind from, CellKind to);
    }
}
```

- [ ] **Step 4: Write `ICell.cs`**

```csharp
using UnityEngine;

namespace IcePEAK.Gadgets
{
    /// <summary>
    /// A single-item container — either a hand or a belt slot.
    /// Held items are parented to Anchor.
    /// </summary>
    public interface ICell
    {
        GameObject HeldItem { get; }
        Transform Anchor { get; }
        CellKind Kind { get; }

        /// Register <paramref name="item"/> as this cell's content. Caller parents the transform.
        void Place(GameObject item);

        /// Returns the current HeldItem (or null) and clears the cell. Does not reparent.
        GameObject Take();
    }
}
```

- [ ] **Step 5: Verify compile**

In Unity: let the editor recompile (watch for the spinner in the bottom-right). Then use Unity MCP to check the console:

```
read_console(types=["error"], count=10)
```

Expected: zero errors. The three files define only types — no runtime behavior yet.

- [ ] **Step 6: Commit**

```bash
git add Assets/IcePEAK/Scripts/Gadgets.meta \
        Assets/IcePEAK/Scripts/Gadgets/CellKind.cs \
        Assets/IcePEAK/Scripts/Gadgets/CellKind.cs.meta \
        Assets/IcePEAK/Scripts/Gadgets/IHoldable.cs \
        Assets/IcePEAK/Scripts/Gadgets/IHoldable.cs.meta \
        Assets/IcePEAK/Scripts/Gadgets/ICell.cs \
        Assets/IcePEAK/Scripts/Gadgets/ICell.cs.meta
git commit -m "feat(gadgets): scaffold Gadgets folder with ICell/IHoldable/CellKind"
```

---

## Task 2: `HandCell` MonoBehaviour

A HandCell's own transform is the anchor. Held items parent to `this.transform`. Adopt the first existing child at Awake (handles pre-parented picks). Otherwise, optionally instantiate `initialPrefab`.

**Files:**
- Create: `Assets/IcePEAK/Scripts/Gadgets/HandCell.cs`

- [ ] **Step 1: Write `HandCell.cs`**

```csharp
using UnityEngine;

namespace IcePEAK.Gadgets
{
    /// <summary>
    /// "Hand holds one item" cell. Attach to a child GameObject of the hand controller.
    /// The GameObject's own transform is the anchor — held items parent to it.
    /// </summary>
    public class HandCell : MonoBehaviour, ICell
    {
        [Tooltip("If the anchor has no existing child at Awake, instantiate this prefab into it. Optional.")]
        [SerializeField] private GameObject initialPrefab;

        public GameObject HeldItem { get; private set; }
        public Transform Anchor => transform;
        public CellKind Kind => CellKind.Hand;

        private void Awake()
        {
            // Rule 1: adopt first existing child (e.g., IcePickLeft reparented under us in the scene).
            if (transform.childCount > 0)
            {
                HeldItem = transform.GetChild(0).gameObject;
                return;
            }

            // Rule 2: instantiate initialPrefab if provided.
            if (initialPrefab != null)
            {
                var inst = Instantiate(initialPrefab, transform);
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
                HeldItem = inst;
            }
        }

        public void Place(GameObject item) { HeldItem = item; }

        public GameObject Take()
        {
            var item = HeldItem;
            HeldItem = null;
            return item;
        }
    }
}
```

- [ ] **Step 2: Verify compile**

```
read_console(types=["error"], count=10)
```

Expected: zero errors.

- [ ] **Step 3: Commit**

```bash
git add Assets/IcePEAK/Scripts/Gadgets/HandCell.cs \
        Assets/IcePEAK/Scripts/Gadgets/HandCell.cs.meta
git commit -m "feat(gadgets): add HandCell with child-adoption + initialPrefab fallback"
```

---

## Task 3: `BeltSlot` MonoBehaviour

Mirror of `HandCell` with a `SetHighlighted` hook. Highlight uses emissive toggling on renderers — swap for an outline shader in P1.

**Files:**
- Create: `Assets/IcePEAK/Scripts/Gadgets/BeltSlot.cs`

- [ ] **Step 1: Write `BeltSlot.cs`**

```csharp
using UnityEngine;

namespace IcePEAK.Gadgets
{
    /// <summary>
    /// Single belt slot. Same adoption/initialPrefab pattern as HandCell.
    /// SetHighlighted toggles emissive on the held item's renderers, or on the
    /// placeholder renderer if the slot is empty.
    /// </summary>
    public class BeltSlot : MonoBehaviour, ICell
    {
        [Tooltip("If the anchor has no existing child at Awake, instantiate this prefab. Optional.")]
        [SerializeField] private GameObject initialPrefab;

        [Tooltip("Renderer on the empty-slot wireframe placeholder. Highlighted when slot is empty.")]
        [SerializeField] private Renderer placeholderRenderer;

        [Tooltip("Emissive color applied on highlight.")]
        [SerializeField] private Color highlightEmissive = new Color(1f, 0.85f, 0.3f) * 2f;

        public GameObject HeldItem { get; private set; }
        public Transform Anchor => transform;
        public CellKind Kind => CellKind.BeltSlot;

        private bool _highlighted;
        private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            // The placeholder renderer is a child of this slot (e.g., a wireframe ring).
            // It should NOT count as an "adopted" held item. Skip it when checking for adoption.
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i).gameObject;
                if (placeholderRenderer != null && child.transform == placeholderRenderer.transform)
                    continue;
                HeldItem = child;
                break;
            }

            if (HeldItem == null && initialPrefab != null)
            {
                var inst = Instantiate(initialPrefab, transform);
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
                HeldItem = inst;
            }
        }

        public void Place(GameObject item)
        {
            HeldItem = item;
            RefreshHighlightTarget();
        }

        public GameObject Take()
        {
            var item = HeldItem;
            HeldItem = null;
            RefreshHighlightTarget();
            return item;
        }

        public void SetHighlighted(bool on)
        {
            _highlighted = on;
            RefreshHighlightTarget();
        }

        private void RefreshHighlightTarget()
        {
            // Full slot → light up the held item. Empty slot → light up the placeholder.
            var renderers = HeldItem != null
                ? HeldItem.GetComponentsInChildren<Renderer>()
                : (placeholderRenderer != null ? new[] { placeholderRenderer } : System.Array.Empty<Renderer>());

            foreach (var r in renderers)
            {
                if (r == null) continue;
                var mat = r.material; // instance, not shared — acceptable for P0
                if (_highlighted)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor(EmissionColorID, highlightEmissive);
                }
                else
                {
                    mat.SetColor(EmissionColorID, Color.black);
                }
            }
        }
    }
}
```

- [ ] **Step 2: Verify compile**

```
read_console(types=["error"], count=10)
```

Expected: zero errors.

- [ ] **Step 3: Commit**

```bash
git add Assets/IcePEAK/Scripts/Gadgets/BeltSlot.cs \
        Assets/IcePEAK/Scripts/Gadgets/BeltSlot.cs.meta
git commit -m "feat(gadgets): add BeltSlot with emissive highlight toggle"
```

---

## Task 4: `GadgetBelt` — positioning + slot registry + nearest-slot lookup

Parent of the four `BeltSlot`s. `LateUpdate` sets yaw from the HMD. `TryGetNearestSlot` is pure math.

**Files:**
- Create: `Assets/IcePEAK/Scripts/Gadgets/GadgetBelt.cs`

- [ ] **Step 1: Write `GadgetBelt.cs`**

```csharp
using UnityEngine;

namespace IcePEAK.Gadgets
{
    /// <summary>
    /// Waist-height belt. Parent to the XR Origin (XR Rig) root, NOT Camera Offset.
    /// Position is static in the prefab; only yaw is updated each LateUpdate from the HMD.
    /// </summary>
    public class GadgetBelt : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Main Camera (HMD) transform — belt yaw tracks this.")]
        [SerializeField] private Transform hmd;

        [Tooltip("All belt slots, in the order you want them iterated.")]
        [SerializeField] private BeltSlot[] slots;

        [Header("Tunables")]
        [Tooltip("Max hand→slot distance that counts as 'hovered'. Meters.")]
        [SerializeField] private float proximityRadius = 0.15f;

        public BeltSlot[] Slots => slots;
        public float ProximityRadius => proximityRadius;

        private void LateUpdate()
        {
            if (hmd == null) return;
            // World-space yaw only — no pitch, no roll, no bobbing when looking up/down.
            transform.rotation = Quaternion.Euler(0f, hmd.eulerAngles.y, 0f);
        }

        /// <summary>
        /// Nearest slot to <paramref name="handWorldPos"/> within proximityRadius.
        /// Deterministic: returns the single closest slot, or null if none in range.
        /// </summary>
        public bool TryGetNearestSlot(Vector3 handWorldPos, out BeltSlot nearest)
        {
            nearest = null;
            if (slots == null) return false;

            float bestSqr = proximityRadius * proximityRadius;
            for (int i = 0; i < slots.Length; i++)
            {
                var s = slots[i];
                if (s == null) continue;
                float sqr = (s.Anchor.position - handWorldPos).sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    nearest = s;
                }
            }
            return nearest != null;
        }
    }
}
```

- [ ] **Step 2: Verify compile**

```
read_console(types=["error"], count=10)
```

Expected: zero errors.

- [ ] **Step 3: Commit**

```bash
git add Assets/IcePEAK/Scripts/Gadgets/GadgetBelt.cs \
        Assets/IcePEAK/Scripts/Gadgets/GadgetBelt.cs.meta
git commit -m "feat(gadgets): add GadgetBelt with HMD-yaw LateUpdate + nearest-slot lookup"
```

---

## Task 5: Build the `GadgetBelt` prefab + wire into `TestScene`

Create the visible belt and place it under the rig so positioning can be verified before any interaction is wired.

**Files:**
- Create: `Assets/IcePEAK/Prefabs/GadgetBelt.prefab`
- Modify: `Assets/IcePEAK/Scenes/TestScene.unity`

- [ ] **Step 1: Create belt root hierarchy in TestScene**

In `Assets/IcePEAK/Scenes/TestScene.unity`:

1. Locate the `XR Origin (XR Rig)` GameObject.
2. Right-click it → Create Empty → rename `GadgetBelt`.
3. Set its transform to `(0, 1.0, 0)` local, rotation `(0, 0, 0)`, scale `(1, 1, 1)`. The `1.0` is `waistHeight` above the rig root (rig root = player's feet).
4. Add component `GadgetBelt` (script just written).
5. As a **visible placeholder** so you can see the belt at runtime: Create Empty child → rename `Placeholder_BeltBand` → add a `MeshFilter` with a default Cube, `MeshRenderer` with default material; scale to `(0.40, 0.05, 0.25)`, position `(0, 0, 0)`.

- [ ] **Step 2: Create the 4 slot child GameObjects**

Under the `GadgetBelt` GameObject, create 4 empty children — one per slot. Each gets a `BeltSlot` component and a visible wireframe-ring placeholder.

For **each** slot:
1. Right-click `GadgetBelt` → Create Empty → rename per table below.
2. Set local position per the table, rotation `(0, 0, 0)`, scale `(1, 1, 1)`.
3. Add component `BeltSlot`.
4. Create child of this slot → rename `Placeholder_Ring` → add `MeshFilter` (default Cylinder), `MeshRenderer` (default material), scale `(0.06, 0.005, 0.06)`, position `(0, 0, 0)`. This is the locatable "empty slot" marker.
5. On the `BeltSlot` component, drag `Placeholder_Ring`'s `MeshRenderer` into the `Placeholder Renderer` field.

| Slot GameObject name | Local Position (x, y, z) |
|---|---|
| `Slot_FrontLeft` | `(-0.12, 0, +0.09)` |
| `Slot_SideLeft` | `(-0.18, 0, 0)` |
| `Slot_SideRight` | `(+0.18, 0, 0)` |
| `Slot_FrontRight` | `(+0.12, 0, +0.09)` |

- [ ] **Step 3: Wire `GadgetBelt`'s references**

On the `GadgetBelt` component:
- `Hmd`: drag in `XR Origin (XR Rig)/Camera Offset/Main Camera`.
- `Slots`: set size 4; drag `Slot_FrontLeft`, `Slot_SideLeft`, `Slot_SideRight`, `Slot_FrontRight` into elements 0–3.
- Leave `Proximity Radius` at `0.15`.

- [ ] **Step 4: Save as prefab**

Drag the `GadgetBelt` GameObject from the Hierarchy into `Assets/IcePEAK/Prefabs/`. Choose "Original Prefab" in the dialog. This creates `GadgetBelt.prefab`. The scene instance becomes a prefab instance (arrow on the icon).

- [ ] **Step 5: Verify in-Play**

Save scene and press Play. Check:

1. `read_console(types=["error","warning"], count=10)` — expect clean.
2. You can see the belt cube + 4 tiny ring placeholders near the rig.
3. Physically turn your head (or rotate XR Simulator's head target): belt rotates yaw to match.
4. Look up/down: belt y stays constant (no pitch coupling).
5. If `ClimbingLocomotion` is tested: climb up ~0.5 m; belt stays at waist relative to the rig (rises with you).

If any of these fail, stop and diagnose before committing.

- [ ] **Step 6: Commit**

```bash
git add Assets/IcePEAK/Prefabs/GadgetBelt.prefab \
        Assets/IcePEAK/Prefabs/GadgetBelt.prefab.meta \
        Assets/IcePEAK/Scenes/TestScene.unity
git commit -m "feat(gadgets): add GadgetBelt prefab with 4 slot placeholders and wire into TestScene"
```

---

## Task 6: `IcePickController` implements `IHoldable` + `SetStowed`

Only change to existing gameplay code. Adds the stow/unstow capability and the `OnTransfer` hook the belt will call.

**Files:**
- Modify: `Assets/IcePEAK/Scripts/IcePick/IcePickController.cs`

- [ ] **Step 1: Add `using` and interface + serialized refs**

Open `Assets/IcePEAK/Scripts/IcePick/IcePickController.cs`. At the top of the file, add the namespace import. In the class declaration, implement `IHoldable`. Add serialized references to the tip collider and (if not already there) the swing detector.

Replace the `using` block:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using IcePEAK.Gadgets;
```

Replace the class declaration line:

```csharp
public class IcePickController : MonoBehaviour, IHoldable
```

In the `[Header("References")]` block (currently around lines 8–13), add a tip collider reference **below** the existing `[SerializeField] private Transform tipTransform;`:

```csharp
    [Tooltip("Trigger collider on the tip — disabled while stowed so it doesn't embed in ice.")]
    [SerializeField] private Collider tipCollider;
```

(Note: `swingDetector` is already serialized on line 9 — reuse it.)

- [ ] **Step 2: Add `SetStowed` + `OnTransfer` methods**

At the bottom of the class, before the closing brace, append:

```csharp
    // --- IHoldable ---

    /// <summary>
    /// Called by the belt/hand cell after the pick has been reparented into the new cell.
    /// Stow disables the tip so a holstered pick can't embed in ice.
    /// </summary>
    public void OnTransfer(CellKind from, CellKind to)
    {
        SetStowed(to == CellKind.BeltSlot);
    }

    /// <summary>
    /// Disables/enables the tip trigger collider and the swing detector. Safe to call repeatedly.
    /// </summary>
    public void SetStowed(bool stowed)
    {
        if (tipCollider != null) tipCollider.enabled = !stowed;
        if (swingDetector != null) swingDetector.enabled = !stowed;
        Debug.Log($"[IcePick {gameObject.name}] SetStowed({stowed})");
    }
```

- [ ] **Step 3: Verify compile**

```
read_console(types=["error"], count=10)
```

Expected: zero errors. If there's a "Collider not assigned" warning at Play time, that's fine — we wire it in Step 4.

- [ ] **Step 4: Wire `tipCollider` in the IcePick prefab**

Open `Assets/IcePEAK/Prefabs/IcePick.prefab`. On the root `IcePickController` component, drag the child `TipCollider` GameObject's `Collider` into the new `Tip Collider` field. Apply. Both left and right pick instances in `TestScene` inherit from this prefab — no per-instance wiring needed.

- [ ] **Step 5: Smoke test `SetStowed`**

Enter Play. In the Hierarchy, select `IcePickLeft`. In the Inspector, find the `TipCollider` child → confirm its `Collider.enabled` is true. In the top-right of `IcePickController`, click the cog → choose "Invoke Context Menu" is not present; instead:

Temporarily add a one-line test harness: in the Inspector of `IcePickController`, you won't see SetStowed as a button. Instead, use the Unity MCP:

```
execute_code(code="var pick = GameObject.Find(\"IcePickLeft\").GetComponent<IcePickController>(); pick.SetStowed(true); return pick.GetComponentInChildren<Collider>().enabled;")
```

Expected return: `False`. Then:

```
execute_code(code="var pick = GameObject.Find(\"IcePickLeft\").GetComponent<IcePickController>(); pick.SetStowed(false); return pick.GetComponentInChildren<Collider>().enabled;")
```

Expected return: `True`.

Exit Play. No scene changes to save.

- [ ] **Step 6: Commit**

```bash
git add Assets/IcePEAK/Scripts/IcePick/IcePickController.cs \
        Assets/IcePEAK/Prefabs/IcePick.prefab
git commit -m "feat(icepick): implement IHoldable + SetStowed for belt integration"
```

---

## Task 7: Add `HandCell`s to the scene + reparent existing picks

The existing picks `IcePickLeft` / `IcePickRight` become the initial `HeldItem` of their hand's `HandCell` via the "adopt first child" rule.

**Files:**
- Modify: `Assets/IcePEAK/Scenes/TestScene.unity`

- [ ] **Step 1: Note the picks' current local transforms**

Before moving anything: select `IcePickLeft`. Write down its current local position + rotation (e.g., `(0, 0, 0.08)` / `(0, 0, 0)` — whatever it is). Repeat for `IcePickRight`. You'll restore these so the pick's in-hand pose doesn't change.

- [ ] **Step 2: Create `HandCell_Left` under Left Controller**

1. In `TestScene`, locate `XR Origin (XR Rig)/Camera Offset/Left Controller` (name may be `LeftHand Controller` depending on template — pick the one that holds `IcePickLeft`).
2. Right-click it → Create Empty → rename `HandCell`.
3. Transform: local position `(0, 0, 0)`, rotation `(0, 0, 0)`, scale `(1, 1, 1)`.
4. Add component `HandCell`.

- [ ] **Step 3: Reparent `IcePickLeft` under `HandCell`**

1. In the Hierarchy, drag `IcePickLeft` **onto** the new `HandCell` GameObject (so `IcePickLeft` becomes a child of `HandCell` instead of `Left Controller`).
2. Select `IcePickLeft` and restore the local position + rotation you noted in Step 1.
3. This reparenting also updates `IcePickController._controllerParent` at the next Awake (the cached parent is the scene's current parent at Awake — which is now `HandCell`).

- [ ] **Step 4: Repeat for right hand**

Same as Steps 2–3 but for `Right Controller` → `HandCell` → reparent `IcePickRight` under it, restore local transform.

- [ ] **Step 5: Verify in Play**

Enter Play. Check:

1. `read_console(types=["error","warning"], count=10)` — expect clean.
2. The picks are visible in the hands, in the same pose as before.
3. Unity MCP: confirm the adoption worked.

```
execute_code(code="var hc = GameObject.Find(\"HandCell\").GetComponent<IcePEAK.Gadgets.HandCell>(); return hc.HeldItem == null ? \"null\" : hc.HeldItem.name;")
```

Expected return: the name of a pick (`IcePickLeft` or `IcePickRight` depending on which `HandCell` is found first; use `find_gameobjects` with `search_term="HandCell"` to disambiguate if needed).

4. Enter an ice surface and try to embed + release — pick should still work exactly as before.

Exit Play without saving if you made any temporary changes; **do** save if only the reparenting was done.

- [ ] **Step 6: Commit**

```bash
git add Assets/IcePEAK/Scenes/TestScene.unity
git commit -m "feat(gadgets): add HandCells under each controller and reparent picks into them"
```

---

## Task 8: `HandInteractionDebugVisualizer`

LineRenderers in Game view. Built before `HandInteractionController` so we can see proximity behavior while hover logic is still being written.

**Files:**
- Create: `Assets/IcePEAK/Scripts/Gadgets/HandInteractionDebugVisualizer.cs`
- Modify: `Assets/IcePEAK/Scenes/TestScene.unity`

- [ ] **Step 1: Write `HandInteractionDebugVisualizer.cs`**

```csharp
using UnityEngine;

namespace IcePEAK.Gadgets
{
    /// <summary>
    /// Per-hand debug lines — one LineRenderer per belt slot.
    /// Line is hidden beyond approachRadius; dim white when in approach range; bright green
    /// when the slot is the current hover target (i.e. would fire on trigger press).
    /// </summary>
    public class HandInteractionDebugVisualizer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HandCell handCell;
        [SerializeField] private GadgetBelt belt;
        [Tooltip("Sibling controller — used to read the current hover target.")]
        [SerializeField] private HandInteractionController handController;

        [Header("Settings")]
        [SerializeField] private bool debugEnabled = true;
        [Tooltip("Lines appear when hand is within this distance of a slot.")]
        [SerializeField] private float approachRadius = 0.30f;
        [SerializeField] private Color activeColor = new Color(0.2f, 1f, 0.3f, 1f);
        [SerializeField] private Color approachColor = new Color(1f, 1f, 1f, 0.5f);
        [SerializeField] private float activeWidth = 0.004f;
        [SerializeField] private float approachWidth = 0.0015f;

        private LineRenderer[] _lines;

        private void Start()
        {
            if (belt == null || belt.Slots == null) return;

            _lines = new LineRenderer[belt.Slots.Length];
            for (int i = 0; i < belt.Slots.Length; i++)
            {
                var go = new GameObject($"DebugLine_{i}");
                go.transform.SetParent(transform, worldPositionStays: false);
                var lr = go.AddComponent<LineRenderer>();
                lr.positionCount = 2;
                lr.useWorldSpace = true;
                lr.material = new Material(Shader.Find("Sprites/Default"));
                lr.enabled = false;
                _lines[i] = lr;
            }
        }

        private void Update()
        {
            if (!debugEnabled || _lines == null || handCell == null || belt == null) return;

            Vector3 handPos = handCell.Anchor.position;
            var hovered = handController != null ? handController.CurrentHoveredSlot : null;

            for (int i = 0; i < belt.Slots.Length; i++)
            {
                var slot = belt.Slots[i];
                var lr = _lines[i];
                if (slot == null || lr == null) continue;

                Vector3 slotPos = slot.Anchor.position;
                float dist = Vector3.Distance(handPos, slotPos);

                if (dist > approachRadius)
                {
                    lr.enabled = false;
                    continue;
                }

                lr.enabled = true;
                lr.SetPosition(0, handPos);
                lr.SetPosition(1, slotPos);

                bool isActive = slot == hovered;
                lr.startColor = lr.endColor = isActive ? activeColor : approachColor;
                lr.startWidth = lr.endWidth = isActive ? activeWidth : approachWidth;
            }
        }
    }
}
```

- [ ] **Step 2: Verify compile**

```
read_console(types=["error"], count=10)
```

Expected: one error about `HandInteractionController.CurrentHoveredSlot` not existing — we'll add it in Task 9. **Do not attach the component yet**; leave the script unreferenced until Task 9 is done. Proceed to Step 3 once the *only* error is that missing symbol.

Actually: stub it now so this task's compile is clean. Create a temporary placeholder class at the bottom of `HandInteractionDebugVisualizer.cs`? No — put the full class in Task 9 and expect this specific compile error. Simpler: reorder — skip attachment until Task 9 wires both scripts together. That leaves an unresolved symbol across tasks, which breaks the "commit after each task" rule.

**Resolution:** split this task — write the visualizer *and* a minimal `HandInteractionController` stub with only `CurrentHoveredSlot` defined, commit both together. Steps 3–5 below reflect this.

- [ ] **Step 3: Write minimal `HandInteractionController` stub**

Create `Assets/IcePEAK/Scripts/Gadgets/HandInteractionController.cs`:

```csharp
using UnityEngine;

namespace IcePEAK.Gadgets
{
    /// <summary>
    /// Per-hand belt interaction controller. Task 9 fleshes out proximity + trigger logic.
    /// This stub exists so the debug visualizer can read CurrentHoveredSlot.
    /// </summary>
    public class HandInteractionController : MonoBehaviour
    {
        public BeltSlot CurrentHoveredSlot { get; protected set; }
    }
}
```

- [ ] **Step 4: Verify compile**

```
read_console(types=["error"], count=10)
```

Expected: zero errors.

- [ ] **Step 5: Attach to both controllers in TestScene**

For **each** of Left Controller and Right Controller:
1. Add component `HandInteractionController` to the controller GameObject.
2. Add component `HandInteractionDebugVisualizer` to the same controller GameObject.
3. On `HandInteractionDebugVisualizer`, wire:
   - `Hand Cell`: the `HandCell` child of this controller.
   - `Belt`: the `GadgetBelt` in the scene.
   - `Hand Controller`: the sibling `HandInteractionController` you just added.
   - Leave other fields at defaults.

- [ ] **Step 6: Verify in Play**

Enter Play. Check:

1. `read_console(types=["error","warning"], count=10)` — clean.
2. Move a hand toward the belt. Within 30 cm of any slot, a thin white line should appear from the hand to that slot.
3. Move hand out of range — lines disappear.
4. No lines turn green yet (hover logic lands in Task 9). That's expected.

- [ ] **Step 7: Commit**

```bash
git add Assets/IcePEAK/Scripts/Gadgets/HandInteractionDebugVisualizer.cs \
        Assets/IcePEAK/Scripts/Gadgets/HandInteractionDebugVisualizer.cs.meta \
        Assets/IcePEAK/Scripts/Gadgets/HandInteractionController.cs \
        Assets/IcePEAK/Scripts/Gadgets/HandInteractionController.cs.meta \
        Assets/IcePEAK/Scenes/TestScene.unity
git commit -m "feat(gadgets): add HandInteractionDebugVisualizer + HandInteractionController stub"
```

---

## Task 9: `HandInteractionController` — hover tracking + highlight

Fill in the stub: nearest-slot tracking, emissive highlight on hover. No trigger input yet — that's Task 10.

**Files:**
- Modify: `Assets/IcePEAK/Scripts/Gadgets/HandInteractionController.cs`
- Modify: `Assets/IcePEAK/Scenes/TestScene.unity` (wire new serialized refs)

- [ ] **Step 1: Replace the stub with the hover implementation**

Overwrite `HandInteractionController.cs` with:

```csharp
using UnityEngine;

namespace IcePEAK.Gadgets
{
    /// <summary>
    /// Per-hand belt interaction controller. This phase: hover tracking only.
    /// Task 10 adds trigger input and swap/stow/draw.
    /// </summary>
    public class HandInteractionController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HandCell handCell;
        [SerializeField] private GadgetBelt belt;

        public BeltSlot CurrentHoveredSlot { get; private set; }

        private void Update()
        {
            if (handCell == null || belt == null) return;

            belt.TryGetNearestSlot(handCell.Anchor.position, out var nearest);

            if (nearest == CurrentHoveredSlot) return;

            // Move highlight
            if (CurrentHoveredSlot != null) CurrentHoveredSlot.SetHighlighted(false);
            CurrentHoveredSlot = nearest;
            if (CurrentHoveredSlot != null) CurrentHoveredSlot.SetHighlighted(true);
        }

        private void OnDisable()
        {
            if (CurrentHoveredSlot != null)
            {
                CurrentHoveredSlot.SetHighlighted(false);
                CurrentHoveredSlot = null;
            }
        }
    }
}
```

Note the `CurrentHoveredSlot` setter is now `private`; the stub had it `protected set` — downgrading is safe because the visualizer only reads it.

- [ ] **Step 2: Verify compile**

```
read_console(types=["error"], count=10)
```

Expected: zero errors.

- [ ] **Step 3: Wire new refs in TestScene**

For **each** `HandInteractionController` (Left and Right controllers):
- `Hand Cell`: the `HandCell` child of this controller.
- `Belt`: the `GadgetBelt` in the scene.

- [ ] **Step 4: Verify in Play**

Enter Play. Check:

1. `read_console(types=["error","warning"], count=10)` — clean.
2. Move a hand within ~15 cm of a slot. That slot's placeholder ring (if empty) or held item (if full) glows emissive.
3. The debug line to that slot turns **green** (active color), others within range stay dim white.
4. Move hand between slots — highlight tracks to the nearest. Old slot un-highlights cleanly.
5. Move hand away — nothing is highlighted; all debug lines disappear beyond 30 cm.

- [ ] **Step 5: Commit**

```bash
git add Assets/IcePEAK/Scripts/Gadgets/HandInteractionController.cs \
        Assets/IcePEAK/Scenes/TestScene.unity
git commit -m "feat(gadgets): add per-hand hover tracking + slot highlight"
```

---

## Task 10: `HandInteractionController` — trigger priority + swap/stow/draw

Final P0 task. Adds trigger input, priority resolution, and the unified `ResolveBeltAction`.

**Files:**
- Modify: `Assets/IcePEAK/Scripts/Gadgets/HandInteractionController.cs`
- Modify: `Assets/IcePEAK/Scenes/TestScene.unity` (wire trigger action + pick reference)

- [ ] **Step 1: Replace with final implementation**

Overwrite `HandInteractionController.cs` with:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

namespace IcePEAK.Gadgets
{
    /// <summary>
    /// Per-hand belt interaction controller. Priority order each frame:
    ///   1. Pick embedded → pick owns trigger (we early-return, pick handles release).
    ///   2. Trigger rising-edge + hand over a slot → swap/stow/draw.
    ///   3. Otherwise no-op.
    /// </summary>
    public class HandInteractionController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HandCell handCell;
        [SerializeField] private GadgetBelt belt;
        [Tooltip("This hand's pick, for the IsEmbedded priority check. Leave null if this hand never holds a pick.")]
        [SerializeField] private IcePickController pick;

        [Header("Input")]
        [Tooltip("Same XRI Activate Value action the pick uses for release.")]
        [SerializeField] private InputActionReference triggerAction;

        public BeltSlot CurrentHoveredSlot { get; private set; }

        private void OnEnable()
        {
            if (triggerAction != null && triggerAction.action != null)
                triggerAction.action.Enable();
        }

        private void Update()
        {
            if (handCell == null || belt == null) return;

            // --- Hover update (same as Task 9) ---
            belt.TryGetNearestSlot(handCell.Anchor.position, out var nearest);
            if (nearest != CurrentHoveredSlot)
            {
                if (CurrentHoveredSlot != null) CurrentHoveredSlot.SetHighlighted(false);
                CurrentHoveredSlot = nearest;
                if (CurrentHoveredSlot != null) CurrentHoveredSlot.SetHighlighted(true);
            }

            // --- Priority resolution ---
            // P1: if pick is embedded, pick's own Update handles trigger-to-release.
            if (pick != null && pick.IsEmbedded) return;

            if (triggerAction == null || triggerAction.action == null) return;
            if (!triggerAction.action.WasPressedThisFrame()) return;

            // P3: nothing hovered → no-op.
            if (CurrentHoveredSlot == null) return;

            // P2: swap/stow/draw.
            ResolveBeltAction(CurrentHoveredSlot);
        }

        private void OnDisable()
        {
            if (CurrentHoveredSlot != null)
            {
                CurrentHoveredSlot.SetHighlighted(false);
                CurrentHoveredSlot = null;
            }
        }

        /// <summary>
        /// Unified swap/stow/draw. Snapshot both cells, empty both, re-place into swapped cells.
        /// Draw = handItem null. Stow = slotItem null. Swap = both non-null. No-op = both null.
        /// </summary>
        private void ResolveBeltAction(BeltSlot slot)
        {
            var handItem = handCell.HeldItem;
            var slotItem = slot.HeldItem;

            if (handItem == null && slotItem == null) return;

            handCell.Take();
            slot.Take();

            if (slotItem != null) PlaceInto(handCell, slotItem, CellKind.BeltSlot);
            if (handItem != null) PlaceInto(slot,     handItem, CellKind.Hand);

            // Re-highlight the now-current contents (held item vs placeholder may have changed).
            slot.SetHighlighted(true);
        }

        private static void PlaceInto(ICell cell, GameObject item, CellKind from)
        {
            item.transform.SetParent(cell.Anchor, worldPositionStays: false);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
            cell.Place(item);
            var holdable = item.GetComponent<IHoldable>();
            holdable?.OnTransfer(from, cell.Kind);
        }
    }
}
```

- [ ] **Step 2: Verify compile**

```
read_console(types=["error"], count=10)
```

Expected: zero errors.

- [ ] **Step 3: Wire new refs in TestScene**

For **each** `HandInteractionController`:
- `Pick`: drag the `IcePickController` component of this hand's pick (`IcePickLeft` / `IcePickRight`).
- `Trigger Action`: assign the same XRI `Activate Value` action reference as the pick uses. Left hand → `XRI LeftHand Interaction/Activate Value`. Right hand → `XRI RightHand Interaction/Activate Value`. These live in `Assets/XRI/`.

- [ ] **Step 4: Optional — seed a placeholder gadget for testing swap**

To test draw/swap (not just stow), set one belt slot's `Initial Prefab` to a small cube prefab:

1. Create `Assets/IcePEAK/Prefabs/TestGadget_Cube.prefab`: a 5 cm cube, any color, no scripts.
2. On the `GadgetBelt` prefab, open one slot (e.g., `Slot_FrontRight`), set `Initial Prefab` → `TestGadget_Cube`.
3. Save the prefab.

- [ ] **Step 5: Run the 14-item manual test plan**

From `doc/Plans/gadget-belt.md §7`, step through each row. Summary:

| # | Check |
|---|---|
| 1 | Climb → belt rises with rig. |
| 2 | Turn head → belt yaw follows. |
| 3 | Look up/down → belt y unchanged. |
| 4 | Debug line appears at ≤30 cm. |
| 5 | Nearest-slot highlight + green debug line. |
| 6 | **Stow:** pick in hand, hover empty slot, trigger → pick parents into slot, hand empty, tip collider disabled. Verify with `execute_code(code="return GameObject.Find(\"IcePickLeft\").GetComponentInChildren<Collider>().enabled;")` → `False`. |
| 7 | **Draw:** empty hand over `TestGadget_Cube` slot, trigger → cube in hand. |
| 8 | **Swap:** pick in hand, hover cube slot, trigger → pick in slot, cube in hand. |
| 9 | Embed pick in ice, trigger → pick releases (belt does nothing). |
| 10 | Free hand trigger in mid-air → nothing. |
| 11 | Stow pick, then move it past ice via scene manipulation → no embed event (tip collider off). |
| 12 | Draw a stowed pick, swing into ice → embeds normally (`SetStowed(false)` re-enabled tip). |
| 13 | Set `HandCell.initialPrefab` or slot `initialPrefab` → cell populated at Play. |
| 14 | Stow/draw simultaneously with both hands → per-hand `CurrentHoveredSlot` independent. |

If any fail, diagnose before committing. Most common issues:
- Pick slides off slot visually: slot anchor orientation. Rotate the slot GameObject so the pick sits naturally.
- Trigger double-fires (release + swap in one press): shouldn't happen because `!pick.IsEmbedded` gates the belt branch, but if it does, confirm `IcePickController.Update` still has the `if (!_isEmbedded) return;` guard on line 68.
- Debug line never turns green: `HandInteractionController` not wired into `HandInteractionDebugVisualizer.Hand Controller` field.

- [ ] **Step 6: Commit**

```bash
git add Assets/IcePEAK/Scripts/Gadgets/HandInteractionController.cs \
        Assets/IcePEAK/Scenes/TestScene.unity \
        Assets/IcePEAK/Prefabs/GadgetBelt.prefab
# If you added TestGadget_Cube.prefab:
git add Assets/IcePEAK/Prefabs/TestGadget_Cube.prefab \
        Assets/IcePEAK/Prefabs/TestGadget_Cube.prefab.meta 2>/dev/null || true
git commit -m "feat(gadgets): add trigger priority + swap/stow/draw to HandInteractionController"
```

---

## Self-review pass (done)

- **Spec coverage:** §3.1 scripts → Tasks 1–4, 8, 9, 10. §3.2 object graph → Tasks 5, 7, 8. §3.3 positioning → Task 4. §3.4 slot layout → Task 5. §3.5 initial loadout → Tasks 2, 3 (adoption + `initialPrefab`). §4.1 proximity → Task 4. §4.2 highlight → Tasks 3, 9. §4.3 trigger priority → Task 10. §4.4 swap/stow/draw → Task 10. §4.5 `IHoldable` → Tasks 1, 6. §4.6 edge cases → covered by manual test plan in Task 10 step 5. §5 debug visualizer → Task 8. §6.1 IcePickController → Task 6. §6.2 ClimbingLocomotion (no changes) → confirmed. §6.3/6.4 no new layers/inputs → confirmed. §7 manual test plan → Task 10 step 5. §8 impl order → matches this plan 1:1.
- **Type consistency:** `ICell.Anchor` (uppercase) used consistently; `BeltSlot.Slots` exposed via `GadgetBelt.Slots`; `CellKind` values match between `IcePickController.OnTransfer` and `HandInteractionController.PlaceInto`; `CurrentHoveredSlot` setter visibility stays private from Task 9 onward.
- **Placeholder scan:** no TBDs. Every code block is compilable. Every wiring step names exact fields.
- **Risk callouts preserved:** Task 8 explicitly handles the cross-file compile ordering problem (stub then flesh out). Task 6 step 5 uses Unity MCP `execute_code` instead of inspector buttons. Task 10 step 5 includes three common failure-mode diagnostics.

---

## What comes after P0

Not part of this plan — shipped in later plans:
- P1 gadget behaviors (each gadget's own `OnTriggerPressedWhileHeld`).
- Supply cache pickup (writes to belt at runtime via `BeltSlot.Place`).
- Respawn persistence (serializer for `BeltSlot.HeldItem` prefab IDs).
- Polish: outline shader, haptics, SFX, holster tween.
