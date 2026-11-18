# AGENTS.md — COMP 590 Assignment 6: VR Escape Room

Rules for AI agents (and humans) working in this repo. Three people (Amit Khanal, Robert Battle, Jay Bhatta) share one Unity project; the point of this file is to keep version control clean and avoid conflicts.

## Assignment

Build a **VR Escape Room game** in Unity as a team. Brief: `Temp/Assignment 6_ Escape Room.pdf` (`Temp/` is gitignored, local only).

- **Theme: Haunted Victorian Mansion.**
- Engine: Unity 6000.5.9f1. Remote: `https://github.com/robertwbattle/COMP590Assignment6.git`, branch `main`.

Requirements (from the brief):
- Has a theme (above) that informs the environment and puzzles.
- At least one object with a **custom shader** (Shader Graph is fine).
- At least **three puzzles**.
- Is a game: has win/lose conditions.
- The user can **move** around the environment.
- At least one **particle system**.
- Feedback for at least one **sense beyond visuals**.
- Environment should not be mostly taken as-is from an asset store; self-made content earns more credit. All outside assets must be attributed.
- Submission: GitHub link, README (how to run/test, list of outside resources, notes), a documentation video, and each member's separate peer assessment rubric.

Do not make design or implementation decisions about how the game works unless the user asks. Follow their direction.

## Ownership

Each person works in their own files. **Before editing anything you didn't create, or anything shared, check with the user** (who will check with the team). If told which person you're working for, keep edits to that person's work.

Shared files, always coordinate first: `ProjectSettings/`, `Packages/manifest.json`, `Assets/Settings/`, `Assets/InputSystem_Actions.inputactions`, `.gitignore`, `.gitattributes`, `README.md`, `CREDITS.md`, and this file.

## Rules to avoid conflicts

1. **Never commit directly to `main`.** Use a branch named `<initials>/<short-topic>` (e.g. `rb/...`, `ak/...`, `jb/...`). Merge via pull request.
2. **Sync before starting and before pushing:** `git fetch && git rebase origin/main`. Keep branches short-lived.
3. **Scenes and prefabs are the main conflict source.**
   - Only one person edits a given scene at a time; confirm with the user before modifying a shared scene.
   - Prefer putting work in prefabs or separate/additive scenes instead of the shared scene, and personal sandbox scenes for experiments.
   - Never open and re-save a scene or prefab you didn't mean to change; Unity creates noisy diffs.
4. **Always commit `.meta` files with their asset.** Never delete, regenerate, or hand-edit `.meta` GUIDs. Move/rename assets inside the Unity Editor (or move the `.meta` too).
5. **Don't commit generated or machine-local files:** `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `obj/`, `Build/`, IDE folders. Don't force-add them.
6. **Watch for silent changes to shared settings.** Unity often modifies `ProjectSettings/*`, `Assets/Settings/*`, and `Packages/manifest.json` just by opening the project. Run `git diff --stat` before committing and revert changes you didn't intend (`git checkout -- <file>`). Intentional shared-settings changes go in their own commit, announced to the team.
7. **No force-push to `main`; no history rewrites on shared branches.** `--force-with-lease` only on your own branch.
8. **Small, focused commits** with imperative messages.
9. **Record every third-party asset** (name, source, license) in `CREDITS.md` in the same commit that adds it.

## Unity setup (each clone, once)

- Keep **Asset Serialization = Force Text** and **Version Control Mode = Visible Meta Files** (currently correct).
- Register the merge driver that `.gitattributes` references (`unityyamlmerge`), path depends on your install:
  ```
  git config merge.unityyamlmerge.name "Unity SmartMerge"
  git config merge.unityyamlmerge.driver '"<UnityInstall>/Editor/Data/Tools/UnityYAMLMerge" merge -p %O %B %A %A'
  ```
- `.gitattributes` defines Git LFS patterns for binary assets. Run `git lfs install` before adding large binaries and verify with `git lfs ls-files`. Keep large videos out of the repo and link them from the README.
- Everyone uses Unity **6000.5.9f1**. Don't upgrade the editor or packages without team agreement.

## Agent checklist

Before editing:
1. `git status`, `git branch --show-current`; if on `main`, create a branch.
2. `git fetch`; if `git log HEAD..origin/main --oneline` shows commits, rebase.
3. Confirm the files you'll touch aren't shared or someone else's.

Before committing:
1. `git diff --stat`; revert unexpected `.asset`, `ProjectSettings`, `.unity`, `.prefab` changes.
2. Every new asset has its `.meta` staged; no ignored/generated files staged; outside assets are in `CREDITS.md`.
3. Stage explicit paths instead of `git add -A` when shared files are modified.

Before pushing:
1. Rebase on `origin/main` and resolve conflicts.
2. `git push -u origin <branch>` and open a PR. If it touches shared files, a teammate should review before merge.

## Resolving conflicts

- **C# scripts:** resolve normally keeping both sides' intent; compile in the Editor.
- **Scenes/prefabs/ScriptableObjects (YAML):** don't hand-merge unless trivial. Take one side (`git checkout --theirs/--ours <file>`), then redo the small change in the Editor.
- **`.meta` conflicts:** duplicate GUIDs mean two assets claim one GUID; keep the one existing references use, regenerate the other in the Editor.
- **`Packages/manifest.json` / `packages-lock.json`:** keep both sides' dependencies; let Unity regenerate the lock file.
- If unsure, stop and ask rather than guess.

## Don't

- Don't commit builds (`.apk`), large recordings, or the assignment PDF.
- Don't rename/move folders or files others are working on.
- Don't reformat whole files (line-ending/whitespace sweeps).
- Don't delete template leftovers (`Assets/TutorialInfo`, `SampleScene`) without team agreement.
