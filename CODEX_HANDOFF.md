# Codex handoff — 2026-07-23

This branch is an intentional WIP checkpoint so work can continue from another
computer without losing the current UI and combat-VFX changes.

## Repository

- Remote: `https://github.com/woohyun5689/Aetheria.git`
- Branch: `codex/ui-combat-visual-refresh`
- Required Unity editor: `6000.3.15f1` (Unity 6.3 LTS)

## Current work

- The earlier expedition/combat UI readability changes are included in this
  branch's working set.
- A new `SkillsV2` combat-effect set was generated for all 26 player skills:
  26 cast, 23 action, 23 impact, and 8 support textures (80 total).
- Four reusable effect layers were added: contact flash, shockwave ring, speed
  streaks, and spark/debris cluster.
- Runtime loading now prefers `SkillsV2` and falls back to the original skill,
  class, and procedural effects.
- Skill motion was split into projectile, melee, vertical, area, and beam
  presentations instead of moving every skill horizontally like a projectile.
- New generated source atlases, exact prompts, manifests, and previews are in
  `ArtSource/Generated/CombatFXV2/`.

## Last verification

- Unity batch compilation succeeded in the isolated QA project.
- V2 import QA passed: `skills=80`, `common=4`.
- Texture dimensions and alpha/corner transparency were checked.
- The original `CombatFX/Skills` assets were not overwritten.

## Work deliberately not completed

- Full rendered UI/VFX QA and play testing were not run after the final changes.
- The main open Unity editor had not refreshed the final files; reload the
  project or leave/re-enter Play mode after pulling.
- Static review found these remaining visual issues:
  - Enemy-facing beam transforms can double-flip directional child art.
  - A negative speed-streak scale can be overwritten by its animation.
  - The support ground ring can still drift while its parent scales/rotates.
- The final fireball warm-color correction was saved, but the image-generation
  worker was stopped before producing its final written report.

## Continue on another computer

```powershell
git clone https://github.com/woohyun5689/Aetheria.git
cd Aetheria
git switch codex/ui-combat-visual-refresh
git pull --ff-only
```

Open the project with Unity `6000.3.15f1`, allow the new textures to import,
then continue by fixing the three review items above and running the full
background render QA.
