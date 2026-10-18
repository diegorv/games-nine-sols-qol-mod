# Releasing to Thunderstore

How a version gets to Thunderstore, from Thunderstore's
[Your first upload](https://wiki.thunderstore.io/mods/your-first-upload) and
[Global Rules](https://wiki.thunderstore.io/moderation/global-rules), applied to this repo.

## How it works

A tag `vMAJOR.MINOR.PATCH` pushed to GitHub runs `.github/workflows/build.yml`:

1. **build**: builds against every game version in `libs-stripped`, then packages the release with the tag's
   version (`-p:Version`, which `PackageMod` passes to `tcli build --package-version`). The package gets a
   `CHANGELOG.md` and the release its notes, both from `scripts/changelog.sh`: the subjects of the commits that
   changed `Source/` or `thunderstore/`, so docs and CI commits stay out. `SHA1SUMS` and `SHA256SUMS` cover the
   zip and the DLL.
2. **release**: the GitHub release "NineSolsQoL x.y.z", with the notes, install instructions, the checksums and
   the zip, DLL and checksum files attached.
3. **thunderstore**: waits for approval in the `thunderstore` environment, then `tcli publish` uploads the same
   zip. Approve only after checking the GitHub release: a Thunderstore version can never be changed.

Commit subjects become the changelog: write them for players ("Add X: what it does", "Fix Y when Z").

## Decisions (taken on 2026-10-18)

- **Team name** (`namespace` in `thunderstore/thunderstore.toml`): `diegorv`. **Permanent**: it is part of
  every dependency string (`diegorv-NineSolsQoL-1.0.0`) and cannot be renamed.
- **Package name**: `NineSolsQoL` (letters, numbers and underscores only). **Permanent**: a new name is a new
  package.
- **First version**: `1.0.0`. A version's files can never be changed; fixes go in a higher version.
- **websiteUrl**: `https://github.com/diegorv/games-nine-sols-qol-mod`.
- **Category**: `mods`. The Nine Sols community has no `AI Generated` category (only Audio, Misc, Libraries,
  Tools, Modpacks and Mods), and the Global Rules ask for it only where the community has it.

## One-time setup

- [ ] Thunderstore: sign in at [thunderstore.io](https://thunderstore.io) (Discord, GitHub or Overwolf) and
      create the team `diegorv`.
- [ ] Thunderstore: in the team's settings, **Service accounts** → add one (e.g. `github-actions`) and copy its
      token (shown once).
- [ ] GitHub repo → Settings → **Environments** → New environment `thunderstore` → **Required reviewers**: me.
      In that environment, add the secret `TCLI_AUTH_TOKEN` with the service account token.
- [ ] Push `main` and check that the build passes.

## Before each release

- [ ] `thunderstore/README.md` describes the current features (UTF-8 without BOM; images only by public URL).
      Check it with Thunderstore's Markdown Preview.
- [ ] New dependency versions in `thunderstore.toml` if needed (tested against them).
- [ ] Build the package locally: `dotnet publish Source/NineSolsQoL.csproj` writes
      `thunderstore/build/diegorv-NineSolsQoL-<csproj version>.zip`. It must hold only `manifest.json`,
      `icon.png`, `README.md` and `NineSolsQoL.dll` (plus `CHANGELOG.md` in CI).
- [ ] Test that zip on a clean r2modman profile:
  - [ ] New profile (e.g. `Release test`), without ScriptEngine; Settings → **Import local mod** → the zip.
        BepInExPack, NineSolsAPI and ConfigurationManager get installed with it (from Online if not).
  - [ ] The log shows the mod loaded and `<Feature>: N game method(s) patched` for each feature, with no errors
        from the mod.
  - [ ] `diegorv.NineSolsQoL.cfg` is created; F1 shows every group; a change applies right away.
  - [ ] Back to the title screen and into the game: F1 still opens and the features still work (a release
        install relies on `Plugin.RegisterLifeCycle`, not ScriptEngine).
  - [ ] A quick run through each feature.

## Release

```powershell
git tag v1.0.0
git push origin main v1.0.0
```

- [ ] The workflow's **build** and **release** jobs pass; the GitHub release has the notes, checksums and files.
- [ ] Approve the **thunderstore** job (Actions → the run → Review deployments).
- [ ] Package page: icon, README, changelog, description in the list, category, the three dependencies.
- [ ] Install it from r2modman's Online tab on a clean profile once.

A failed Thunderstore upload can be retried with "Re-run failed jobs"; a broken release needs a new version (a
new tag), not a changed one.

## The first upload's checklist

- [x] `DebugLogging` off by default; LootMagnet's one-off F9 list of nearby drops removed.
- [x] `thunderstore.toml`: namespace, description (at most 250 characters), websiteUrl, BepInExPack `5.4.2305`
      (the version we tested on), category.
- [x] `thunderstore/README.md`: what it does in the first lines, compatibility, credits.
- [x] `thunderstore/icon.png`: our own icon (a jade talisman with 九 and QoL; source in `icon.svg`), 256x256 PNG.

## Rejection causes

| Message | Usual cause |
|---|---|
| Missing required files | Folder zipped instead of files at the root (`tcli` avoids it) |
| Invalid icon | Not exactly 256x256, or not PNG |
| Invalid manifest | Malformed JSON or a missing field |
| Invalid name | Spaces or other characters in the name |
| Invalid submission | Manual review: ask the moderators on the Thunderstore Discord |
