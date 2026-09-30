# What I Learned Making Mistwalker

These are my notes from building this game. Not a tutorial. Mostly a record of the
things that broke, what I thought was wrong, and what was actually wrong, so I don't
make the same mistakes twice.

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

That's pretty much how it went. The first ten minutes in Unity felt easy. Then it
wasn't easy for a long time.

---

## The sword that wouldn't show up

All I wanted was to swap the warrior's axe for a Viking sword. I figured it would
take ten minutes. It took days. It also ended up teaching me most of what I know about Unity.

First I had to get used to how the editor works. The Scene view is where you edit and
the Game view is what the player sees, and they don't have to match. Anything you
change during Play mode gets thrown away when you stop. I lost work to that more than
once. Pressing F frames the selected object, but only when your mouse is over the
Scene view, which took me an embarrassingly long time to notice.

Then the basics. A GameObject is just an empty box until you add components to it. To
see a mesh you need a MeshFilter, an enabled MeshRenderer and a material. Leave out
any one and you get nothing. Prefabs are templates, and changing one copy in the scene
only changes that copy.

Putting the sword in his hand meant parenting it to the hand bone, so it moves with
the animation. The first time, I rotated it to point down in world space. It looked
perfect in the rest pose and completely wrong once the idle animation moved his arm.
The rotation has to be set relative to the hand, not the world. A weapon's position
only means something compared to the bone it's attached to.

The worst part was that the sword was invisible. It had a mesh, a renderer, a
material, and its bounds looked right. Opening the prefab on its own showed it
perfectly. In the scene, nothing.

What was actually going on:

- There were two warriors. A disabled copy called `Warrior` still had the old
  skeleton, and `VikingChampion.weapon` pointed at the axe on that one. The visible
  one, `Warrior (1)`, had a different skeleton with a bone called `hand.r`. I'd been
  attaching the sword to the wrong, switched-off twin the whole time.
- An object can be active itself and still not draw if something above it is
  disabled. That's the difference between `activeSelf` and `activeInHierarchy`.
- The warrior's body still showed up because skinned meshes follow bone positions,
  even when the bones' GameObjects are inactive. The sword is a normal mesh, so it
  just vanished.
- `Renderer.bounds` gives you numbers even when nothing is on screen. Good bounds
  don't prove anything.

What cracked it was putting a bright magenta cube in the same spot. When the cube
didn't show up either, I knew the sword was fine and the problem was where it was
attached. After that I stopped trusting "it should be there" and started checking.

Once it showed up, it was a giant. The model was imported at 100× scale, so the bones
had a `lossyScale` of 100 and the sword came out 3 or 4 meters long. Setting its
`localScale` to 1/100 fixed it. I check import scale on every model now.

I also ended up writing editor tools for this, which I didn't know you could do. Any
script in an `Assets/Editor/` folder can add its own menu items with
`[MenuItem("Tools/...")]`. Mine printed the things I needed to see: positions, active
states, parent chains. Commands like `Map Warriors` and `Trace Active Chain` found
the two-warriors problem faster than clicking around in the Inspector ever did. New
menu items only appear after the scripts recompile, so sometimes you have to force a
refresh (Cmd+R).

And I learned to save the scene. At one point the sword was attached and working, I
never pressed ⌘S, and it was just gone the next time I opened the project.

---

## Swapping in the red-haired Viking

Later I replaced the whole player with a red-haired Viking model. His skeleton is
set up as Humanoid, like the old one, so all the walk, run and slash animations kept
working. Humanoid rigs map bones to a standard human skeleton, which is what lets
animations move between characters.

The hard part was everything that pointed at the old character. The camera, the
health bar and the ground check all needed to point at the new one. If you miss one,
nothing crashes. It just quietly stops working.

The sword had another surprise. After the swap the Viking was holding it by the blade. Ouch.
The model's pivot, the point it rotates around, was at the blade end instead of the
handle. So I measured the mesh's bounds, worked out which end was the handle, flipped
the sword around and slid it so the grip sat in his fist. Then I checked it while he
was moving, because it can look right standing still and wrong mid-swing.

## New enemies, and why the wolf was purple

I swapped the shirtless zombie for a creepier creature, then added wolves. Neither
has a Humanoid skeleton. They're "Generic", so the zombie's animations don't work on
them, and each needed its own animation controller built from its own clips. What
made this manageable is that my enemy script only sends two signals, `IsAttacking`
and `IsDead`. Any creature can be an enemy as long as its controller responds to
those two names.

The wolf showed up bright purple. Not great. Unity has three render pipelines (Built-In, URP and
HDRP), and my game uses Built-In. When a material's shader belongs to a different
pipeline, Unity can't draw it and paints it magenta instead. The wolf's fur used a
URP/HDRP shader, even in the folder labeled Built-In. The body was fine, so I just hid
the fur layers. Now I check the render pipeline and supported Unity versions on a
store page before I download anything. The Terrain Sample pack needed a newer Unity
than mine, and Unity's free Viking Village only works in URP.

