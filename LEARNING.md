# My Unity Learning Curve

A running log of what I actually learned building Mistwalker, written from real
problems I hit rather than from a textbook. It started with one "simple" task:
**swapping the warrior's axe for a Viking sword.** That job alone touched almost
every core Unity concept. Everything after it (new characters, new enemies,
shipping the game to the web) taught the rest.

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

The lesson of the curve itself: **Unity feels easy for 10 minutes, then humbling
for a while, then it clicks.** The humbling part is where the real learning is.

---

## Part 1: The sword that wouldn't render

### Stage 1: The editor is a 3D world, not a code file

- The **Scene view** is for editing. The **Game view** is what the player sees. They
  can show completely different things (different camera, different angle).
- **Play mode is a sandbox.** Any change you make while the game is playing is
  **thrown away** when you stop. I lost edits more than once before this sank in.
- **Selecting** an object and pressing **F** frames it in the Scene view, but only if
  the mouse is hovering over the Scene view. Tiny detail, big time-saver.

### Stage 2: GameObjects, Components, and the Inspector

- A **GameObject** is an empty container. It does nothing until you add **Components**
  (MeshFilter = the shape, MeshRenderer = makes it visible, Collider, scripts, etc.).
- The **Inspector** is a live view of those components. Transform (Position /
  Rotation / Scale) is the one every object has.
- A visible mesh needs **all three**: a MeshFilter (which mesh), an enabled
  MeshRenderer, and a **Material**. Miss any one and you see nothing.

### Stage 3: Prefabs are templates, instances are copies

- A **prefab** is a reusable blueprint stored as an asset. Dragging it into the scene
  creates an **instance** linked back to the prefab.
- Editing one instance creates **overrides** (shown in the Inspector). Editing the
  prefab asset changes every instance.
- **Prefab isolation mode** ("Open" on the prefab) renders the object on a neutral
  background. It became my secret weapon: it proved the *asset was fine* even when
  the in-scene copy refused to show up, which pointed the bug elsewhere.

### Stage 4: Parenting, bones, and local vs world space

- Child transforms are **relative to their parent**. `localPosition (0,0,0)` means
  "exactly at the parent's origin," *not* the world origin.
- A character is a **skeleton of bones** (nested GameObjects). To put a weapon in a
  hand, you parent it to the **hand bone**, and it follows the animation for free.
- **World-space orientation breaks under animation.** I first rotated the sword to
  point "down" in world space. It was perfect in the bind pose and totally wrong once
  the idle animation moved the arm. The fix: orient in the **hand's local space**, so
  the blade keeps the right angle in *every* animation frame.

> 💡 Biggest mental shift: a held weapon's transform means nothing in isolation.
> It only makes sense **relative to the bone it hangs from.**

### Stage 5: The bug that taught me the most

The sword had a valid mesh, an enabled renderer, a material, and correct bounds, and
was **completely invisible** in the scene and game, yet visible in prefab isolation.
Chasing that taught me a stack of lessons:

1. **`activeInHierarchy` vs `activeSelf`.** An object can be "active" itself but still
   not render because a **parent up the chain is disabled**. The whole branch goes dark.
2. **Skinned meshes deform from bone *transforms*, not bone *active state*.** A
   character's body can look perfectly fine while its bone GameObjects are inactive.
   That's exactly why my weapon (a normal MeshRenderer parented to a bone) vanished
   while the warrior still showed.
3. **There were two warriors.** A *disabled* duplicate (`Warrior`) held the legacy
   skeleton and the axe that `VikingChampion.weapon` still pointed at, while the
   *active, visible* one (`Warrior (1)`) had a different skeleton with bones named
   `hand.r`. I'd been attaching the sword to the **wrong, disabled twin** the whole time.
4. **`Renderer.bounds` returns values even for objects that aren't drawing.** "The
   bounds look right" does **not** mean "it's on screen." Don't trust a single signal.

> 💡 Debugging lesson: when something is invisible, **isolate the variable.** I dropped
> a bright unlit-magenta cube at the same spot. When *it* didn't show either, I knew
> the problem was the *location/parent*, not the sword mesh. That one test cracked it.

### Stage 6: Scale is a trap

- The visible warrior was imported at **100× scale**. Its bones carry a `lossyScale`
  of 100, so a sword at `localScale = 1` rendered as a **3–4 meter monster blade.**
- **`lossyScale`** is an object's true world scale after all parent scales multiply
  together. To get a normal-sized sword I set `localScale = 1 / parentLossyScale`
  (i.e. `0.01`) to cancel the 100×.
- Lesson: **always check a model's import scale.** Mismatched scales are one of the
  most common "why is everything huge/tiny" beginner traps.

