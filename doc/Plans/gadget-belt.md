# Gadget Belt — Design Spec

**Status:** P0 design approved, ready to plan
**Date:** 2026-04-17
**Branch:** `GadgetBelt`
**Related plan:** `doc/Plans/icepicks.md` (ice pick system — already implemented)

---

## 1. Context

IcePEAK is a VR ice-climbing game for Quest 3. The design calls for a *diegetic*
gadget system: every gadget is a physical object the player grabs from a harness
on their body. No menus, no HUD, no inventory screens.

Gadgets referenced in the design doc:
- Grappling hook (held, single-use)
- Cold spray (held, modifies ice crack timer)
- Poles (held, anchor to rock)
- Helmet (worn, not held — out of scope for this system)

Inventory is acquired from supply caches mid-run and persists across respawns.
Individual gadget behaviors are out of scope for this spec — this is the
holstering/inventory foundation that those systems will plug into.

## 2. Goal & scope

**Goal:** A foundation belt/holster system that future gadget-specific systems
(cold spray's crack-timer modifier, grapple's projectile, pole's anchor spawn)
can plug into without belt changes.

### P0 — this spec

- A visible 4-slot belt parented to the XR rig, rotating with the player's
  head yaw.
- Each hand can hover over a slot, see a debug visualization of the hand→slot
  distance checks, and press trigger to swap / stow / draw with that slot.
- Trigger resolution is priority-ordered: **embed-release → belt interaction →
  no-op**.
- Inspector-configurable initial loadout for staging test scenes.
- Only change to existing code: one method and one interface implementation
  added to `IcePickController`.

### P1 — later (waits on other systems)

- Supply cache pickup (adds items to the belt at runtime). Hook on belt stays
  unused in P0 but is designed-in.
- Inventory persistence across respawn (depends on `PlayerFallHandler` per
  `icepicks.md §9`).
- Distinct gadget prefabs (grapple, spray, poles) — each a `Holdable` with its
  own `OnTriggerPressedWhileHeld()` hook (interface added in P1).
- Outline shader for highlight instead of emissive swap.
- Haptics (via `HapticImpulsePlayer`) + SFX on swap/stow/draw.

### P2 — polish / separate

- Helmet — worn equipment, different interaction model (auto-equip when hand
  passes over head). Not a belt slot.
- Holster snap/tween animations instead of hard-snap teleport.
- Belt fade-in when looking down.
- Physical "empty slot" icons (silhouette of the gadget that goes there).

## 3. Architecture

### 3.1 Scripts (all in `Assets/IcePEAK/Scripts/Gadgets/`)

| Script | Purpose |
|---|---|
| `GadgetBelt` | Belt positioning (LateUpdate yaw sync) + nearest-slot lookup. Owns the 4 `BeltSlot`s. |
| `BeltSlot` | Single slot. MonoBehaviour on a child GameObject of the belt; the GameObject's own transform is the slot anchor. Holds one `HeldItem`. `SetHighlighted(bool)`. |
| `HandCell` | "Hand holds one item" cell. MonoBehaviour on a child GameObject of the hand controller; the GameObject's own transform is the anchor where held items parent. Same shape as `BeltSlot`. |
| `HandInteractionController` | Per-hand. Serialized references: this hand's `HandCell`, this hand's `IcePickController` (for `IsEmbedded` check), the `GadgetBelt`, and the XRI `Activate Value` trigger action. Reads trigger, runs priority resolution, tracks hovered slot, performs swap/stow/draw. |
| `Holdable` (interface `IHoldable`) | Marker on items. `OnTransfer(CellKind from, CellKind to)` hook. |
| `HandInteractionDebugVisualizer` | Sibling of `HandInteractionController`. Per-frame LineRenderers from hand to slots. |

### 3.2 Runtime object graph

```
XR Origin (XR Rig) ←──────── ClimbingLocomotion moves this
├── Camera Offset (y=1.36, existing)
│   ├── Main Camera (HMD) ←── read yaw each LateUpdate
│   ├── Left Controller ←──── existing
│   │   ├── IcePickLeft ←──── existing (pick parented here at start)
│   │   ├── HandCell (new, anchor transform)
│   │   ├── HandInteractionController (new)
│   │   └── HandInteractionDebugVisualizer (new)
│   └── Right Controller (same mirror)
└── GadgetBelt (new prefab, child of rig root — NOT Camera Offset)
    ├── Slot_FrontLeft  (anchor + wireframe ring placeholder)
    ├── Slot_SideLeft
    ├── Slot_SideRight
    └── Slot_FrontRight
```

**Why `GadgetBelt` is parented to the rig root, not Camera Offset:** The rig
root sits at the player's feet; Camera Offset has a `y=1.36m` offset for
eye-level. Parenting at the rig root lets us use a simple local `(0, waistHeight, 0)`
(≈ 1.0 m) and stay at waist level. Child of Camera Offset would shift the
belt head-ward.

### 3.3 Belt positioning math

`GadgetBelt.LateUpdate()`:

```csharp
// Local position is set once in the prefab — never touched.
// Only rotation updates each frame: world-space yaw from HMD.
transform.rotation = Quaternion.Euler(0f, _hmd.eulerAngles.y, 0f);
```

- Parent: `XR Origin (XR Rig)` root.
- Local position: `(0, waistHeight, 0)`; `waistHeight` serialized, default 1.0m.
- `_hmd`: serialized `Transform` pointing at `Main Camera`.
- **LateUpdate** ensures `TrackedPoseDriver` has already written the current
  frame's HMD pose before we sample yaw.

Result: belt at waist height above rig feet; always facing the player's body;
rises with climb (rig rises); unaffected by head pitch/roll.

### 3.4 Slot layout (placed manually in belt prefab)

Approximate local positions on the belt root:

| Slot | x | y | z |
|---|---:|---:|---:|
| `Slot_FrontLeft` | -0.12 | 0.00 | +0.09 |
| `Slot_SideLeft` | -0.18 | 0.00 | 0.00 |
| `Slot_SideRight` | +0.18 | 0.00 | 0.00 |
| `Slot_FrontRight` | +0.12 | 0.00 | +0.09 |

Back is intentionally empty — those positions are unreachable in VR without
twisting. Tune in the prefab inspector; exact numbers are not code constants.

Each slot has a small wireframe-ring placeholder mesh so empty slots are
locatable at a glance.

### 3.5 Initial loadout

Both `BeltSlot` and `HandCell` accept their starting contents two ways:

1. **Pre-parented in the scene.** If the cell's anchor has a child GameObject at
   `Awake`, that child is the initial `HeldItem`. This is how the existing
   `IcePickLeft` / `IcePickRight` plug in — we reparent them under their hand's
   `HandCell` anchor in the scene, and the cell picks them up automatically.
2. **Serialized prefab.** Each cell also exposes `[SerializeField] GameObject initialPrefab`.
   If the anchor has no child at `Awake` and `initialPrefab` is non-null, the
   cell instantiates it into the anchor (local zero). Useful for populating
   belt slots with placeholder gadget cubes without dragging instances into the
   scene.

Slots with neither a child nor an `initialPrefab` start empty.

## 4. Behavior rules

### 4.1 Proximity detection

`GadgetBelt.TryGetNearestSlot(Vector3 handWorldPos, out BeltSlot slot)`:

- Iterates all slots, takes min `Vector3.Distance`.
- Returns `true` only if `minDist <= proximityRadius` (serialized, default 0.15m).
- Pure math, no physics triggers. Deterministic.

### 4.2 Highlight

`HandInteractionController` tracks `_currentHoveredSlot`. Each `Update`:

```csharp
belt.TryGetNearestSlot(handCell.anchor.position, out var newHovered);
if (newHovered != _currentHoveredSlot) {
    _currentHoveredSlot?.SetHighlighted(false);
    _currentHoveredSlot = newHovered;
    _currentHoveredSlot?.SetHighlighted(true);
}
```

`BeltSlot.SetHighlighted(bool on)`:
- Slot full → toggle emissive on the held item's renderer(s).
- Slot empty → toggle emissive on the wireframe-ring placeholder.

### 4.3 Trigger priority resolution (per hand, each Update)

```csharp
void Update() {
    if (pick != null && pick.IsEmbedded) return;          // P1: pick owns its trigger
    if (!triggerAction.action.WasPressedThisFrame()) return; // rising edge
    if (_currentHoveredSlot == null) return;               // P3: no-op
    ResolveBeltAction(_currentHoveredSlot);                // P2
}
```

`IcePickController.Update` already early-returns when `!_isEmbedded`
(`IcePickController.cs:68`), so the pick's release logic and the belt
interaction are mutually exclusive by state — no double-fire.

`WasPressedThisFrame()` is Input System's rising-edge read; holding the
trigger doesn't re-fire.

### 4.4 Swap / stow / draw — unified cell transfer

All three operations collapse to one code path: snapshot both cells, empty both,
re-place the snapshots into their swapped cells. Draw and stow are just the
case where one snapshot was null.

```csharp
void ResolveBeltAction(BeltSlot slot) {
    var handItem = handCell.HeldItem;
    var slotItem = slot.HeldItem;
    if (handItem == null && slotItem == null) return;  // no-op

    handCell.Take();
    slot.Take();

    if (slotItem != null) PlaceInto(handCell, slotItem, from: CellKind.BeltSlot);
    if (handItem != null) PlaceInto(slot,     handItem, from: CellKind.Hand);
}

void PlaceInto(ICell cell, GameObject item, CellKind from) {
    item.transform.SetParent(cell.anchor, worldPositionStays: false);
    item.transform.localPosition = Vector3.zero;
    item.transform.localRotation = Quaternion.identity;
    cell.Place(item);
    item.GetComponent<IHoldable>()?.OnTransfer(from, cell.Kind);
}
```

`ICell` is implemented by `BeltSlot` and `HandCell`:

```csharp
public interface ICell {
    GameObject HeldItem { get; }
    Transform anchor { get; }      // this.transform on the MonoBehaviour's GameObject
    CellKind Kind { get; }         // Hand | BeltSlot
    void Place(GameObject item);
    GameObject Take();              // returns HeldItem, sets HeldItem = null
}
```

### 4.5 `IHoldable` contract

```csharp
public enum CellKind { Hand, BeltSlot }

public interface IHoldable {
    void OnTransfer(CellKind from, CellKind to);
}
```

Implemented by `IcePickController`:

```csharp
public void OnTransfer(CellKind from, CellKind to) {
    SetStowed(to == CellKind.BeltSlot);
}

public void SetStowed(bool stowed) {
    _tipCollider.enabled = !stowed;
    _swingDetector.enabled = !stowed;
}
```

P0 placeholder gadget: no-op `OnTransfer`.

### 4.6 Edge cases — explicit rules

| Situation | Behavior |
|---|---|
| Pick embedded + hand near belt + trigger | Priority 1 wins → pick releases. Belt inert. |
| Both cells empty + trigger | No-op. |
| Trigger held (not fresh press) | No-op. |
| Hand near two slots (defensive) | `TryGetNearestSlot` returns the nearer — deterministic. |
| Stowed pick's tip brushes ice | No embed — tip collider disabled by `SetStowed(true)`. |
| Draw stowed pick, swing into ice | Embeds normally — `SetStowed(false)` re-enables tip collider. |

## 5. Debug visualizer

`HandInteractionDebugVisualizer` (sibling of `HandInteractionController`):

Serialized:
- `bool enabled = true`
- `float approachRadius = 0.30f` (≈ 2 × `proximityRadius`)
- `Color activeColor` (bright green)
- `Color approachColor` (dim white)

`Awake`: spawn 4 child `LineRenderer` GameObjects (one per slot). Line renderers
show in **Game view** (needed for XR Simulator + Quest dev builds), not just
Scene gizmos.

Each `Update`:

```
for each slot in belt.slots:
    dist = Vector3.Distance(hand.position, slot.anchor.position);
    if (dist > approachRadius):
        line.enabled = false;
    else:
        line.enabled = true;
        line.SetPositions(hand.position, slot.anchor.position);
        if (slot == handController.CurrentHoveredSlot):
            line.color = activeColor;   line.width = 0.004f;  // trigger will fire here
        else:
            line.color = approachColor; line.width = 0.0015f; // in range but not nearest
```

Reads `HandInteractionController.CurrentHoveredSlot` (new public getter) so
"which slot would fire" logic is defined in one place.

**What you see in-editor:**
- Hand far from belt → no lines.
- Hand within ~30 cm of a slot → thin white line appears to that slot.
- Hand within ~15 cm and it's the nearest → thick green line. Trigger fires on
  that slot.

Doubles as the manual test mechanism for P0 — no unit tests needed to confirm
proximity & hover priority behave.

## 6. Integration with existing systems

### 6.1 `IcePickController`

- Add `public void SetStowed(bool stowed)` (see §4.5).
- Implement `IHoldable`.
- Embed/release logic untouched.
- `IsEmbedded` stays the authoritative "in ice" flag; read by
  `HandInteractionController`'s priority check.

### 6.2 `ClimbingLocomotion`

- **No changes.** The belt is a sibling under the rig; climbing moves the rig,
  belt follows passively (its local position is static relative to rig root).

### 6.3 Physics layers

No new layers needed. Belt visuals are renderers only; proximity is pure math,
no trigger colliders.

### 6.4 Input action maps

Reuse XRI default trigger (`XRI LeftHand Interaction/Activate Value`, same on
right hand). Shared with the pick's release trigger — no conflict because of
priority resolution + state-disjoint handling in `IcePickController.Update`.

## 7. Manual test plan (in `Assets/IcePEAK/Scenes/TestScene.unity`)

No headless runner configured — all checks are in-editor with XR Simulation or
on-device.

| # | Behavior | Verification |
|---|---|---|
| 1 | Belt follows rig vertically | Climb; belt rises with rig. |
| 2 | Belt takes HMD yaw | Turn head/body physically; belt rotates to match. |
| 3 | Belt doesn't bob | Look up/down; belt y stays constant. |
| 4 | Debug lines appear at approach | Move hand toward belt; line appears within `approachRadius`. |
| 5 | Nearest-slot highlight | Move hand between slots; highlight transfers; debug line on nearest turns thick green. |
| 6 | Stow pick to empty slot | Pick in hand, near empty slot, trigger → pick in slot, hand empty, tip collider disabled. |
| 7 | Draw from filled slot | Empty hand near filled slot, trigger → item in hand. |
| 8 | Swap pick ↔ gadget | Pick in hand, near slot with cube, trigger → pick in slot, cube in hand. |
| 9 | Priority: embed-release wins | Embed pick in ice, press trigger → pick releases (belt inert). |
| 10 | Priority: no-op in mid-air | Free hand, trigger, not near belt → nothing. |
| 11 | Stowed pick's tip is inert | Stow pick, drag past ice (scene manipulation) → no embed event. |
| 12 | Drawn pick's tip re-activates | Draw previously-stowed pick, swing into ice → embeds. |
| 13 | Inspector loadout respected | Set initial contents; at Play, slots reflect it. |
| 14 | Two-hand independent operation | Both hands stow/draw simultaneously; hovered slot tracked per hand. |

## 8. Implementation order

1. `GadgetBelt` skeleton + positioning. Temporarily give the belt root a visible
   primitive (e.g., a flat torus or thin cube) so the position is observable.
   Test: enter Play, climb / turn head — belt stays at waist, rotates with HMD yaw.
2. `BeltSlot` + 4 slot anchor GameObjects with wireframe-ring placeholders.
3. `HandCell` (trivial).
4. `IHoldable` + `IcePickController.SetStowed` + `OnTransfer`. Test: toggle
   manually, verify tip collider + swing detector disable.
5. `HandInteractionDebugVisualizer`. Test: lines appear/disappear with hand
   proximity.
6. `HandInteractionController` — proximity tracking + highlight (no input yet).
   Test: hover tracks hand, debug line turns green on nearest.
7. `HandInteractionController` — trigger priority + `ResolveBeltAction`. Test:
   all 14 manual cases.
8. Belt prefab + wiring in `TestScene`.

Estimated ~1 dev-day for the first working prototype with placeholder art.

## 9. Tunables

| Field | Default | Where | Notes |
|---|---:|---|---|
| `waistHeight` | 1.0 m | `GadgetBelt` | Local y above rig root (rig root is at the player's feet). |
| `proximityRadius` | 0.15 m | `GadgetBelt` | Slot hover range. |
| `approachRadius` | 0.30 m | `HandInteractionDebugVisualizer` | Debug line visibility range. |
| `triggerReleaseThreshold` | 0.5 | `IcePickController` (existing) | Unchanged. |

## 10. Open questions — none for P0

Deferred to P1:
- Supply cache pickup range / gesture.
- Respawn serialization format.
- Per-gadget "use" gesture when held in hand (grapple fire, spray nozzle, pole
  strike).
