# My Unity Learning Curve

A running log of what I actually learned building and modifying this project: written
from real problems I hit, not a textbook. Most of these lessons came from one
"simple" task: **swapping the warrior's axe for a Viking sword.** That one job
touched almost every core Unity concept.

---

## The shape of the curve

```
Confidence
  ^
  |          "it's just drag-and-drop"            ____ "okay, I actually get it now"
  |         /\                                    /
  |        /  \                                  /
  |       /    \      "why won't it RENDER?!"   /
  |      /      \________   ___________________/
  |     /                \ /
  |____/                  V  (the despair valley)
  +-------------------------------------------------> Time
       Day 1     The struggle        The breakthrough
```

The lesson of the curve itself: **Unity feels easy for 10 minutes, then humbling for
a while, then it clicks.** The humbling part is where the real learning is.

---

## Stage 1: The editor is a 3D world, not a code file

What I learned first:

- The **Scene view** is for editing; the **Game view** is what the player sees. They
  can show completely different things (different camera, different angle).
- **Play mode is a sandbox.** Any change you make while the game is playing is
  **thrown away** when you stop. I lost edits more than once before this sank in.
- **Selecting** an object and pressing **F** frames it in the Scene view: but only if
  the mouse is hovering over the Scene view. Tiny detail, big time‑saver.

---

## Stage 2: GameObjects, Components, and the Inspector

- A **GameObject** is an empty container. It does nothing until you add **Components**
  (MeshFilter = the shape, MeshRenderer = makes it visible, Collider, scripts, etc.).
- The **Inspector** is just a live view of those components. Transform (Position /
  Rotation / Scale) is the one every object has.
- A mesh needs **all three**: a MeshFilter (which mesh), a MeshRenderer (enabled), and
  a **Material**: miss any one and you see nothing.

---

## Stage 3: Prefabs are templates, instances are copies

- A **prefab** is a reusable blueprint stored as an asset. Dragging it into the scene
  creates an **instance** linked back to the prefab.
- Editing one instance creates **overrides** (shown in the Inspector). Editing the
  prefab asset changes every instance.
- **Prefab isolation mode** ("Open" on the prefab) renders the object on a neutral
  background. This became my secret weapon: it proved the *asset was fine* even when
  the in‑scene copy refused to show up: which pointed the bug elsewhere.

---

## Stage 4: Parenting, bones, and local vs world space

This is where it got real.

- Child transforms are **relative to their parent**. `localPosition (0,0,0)` means
  "exactly at the parent's origin," *not* world origin.
- A character is a **skeleton of bones** (nested GameObjects). To put a weapon in a
  hand, you parent it to the **hand bone**, and it follows the animation for free.
- **World‑space orientation breaks under animation.** I first rotated the sword to
  point "down" in world space: perfect in the bind pose, totally wrong once the
  idle animation moved the arm. The fix: orient in the **hand's local space**, so the
  blade keeps the right angle in *every* animation frame.

> 💡 Biggest mental shift: a held weapon's transform is meaningless in isolation : 
> it only makes sense **relative to the bone it hangs from.**

---

## Stage 5: The bug that taught me the most: "it won't render"

The sword had a valid mesh, an enabled renderer, a material, and correct bounds…
and was **completely invisible** in the scene and game: but visible in prefab
isolation. Chasing that taught me a stack of lessons:

1. **`activeInHierarchy` vs `activeSelf`.** An object can be "active" itself but still
   not render because a **parent up the chain is disabled**. The whole branch goes dark.
2. **Skinned meshes deform from bone *transforms*, not bone *active state*.** So a
   character's body can look perfectly fine on screen while its bone GameObjects are
   technically inactive: which is exactly why my weapon (a normal MeshRenderer
   parented to a bone) vanished while the warrior still showed.
3. **There were two warriors.** A *disabled* duplicate (`Warrior`) held the legacy
   skeleton and the axe the `PlayerController.weapon` field still pointed at, while
   the *active, visible* one (`Warrior (1)`) had a completely different skeleton with
   bones named `hand.r`. I'd been attaching the sword to the **wrong, disabled twin**
   the whole time.