### Stage 7: Editor scripting is a superpower

- A C# file in an **`Assets/Editor/`** folder can add menu items with
  `[MenuItem("Tools/...")]`.
- When the GUI fought me (drag-and-drop misfiring, copy-paste renaming objects), a
  **one-click editor tool** that does the work in code (`PrefabUtility.InstantiatePrefab`,
  `SetParent`, set the transform) was deterministic and repeatable.
- I learned to **make the editor print what I needed**: positions, active states,
  bounds, parent chains. Half of solving the invisible-sword bug was writing small
  diagnostic commands (`Map Warriors`, `Trace Active Chain`, `List Warrior1 Hand Bones`).
- Later, one-off editor scripts did whole jobs in one click: swapping the player model,
  building an enemy prefab, painting the terrain. Run it once, check the result,
  delete the script.

> 💡 If you find yourself doing the same fiddly thing in the Inspector more than twice,
> write an editor command for it.

### Stage 8: Workflow lessons (the unglamorous but vital ones)

- **Scripts must recompile before menu items appear.** Saving the file isn't always
  enough; forcing an asset refresh (Cmd+R) reliably triggers the compile.
- **Read the Console, but triage it.** Some messages are harmless and constant
  (deprecation warnings, audio-device notices). Learning which to ignore vs act on is
  its own skill.
- **Save the scene (⌘S).** The sword once "disappeared" from the game because I'd
  attached it and never saved. Unity doesn't autosave scenes.

---

## Part 2: Growing the game

### Stage 9: Render pipelines, and why things turn pink

- Unity has three render pipelines: **Built-In**, **URP**, and **HDRP**. Mistwalker uses
  Built-In. A material whose shader belongs to another pipeline can't be drawn, and
  Unity paints it **bright magenta/purple** instead.
- The wolf came out purple because its fur used a **URP/HDRP-only Shader Graph**, even
  inside the pack's "Built-In" folder. The body used the standard shader and was fine,
  so the fix was hiding the two fur layers, not replacing the whole model.
- **Check the listing before downloading.** On the Asset Store, look for the render
  pipeline table (Built-in: ✅) and the supported Unity versions. A free Terrain pack
  I wanted required a newer Unity than mine, and Unity's own Viking Village is
  URP-only, which would have meant converting the whole project.

> 💡 Purple = "I can't run this shader here." Find which *material* is on the purple
> part, then check which pipeline its shader targets.

### Stage 10: Swapping characters: Humanoid vs Generic rigs

- A **Humanoid** rig maps bones to a standard human skeleton (an **Avatar**), so
  animations can move between characters. Swapping the old warrior for the red-haired
  Viking kept all the walk/run/slash animations because both were Humanoid.
- A **Generic** rig (the Creep, the wolf) has its own skeleton, and humanoid clips don't
  apply. Each needed its own **Animator Controller** built from its own clips.
- The glue between code and animation is **parameter names**. `HostileWarrior` only
  fires two triggers, `IsAttacking` and `IsDead`, so any creature works as an enemy as
  long as its controller answers to those two names.
- When you replace a character, everything that pointed at the old one (camera follow,
  health bar, ground check) must be **re-pointed**. Missing one reference means a
  silently broken feature.

### Stage 11: Hold the sword by the handle

- A mesh's **pivot** isn't always where you'd hold it. The sword's pivot sat at the
  *blade* end, so attaching it to the hand made the Viking hold the blade (剑刃), not
  the hilt (剑柄).
- The fix was measuring instead of guessing: find the blade's long axis from the mesh
  **bounds**, work out which end is the hilt, flip the sword 180°, and slide it so the
  grip point lands in the hand.
- Then **check it animated**, not just in the rest pose. A weapon can look right in
  T-pose and wrong mid-swing.

### Stage 12: Making combat feel fair

- The first attack hit everything inside a sphere, including enemies **behind** me.
  Real-feeling combat needs three checks:
  1. **Facing:** only hit targets inside an arc in front (`Vector3.Angle` against
     `transform.forward`).
  2. **Line of sight:** a `Physics.Linecast` from chest to target; if a tree is in
     the way, no hit.
  3. **Blocked swing:** a short `SphereCast` forward; if a trunk is within blade reach,
     don't swing at all.
- **Players need a way back.** With no healing, every hit was permanent. Heal-on-kill
  plus out-of-combat regeneration made fights winnable and rewarded good play.
- **Death needs an ending.** Without a "You have fallen / press R" screen the game just
  froze. `PlayerPrefs` keeps a best score between plays.
- **`DontDestroyOnLoad` bites on restart.** The score keeper survived the scene reload
  but kept pointing at the *old* scene's text objects. For a one-scene game, let it
  reload with the scene.
