# Uploading to Thunderstore

Checklist for the first upload, from Thunderstore's
[Your first upload](https://wiki.thunderstore.io/mods/your-first-upload) and
[Global Rules](https://wiki.thunderstore.io/moderation/global-rules), applied to this repo. `tcli` builds the
package from `thunderstore/thunderstore.toml`: it writes `manifest.json` itself and zips the files at the root.

## 1. Decisions (taken on 2026-10-18)

- [x] **Team name** = `namespace` in `thunderstore.toml`: `diegorv`. **Permanent**: it is part of every
      dependency string (`diegorv-NineSolsQoL-1.0.0`) and cannot be renamed.
- [x] **Package name**: `NineSolsQoL` (letters, numbers and underscores only; shown on the site as "NineSolsQoL").
      **Permanent**: a new name is a new package.
- [x] **Version**: `1.0.0` (`Major.Minor.Patch`). A version's files can never be changed; fixes go in a higher
      version.
- [x] **websiteUrl**: a public GitHub repo named `NineSolsQoL`. The default branch must match
      `.github/workflows/build.yml` (it builds on `main`; ours is `master`): rename the branch or the trigger.
- [x] **AI Generated category**: yes. The Global Rules say "a package with AI generated assets or code should be
      given the `AI Generated` category" (if the community has it).

## 2. Prepare the package

- [ ] Release polish (`docs/open-items.md`): `DebugLogging` off by default (`Source/Core/DebugTools.cs:34`);
      remove one-off dump lines (LootMagnet's F9 list of nearby drops).
- [ ] `thunderstore/thunderstore.toml`:
  - [ ] `namespace` = the team name (today the placeholder `yournamespace`).
  - [ ] `versionNumber` = the chosen version, the same as `<Version>` in `Source/NineSolsQoL.csproj`.
  - [ ] `description`: at most 250 characters, covering every feature (today it only mentions the respawn).
  - [ ] `websiteUrl` (today the placeholder `link to the git repo`).
  - [ ] `BepInEx-BepInExPack` = `5.4.2305`, the version installed in the profile we tested on (today `5.4.2100`).
- [ ] `thunderstore/icon.png`: our own icon, exactly 256x256 PNG, readable at small size. Today it is the
      template's placeholder.
- [ ] `thunderstore/README.md` (UTF-8 without BOM, already the case):
  - [ ] What the mod does in the first two lines; drop "Work in progress".
  - [ ] Known issues and incompatibilities: a feature turns itself off when the mod it replaces is installed
        (list them), the 99,999 Jin cap.
  - [ ] Credits for the mods whose ideas we used (the table in `README.md`).
  - [ ] Screenshots only by public URL (images inside the zip render broken). Optional.
- [ ] Optional: a `CHANGELOG.md`, newest version first.

## 3. Build the zip

```powershell
dotnet publish Source/NineSolsQoL.csproj
```

It builds in Release, copies the DLL to `thunderstore/build/dll` and runs `tcli build`, which writes
`thunderstore/build/<Team>-NineSolsQoL-<version>.zip`. The build also copies the DLL to the dev profile's
`BepInEx/scripts` (`CopyMod`), as any build does.

- [ ] Open the zip: `manifest.json`, `icon.png`, `README.md` and `NineSolsQoL.dll` at the root, nothing else (no
      `.pdb`, no game DLLs, no dev files).
- [ ] Check `manifest.json` with Thunderstore's Manifest Validator and the README with its Markdown Preview.

## 4. Test the zip on a clean profile

- [ ] r2modman: create a new profile (e.g. `Release test`), without ScriptEngine.
- [ ] Settings → **Import local mod** → the zip. Check that BepInExPack, NineSolsAPI and ConfigurationManager are
      installed with it (install them from Online if not).
- [ ] Start the game. The log shows the mod loaded, `<Feature>: N game method(s) patched` for each feature, and
      no errors from the mod.
- [ ] `diegorv.NineSolsQoL.cfg` is created; F1 shows every group; a change in F1 applies right away.
- [ ] Back to the title screen and into the game again: F1 still opens and the features still work (in a release
      install `Plugin.RegisterLifeCycle` keeps the mod alive, not ScriptEngine).
- [ ] A quick run through each feature (die once, parry, pick up drops, open a Root Node...).
- [ ] Anything changed → build the zip again and test again.

## 5. Commit and tag

- [ ] Commit the release changes; tag `v<version>`.
- [ ] If the repo goes public: push (`.github/workflows/build.yml` builds on `main` and tags `v*`; our branch is
      `master`).

## 6. Upload

- [ ] Sign in at [thunderstore.io](https://thunderstore.io) (Discord, GitHub or Overwolf).
- [ ] Create the team, with exactly the `namespace` name.
- [ ] Upload page: the zip, the team, community **Nine Sols**, the categories that apply (and `AI Generated`, if
      decided), **Contains NSFW content: No** (it must be set on every upload).
- [ ] Review the README before submitting: the files of a version can't be changed afterwards.

## 7. After the upload

- [ ] Package page: icon, README, description in the list, categories, NSFW flag, the three dependencies.
- [ ] Install it from r2modman's Online tab on a clean profile once more.
- [ ] Updates: bump the version in `thunderstore.toml` and the `.csproj`, build, upload as the same team and
      package name (or `tcli publish` with a team service account token).

## Rejection causes

| Message | Usual cause |
|---|---|
| Missing required files | Folder zipped instead of files at the root (`tcli` avoids it) |
| Invalid icon | Not exactly 256x256, or not PNG |
| Invalid manifest | Malformed JSON or a missing field |
| Invalid name | Spaces or other characters in the name |
| Invalid submission | Manual review: ask the moderators on the Thunderstore Discord |