4. **`Renderer.bounds` returns values even for objects that aren't drawing.** So
   "the bounds look right" does **not** mean "it's on screen." Don't trust a single
   signal.

> 💡 Debugging lesson: when something is invisible, **isolate the variable.** I dropped
> a bright unlit‑magenta cube at the same spot: when *it* didn't show either, I knew
> the problem was the *location/parent*, not the sword mesh. That one test cracked it.

---

## Stage 6: Scale is a trap

- The visible warrior was imported at **100× scale**. Its bones carry a `lossyScale`
  of 100, so a sword at `localScale = 1` rendered as a **3–4 meter monster blade.**
- **`lossyScale`** = an object's true world scale after all parent scales multiply
  together. To get a normal‑sized sword I had to set `localScale = 1 / parentLossyScale`
  (i.e. `0.01`) to cancel the 100×.
- Lesson: **always check the import scale** of a model. Mismatched scales are one of
  the most common "why is everything huge/tiny" beginner traps.

---

## Stage 7: Editor scripting is a superpower

- A C# file in an **`Assets/Editor/`** folder can add menu items with `[MenuItem("Tools/...")]`.
- When the GUI fought me (drag‑and‑drop misfiring, copy‑paste renaming objects), a
  **one‑click editor tool** that does the work in code: `PrefabUtility.InstantiatePrefab`,
  `SetParent`, set transform: was deterministic and repeatable.
- I learned to **make the editor print what I needed**: `Debug.Log` of positions,
  active states, bounds, parent chains. Half of solving the invisible‑sword bug was
  just writing small diagnostic commands (`Map Warriors`, `Trace Active Chain`,
  `List Warrior1 Hand Bones`).

> 💡 If you find yourself doing the same fiddly thing in the Inspector more than twice,
> write an editor command for it.

---

## Stage 8: Workflow lessons (the unglamorous but vital ones)

- **Scripts must recompile before menu items appear.** Saving the file isn't always
  enough: forcing an asset refresh (Cmd+R) reliably triggers the compile.
- **iCloud + Unity = conflict copies.** Editing project files on disk while the editor
  is open spawned `filename 2.cs` duplicates that broke the build. Now I check for
  ` 2.cs` files after every external edit.
- **Read the Console, but triage it.** Some errors are harmless and constant (a
  missing `AudioSource` on a zombie, ambisonic‑audio warnings). Learning which
  messages to ignore vs act on is its own skill.

---

## The meta‑lessons

1. **A "simple" task is a great teacher.** "Put a sword in his hand" forced me through
   prefabs, parenting, bones, local/world space, active state, scale, materials,
   culling, animation, and editor scripting.
2. **Trust data over assumptions.** "It should be there" cost me hours; one diagnostic
   log (`activeInHierarchy=False`) ended the mystery instantly.
3. **Isolate one variable at a time.** The magenta cube and prefab‑isolation view
   each removed an entire class of possible causes.
4. **The Scene is a graph, not a list.** Almost every hard bug traced back to *where*
   an object sat in the hierarchy: its parent, its scale, its active branch.

---

## Cheat‑sheet I wish I'd had on day one

| Symptom | Likely cause |
| --- | --- |
| Object invisible but components look fine | A **parent is disabled** (`activeInHierarchy = false`) |
| Object huge or microscopic | **Import scale** / parent `lossyScale` mismatch |
| Weapon right in bind pose, wrong when animating | Oriented in **world space** instead of **bone‑local** space |
| Renders in prefab mode but not in scene | Difference is the **scene instance** (active state, parent, scale) |
| Mesh shows nothing at all | Missing **Material**, disabled **MeshRenderer**, or empty **MeshFilter** |
| Edits disappear | You changed them in **Play mode** |
| New `[MenuItem]` doesn't appear | Scripts haven't **recompiled** yet |

---

*Still climbing the curve: but now I know which way is up.*