- **Unity's fake null:** `GetComponent<T>() ?? AddComponent<T>()` doesn't work on Unity
  objects. Use an explicit `if (x == null)` check.

### Stage 13: Atmosphere is mostly settings

- **Fog** is a Lighting setting, but the **skybox ignores fog**, so the horizon showed a
  hard line. Setting the camera background to the fog colour made the distance melt
  into mist.
- A close, downward camera needs **thicker fog** than you'd guess to feel misty.
- The "checkerboard" ground was the terrain's **missing texture layers**, not a design
  choice. One free CC0 texture (Poly Haven) painted as a Terrain Layer fixed it.
- Small touches carry a lot: a flickering lantern light, a campfire with sound only
  nearby (3D audio), music that loops quietly.

---

## Part 3: Shipping it

### Stage 14: Continuous builds and the web version

- **GitHub Actions + GameCI** builds the game on every push. The old `.NET` workflow
  could never compile a Unity project; Unity builds need the Unity Editor.
- **Licensing is its own puzzle.** My school account signs in through USC, so it has no
  password, and CI can't log in with it. A separate free Personal account worked. The
  license file from Unity Hub is tied to my Mac, so CI falls back to signing in with
  that account's email and password.
- **CI catches what you can't see.** A find-and-replace in a "docs" commit turned
  `a - b` into `a: b` inside a C# file. Unity wasn't installed at the time, so only the
  CI build noticed.
- **WebGL on GitHub Pages** needed two settings: the scene listed in Build Settings
  (otherwise the build is empty) and *decompression fallback* on (Pages can't serve
  pre-compressed files the way the browser expects).
- Browsers **block sound until the first click or key press**. That's normal, not a bug.

### Stage 15: Repository hygiene

- **GitHub rejects files over 100 MB.** The Viking model is 115 MB, so it lives in
  **Git LFS**, and CI has to check out with `lfs: true`.
- **Assets pile up.** Asset packs bring demo scenes, colour variants, and other
  pipelines' versions. A dependency trace from the scene found **624 MB** nothing used.
- **Anything in a `Resources` folder ships in every build,** used or not. That makes
  extras folders worth deleting.
- **Convert big media.** A 40 MB WAV became a 4 MB MP3 with no audible difference.
- Deleted files stay in **git history**, so cleaning up is safe to undo.

---

## The meta-lessons

1. **A "simple" task is a great teacher.** "Put a sword in his hand" forced me through
   prefabs, parenting, bones, local/world space, active state, scale, materials,
   culling, animation, and editor scripting.
2. **Trust data over assumptions.** "It should be there" cost me hours; one diagnostic
   log (`activeInHierarchy=False`) ended the mystery instantly.
3. **Isolate one variable at a time.** The magenta cube and prefab isolation view each
   removed a whole class of possible causes.
4. **The Scene is a graph, not a list.** Almost every hard bug traced back to *where*
   an object sat in the hierarchy: its parent, its scale, its active branch.
5. **Check it running, not just placed.** The sword grip, the fog, the wolf's colours:
   each looked fine in one view and wrong in another. The Game view in Play mode is
   the only one that counts.

---

## Cheat-sheet I wish I'd had on day one

| Symptom | Likely cause |
| --- | --- |
| Object invisible but components look fine | A **parent is disabled** (`activeInHierarchy = false`) |
| Object huge or microscopic | **Import scale** / parent `lossyScale` mismatch |
| Weapon right in bind pose, wrong when animating | Oriented in **world space** instead of **bone-local** space |
| Character holds the weapon by the wrong end | The mesh **pivot** is at the other end; flip and offset to the grip |
| Renders in prefab mode but not in scene | Difference is the **scene instance** (active state, parent, scale) |
| Mesh shows nothing at all | Missing **Material**, disabled **MeshRenderer**, or empty **MeshFilter** |
| Bright pink / purple object | Shader from **another render pipeline** (URP/HDRP on Built-In) |
| Checkerboard ground | Terrain has **missing Terrain Layers** |
| Hard line where fog meets sky | **Skybox isn't fogged**; use a solid camera background in the fog colour |
| New character won't animate | **Generic** rig needs its own controller; Humanoid needs an **Avatar** |
| Edits disappear | You changed them in **Play mode**, or didn't **save the scene** |
| New `[MenuItem]` doesn't appear | Scripts haven't **recompiled** yet |
| HUD breaks after restarting | An object survived the reload (`DontDestroyOnLoad`) with stale references |
| `git push` rejected for a big file | Over **100 MB**; track it with **Git LFS** |
| Web build is empty or won't load | No scene in **Build Settings**, or compression without **decompression fallback** |

---

*Still climbing the curve, but now I know which way is up.*
