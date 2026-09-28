# MISTWALKER: A Saga of the Fog

> *A fallen viking wakes in Niflheim, the world of mist. The dead walk the frozen
> forest, and the only road to Valhalla is paved with the draugr you put back in
> the ground. Carry your blade. Hold your lantern. Do not stop walking.*

**Mistwalker** is a third-person survival-horror prototype built in **Unity 6
(6000.2.12f1)** on the Built-In render pipeline. You play a red-haired Viking
warrior (sword and shield) walking an endless, fog-covered forest while
waves of draugr (undead) ambush you. It started as a generic zombie arena and
is being reworked into a Norse-underworld journey.

![Mistwalker gameplay in Unity](docs/images/mistwalker-gameplay.png)

**▶ Play in your browser:** https://wuisabel-gif.github.io/Mistwalker/

---

## Contents

- [Quick Start](#quick-start)
- [Controls](#controls)
- [Architecture](#architecture)
- [Gameplay Systems](#gameplay-systems)
- [Tuning Reference](#tuning-reference)
- [Editor Tooling](#editor-tooling)
- [Project Layout](#project-layout)
- [Known Issues](#known-issues)
- [Roadmap](#roadmap)
- [Credits](#credits)

---

## Quick Start

**Requirements**

| | |
| --- | --- |
| Unity | 6000.2.12f1 (any 6000.2.x should work) |
| Render pipeline | Built-In |

**Run it**

1. Install Unity 6000.2.x through Unity Hub.
2. Install [Git LFS](https://git-lfs.com) (the player model is stored with it), then `git clone` this repo and add the folder in Unity Hub (**Add → Add project from disk**).
3. Let the first import finish. It can take a few minutes.
4. Open `Assets/Scenes/SampleScene.unity` and press **Play**.

**CI:** `.github/workflows/unity.yml` uses [GameCI](https://game.ci) to build the
WebGL player on every push and pull request, which also checks that the project
compiles. Pushes to `main` deploy it to GitHub Pages.

**Package dependencies** (from `Packages/manifest.json`)

| Package | Version | Used for |
| --- | --- | --- |
| `com.unity.inputsystem` | 1.14.2 | Player movement, run, jump, slash |
| `com.unity.cinemachine` | 3.1.7 | Third-person follow camera |
| `com.unity.ai.navigation` | 2.0.13 | Installed, not used by gameplay yet |
| `com.unity.timeline` | 1.8.9 | Installed, not used by gameplay yet |
| `com.unity.test-framework` | 1.6.0 | No tests yet |

`Active Input Handling` is set to **Both**. The project needs this because some
scripts use the new Input System and others use the legacy `UnityEngine.Input`
API (see [Known Issues](#known-issues)).

---

## Controls

Bindings live in `Assets/PlayerInput.inputactions` (action map `CharacterControls`).
The C# wrapper `Assets/Scripts/PlayerInput.cs` is auto-generated from it. Don't
edit it by hand.

| Action | Keyboard / Mouse | Gamepad |
| --- | --- | --- |
| Move | `W` `A` `S` `D` | Left Stick |
| Run (hold) | `Left Shift` | Left Shoulder |
| Jump | `Space` | South button (A / Cross) |
| Basic Slash | Left Mouse | West button (X / Square) |

---

## Architecture

The game runs on plain `MonoBehaviour` components wired in the scene. There is
no central game manager. The one exception is `JourneyLedger`, a
`DontDestroyOnLoad` singleton that holds score.

```
             ┌──────────────────────── Player ("Player" tag) ─────────────────────────┐
 Input  ───► │ HeroMotionDriver ──► CharacterController + Animator (walk/run/jump/slash) │
 System      │        ▲                                                                   │
             │ FootingProbe (ground raycast)                                              │
             │                                                                            │
 Legacy ───► │ VikingChampion: health, sword strike (OverlapSphere), death                │
 Input       └───────────┬──────────────────────────────▲─────────────────────────────────┘
                         │ ReceiveHit(dmg)              │ ReceiveDamage(dmg)
                         ▼                              │
            HostileWarrior ("Enemy" tag): chase, melee, die ──► JourneyLedger.AwardEnemyDefeat(125)
                         ▲                                              │
            AmbushTrigger: spawns HostileWarrior groups,               ├──► score / kill HUD (UI Text)
                           size scales with score  ◄── GetScore() ─────┘
                                                                        
            VitalityDisplay ◄── reads VikingChampion.currentHealth every frame (health bar)
            PathTileCycler  ◄── reads player.z, recycles terrain tiles (endless forest)
            csFogWar (AOS Fog of War asset): fog-of-war plane around the player
```

### Script reference (`Assets/Scripts/`)

| Script | In scene | Role |
| --- | --- | --- |
| `HeroMotionDriver.cs` | ✅ | Main locomotion. Reads the `CharacterControls` action map, drives `CharacterController.Move`, turns toward the move direction with `Quaternion.Slerp`, sets animator params `IsWalking`, `IsRunning`, `isJumping`, `isBasicSlashingTrigger`. Locks movement during the slash until `BasicSlash` reaches 85% normalized time. |
| `FootingProbe.cs` | ✅ | Downward `Physics.Raycast` (0.45 m, filtered by `groundMask`) exposing `isGrounded`. |
| `VikingChampion.cs` | ✅ | Player health (120), sword strike, death. On left click it does `Physics.OverlapSphere` at the weapon position and calls `HostileWarrior.ReceiveHit` on every collider tagged `Enemy`. Heals +20 HP each time score passes a multiple of 900. |
| `HostileWarrior.cs` | via `Draugr.prefab` | Enemy AI. Moves straight at the player with `Vector3.MoveTowards` (no pathfinding). When in range it attacks on a cooldown and calls `VikingChampion.ReceiveDamage`. On death it awards 125 points and is destroyed after 2.5 s. |
| `AmbushTrigger.cs` | ✅ ×5 | One-shot trigger volume. When the player enters, it spawns `baseGroupSize + clamp(score / 500, 0, 8)` enemies at random points inside `spawnRadius`. |
| `JourneyLedger.cs` | ✅ | Singleton for score and kill count. Updates the HUD `Text` and flashes the score color on each kill. |
| `VitalityDisplay.cs` | ✅ | Health bar. Sets `Image.fillAmount` and color: green above 65%, amber above 35%, red otherwise. |
| `PathTileCycler.cs` | ✅ | Endless-terrain treadmill. Keeps a ring buffer of `Terrain` tiles along +Z. When the player gets `bufferTiles × tileLength` past the rear tile, that tile moves to the front (and the reverse when walking back). |
| `BlendTreeMotionDriver.cs` | — | Experimental 2D blend-tree driver (`VelocityX` / `VelocityZ`). Not used. |
| `SimpleMotionAnimator.cs` | — | Experimental 1D blend-tree driver (`Velocity`). Not used. |
| `PlayerInput.cs` | — | Generated Input System wrapper. |

---

## Gameplay Systems

### Combat loop

1. The player walks into an `AmbushTrigger` collider, which fires once per trigger.
2. A group of `HostileWarrior`s spawns. Group size grows by 1 for every 500
   points, up to +8.
3. Each enemy chases the player and hits for 12 damage every 1.25 s once within
   1.6 m.
4. The player's slash does 9 damage in a 2.2 m sphere and has a 1.6 s cooldown.
   An enemy with 45 HP takes 5 hits.
5. Each kill gives +125 score. Every 900 score heals the player +20 HP.

### Jump physics

`HeroMotionDriver` computes gravity and initial velocity from the jump height
and duration you want:

```
timeToApex          = maxJumpTime / 2
gravity             = -2 · maxJumpHeight / timeToApex²
initialJumpVelocity =  2 · maxJumpHeight / timeToApex
```

Gravity is doubled on the way down (`fallMultiplier = 2`) so the fall feels
snappier. Velocity is integrated with a velocity-Verlet style average
`(v_prev + v_new) / 2`, which keeps jump arcs more consistent across frame rates.

### Endless forest

`PathTileCycler` recycles a fixed pool of terrain tiles instead of streaming
new ones, so memory use stays flat no matter how far you walk. The pool must be
in travel order in the `tiles` array, and each tile must be exactly
`tileLength` long on Z.

### Fog of war

This uses the third-party **AOS Fog of War** asset
(`Assets/Downloaded Assets/AOSFogWar/csFogWar.cs`). See the PDF in that folder
for its configuration.

---

## Tuning Reference

These are the script defaults. Scene or prefab overrides take precedence in the
Inspector.

| Component | Field | Default |
| --- | --- | --- |
| `VikingChampion` | `maxHealth` / `strikePower` / `strikeRadius` / `strikeDelay` | 120 / 9 / 2.2 m / 1.6 s |
| `HostileWarrior` | `health` / `pursuitSpeed` / `meleeDistance` / `hitStrength` / `attackInterval` | 45 / 2.35 m/s / 1.6 m / 12 / 1.25 s |
| `AmbushTrigger` | `baseGroupSize` / `spawnRadius` | 2 / 14 m |
| `HeroMotionDriver` | `walkSpeed` / `runMultiplier` / `maxJumpHeight` / `maxJumpTime` | 1.0 / 3.0× / 1.0 m / 0.5 s |
| `PathTileCycler` | `tileLength` / `bufferTiles` | 100 m / 2 |

---

## Editor Tooling

`Assets/Editor/SwordAttacher.cs` adds a **Tools** menu for fitting the sword to
the warrior rig.
The rig is awkward: the playable `Warrior (1)` is imported at
**100× scale** with a `hand.r` bone, and a disabled duplicate `Warrior` still
carries the legacy `Warrior_RightHand` skeleton.

| Menu item | What it does |
| --- | --- |
| **Attach To Active Hand** | Instantiates or re-parents the sword under the active `hand.r` bone. |
| **Normalize Sword Scale** | Cancels the 100× bone scale so the blade is about 0.8 m. |
| **Sword Local Rot +X90 / +Y90 / +Z90** | Rotates the blade's local orientation in 90° steps. |
| **Match Axe (…)** | Copies the original axe's local transform, with optional spin. |
| **Map Warriors / List Warrior1 Hand Bones / Trace Active Chain** | Diagnostics for the two-skeleton setup. |
| **Report Sword Size / Diagnose Placement** | Logs world-space bounds and parent chain. |
| **Highlight Sword / Test Cube At Hand** | Visual debugging aids. |

This is editor-only code under an `Editor/` folder, so it is never included in
player builds.

---

## Project Layout

```
Assets/
├─ Scenes/SampleScene.unity      # the playable scene + 8 TerrainData tiles
├─ Scripts/                      # gameplay code (see Script reference)
├─ Editor/SwordAttacher.cs       # Tools menu for sword rigging
├─ Animation/                    # humanoid clips, BotController / RetargetController
├─ PlayerInput.inputactions      # Input System bindings
├─ Agarkova_CG/                 # red-haired Viking model (Warrior.fbx stored with Git LFS)
├─ Medieval Viking Sword/        # sword model (in the Viking's right hand)
├─ Creep Horror Creature/        # draugr enemy (Prefabs/Draugr.prefab, DraugrController)
└─ Downloaded Assets/            # third-party: AOSFogWar, Warrior Model (shield),
                                 #   Dry_Trees, RockFREE, Fantasy Skybox
Packages/manifest.json           # package versions
ProjectSettings/                 # Unity project settings (editor version pinned here)
```

---

## Known Issues

- **Two movement paths on the player.** `HeroMotionDriver` moves the
  `CharacterController` through the Input System. `VikingChampion` also calls
  `transform.Translate` from legacy `Input.GetAxis`. With both enabled, WASD
  moves the player twice.
- **Attack isn't tied to the animation.** `VikingChampion` applies damage right
  away on mouse down. It is not driven by an animation event, and its cooldown
  is separate from `HeroMotionDriver`'s slash lock.
- **Enemies ignore obstacles.** `HostileWarrior` moves in a straight line.
  `com.unity.ai.navigation` is installed but no NavMesh is baked.
- **`JourneyLedger` persists across scene loads** (`DontDestroyOnLoad`).
  Call `ResetLedger()` when a new run starts.

---

## Roadmap

- [ ] Merge player movement into `HeroMotionDriver`; keep `VikingChampion` for health and combat only
- [ ] Apply damage from an animation event on the `BasicSlash` clip
- [ ] NavMesh-based draugr pursuit
- [ ] Lantern light radius and darkness pressure
- [ ] Draugr banish finisher and VFX
- [ ] Fog thinning that gates progress between clearings
- [ ] Honor-as-health economy (combine score and HP)
- [ ] Ambient Norse soundscape
- [ ] Boss: the **Warden of the Bridge**

---

## Lessons Learned

Building this was a crash course in prefabs, bone parenting, local vs. world
space, active-state inheritance, import scale, and editor scripting. The full
write-up is in **[LEARNING.md](LEARNING.md)**.

---

## Credits

- **Player character:** *Warrior viking with red hair and armor* by AgarkovaCG
- **Props:** Medieval Viking Sword; shield from Warrior Model
- **Fog of war:** *AOS Fog of War*
- **Enemy:** *Creep Horror Creature* by AC Game Assets
- **Environment:** Dry Trees, RockFREE, Fantasy Skybox FREE
- **Engine and packages:** Unity, Cinemachine, Input System

---

*Skál. Walk on.*