## Making the fighting feel fair

My first attack hit anything inside a circle around me, including monsters behind my
back. I didn't even have to look at them. Way too easy. Now a swing only hits what's in front of
you, it can't hit through a tree, and if a trunk is right in front of you, you don't
swing at all. Otherwise the sword just goes straight through the bark.

There was also no way to get health back, so every hit was permanent. Now killing a
monster heals you a bit, and you slowly regenerate if you stay out of a fight for a
few seconds.

When you died, the game just froze. I added a "You have fallen" screen with your
score, your best score and "press R to restart". Restarting broke the score display
at first. The score keeper was set to survive scene reloads (`DontDestroyOnLoad`),
so it kept pointing at text from the old scene. I only have one scene, so it didn't
need to survive.

One C# thing that got me: `GetComponent<T>() ?? AddComponent<T>()` doesn't work on
Unity objects. Unity has its own idea of null, so you need a plain `if (x == null)`.

## Fog, light and the ground

Most of the atmosphere came from settings, not art. I turned on fog, made it a cold
blue-grey, and swapped the bright blue sun for dim moonlight. The fog doesn't affect
the skybox, though, so there was a hard line on the horizon. Setting the camera's
background to the fog colour made the distance fade into mist. The camera is close
and looks down, so the fog had to be thicker than I expected.

The ground was an ugly checkerboard for ages. I hated it. I thought it was just a placeholder
texture. It turned out the terrain's texture layers were missing entirely. One free
forest-floor texture from Poly Haven fixed it.

The small things did more than I expected: a flickering lantern, a campfire you only
hear when you're near it, and quiet music in the background.

---

## Getting it online

I wanted the game playable in a browser, so it builds automatically on GitHub now.
The old GitHub check tried to build it as a .NET project, which can't work, because
Unity games need the Unity Editor to build.

Licensing was a whole puzzle, and honestly the most annoying part. My school account logs in through USC, so there's no
password, and GitHub's build machine can't sign in with it. I made a separate free
Unity account just for builds. Even then, the license file is tied to my Mac, so the
build ends up signing in with that account's email and password instead.

The automatic build caught a bug I never would have seen. I'd done a find-and-replace
on some text in a docs commit, and it changed `a - b` into `a: b` inside a C# file. I
didn't have Unity installed at the time, so nothing complained until the build did.

For the web version to work, the scene had to be listed in Build Settings (otherwise
the build is just empty), and "decompression fallback" had to be on for GitHub Pages.
Also, browsers don't play sound until you click or press a key. That's normal and not
a bug.

## Keeping the project small

GitHub won't take files over 100 MB, and the Viking model is 115 MB, so it lives in
Git LFS. Asset packs also bring a lot of stuff you never use: demo scenes, colour
variants, versions for other pipelines. When I traced what the scene actually uses,
624 MB of files weren't used by anything. Anything in a folder named `Resources` gets
packed into the game even if nothing uses it, so those folders are worth checking.
Converting a 40 MB WAV to a 4 MB MP3 sounded the same. Deleted files stay in git
history, so cleaning up isn't scary.

---

## What I'd tell myself on day one

A "simple" task teaches you the most. Putting a sword in a hand dragged me through
prefabs, bones, scale, active state and editor scripts.

Check, don't assume. Every time I said "it should be there," I was wrong, and one
debug log or one test cube settled it.

Where an object sits in the hierarchy matters more than almost anything else. Most of
my hard bugs came down to its parent, its scale, or a switched-off branch above it.

And test it in Play mode. The sword grip, the fog and the wolf's colour all looked
fine somewhere else and wrong in the actual game.

## Quick reference: things that bit me

| What I saw | What it actually was |
| --- | --- |
| Object invisible, components look fine | A parent is disabled (`activeInHierarchy` is false) |
| Object huge or tiny | Import scale, or the parent's `lossyScale` |
| Weapon fine standing still, wrong when animating | Rotated in world space instead of relative to the bone |
| Holding the sword by the wrong end | The mesh's pivot is at the other end |
| Shows in prefab mode but not in the scene | Something about the scene copy (parent, scale, active state) |
| Bright pink or purple | A shader from a different render pipeline |
| Checkerboard ground | Terrain has no texture layers |
| Hard line where fog meets sky | Skybox isn't fogged; use a solid background colour |
| New character won't animate | Generic rig needs its own controller |
| Edits disappeared | Made them in Play mode, or didn't save the scene |
| New menu item missing | Scripts haven't recompiled yet |
| Score broke after restart | Something survived the reload with old references |
| `git push` rejected | File over 100 MB; use Git LFS |
| Web build empty or won't load | No scene in Build Settings, or decompression fallback off |

Still figuring it out, but it's a lot less confusing than it was.
