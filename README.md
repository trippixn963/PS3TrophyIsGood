<div align="center">

<picture>
   <source media="(prefers-color-scheme: dark)" srcset="doc/images/header-dark.png" width="100%" height="auto">
   <img alt="PS3TrophyIsGood logo" src="doc/images/header-light.png" width="100%" height="auto">
</picture>

# PS3TrophyIsGood

A modern, English, actively-maintained PS3 trophy editor.

[![][github-stars-shield]][github-stars-link]
[![][github-issues-shield]][github-issues-link]
[![][license-shield]][license-link]
[![][last-commit-shield]][last-commit-link]

![DEMO](http://4.bp.blogspot.com/-dMj1nom1pKc/USnCAcmDu6I/AAAAAAAADWg/UFiD6o3uguU/s1600/t1.png)

</div>

## About

PS3TrophyIsGood is a Windows trophy editor for the PlayStation 3 — open a trophy folder copied off a console, edit timestamps, resign for a different account ID, and sync the result back. This is a clean-room continuation of an older Chinese-language project that hasn't seen meaningful updates in years. The codebase has been translated, refactored, and modernized; new features are being added on top.

**What's different in this fork:**

- Full English UI, identifiers, and code comments (original was Chinese)
- Refactored, cleaner internals — easier to extend
- Local-JSON timestamp import alongside the original web-scrape importer
- Optional FlareSolverr (the old build crashed on startup without it)
- Bundled `TROPHYParser`, `BigEndianTool`, `ListViewEx` sources — no submodule setup
- Vendored Visual Studio MigrationBackup junk removed from the tree
- Active issue triage and roadmap

## Build

    git clone https://github.com/trippixn963/PS3TrophyIsGood.git

Open `PS3TrophyIsGood.sln` in Visual Studio 2019+ with the **.NET desktop development** workload and build. Or from a Developer command prompt:

    msbuild PS3TrophyIsGood.sln -t:restore
    msbuild PS3TrophyIsGood.sln -p:Configuration=Release

Targets .NET Framework 4.8.

## Warning

- Syncing modified trophies to PSN is **dangerous** and may get your account banned.
- Trophies may become corrupted.
- Syncing may fail.

If you proceed anyway, you do so at your own risk. Read the rest of this README before clicking through.

## Usage

The program bundles `pfdtool` and handles encryption/decryption automatically — **do not** decrypt anything yourself. After each edit, close the file (or the program) so the trophies get re-encrypted.

Color legend:
- **Gray** — not obtained
- **Red** — already synced to PSN
- **White** — obtained, not yet synced

![DEMO1](http://4.bp.blogspot.com/-dMj1nom1pKc/USnCAcmDu6I/AAAAAAAADWg/UFiD6o3uguU/s1600/t1.png)

The green circle is the trophy name; the red circle is the completion rate. Blue is the PSN XP value — how much your account gains once synced.

![DEMO2](http://1.bp.blogspot.com/-wl5TyzveZ3A/USnDCY--SzI/AAAAAAAADW8/UVHzFwXgaTo/s1600/T2.png)

### Opening a file

Drag the trophy folder straight onto the window — it loads automatically. No need to click through file pickers.

## Walkthrough

If your trophies have never been synced to PSN before, set the console ID and user ID correctly. To change the console ID, edit `global.conf`.

**1.** Copy the trophies off your PS3. They live at `/dev_hdd0/home/000000XX/trophy/`.

**2.** Open the program and drag the trophy folder onto it.

**3.** Edit, then copy the trophies back to the console (back up first).

**4.** Verify:

![outside](http://2.bp.blogspot.com/-v8NAzSPKSHo/USnB_kbSsbI/AAAAAAAADWM/KKRthffJW2g/s1600/TVCAM%25E8%25A3%259D%25E7%25BD%25AE_20130224_145637.289.jpg)

The trophies now show 2%. If you actually open them, they may still appear locked — this is a known quirk; even unlocking everything can show 100% complete while the per-trophy state looks locked. Syncing to PSN still works.

![before sync](http://4.bp.blogspot.com/-yLq0hQb8b28/USnCAKajXSI/AAAAAAAADWY/8ovaRs6eQZ0/s1600/TVCAM%25E8%25A3%259D%25E7%25BD%25AE_20130224_145652.047.jpg)

**5.** Sync to PSN. **This step is dangerous.** Make sure you understand the consequences. If your account or console ID (IDPS) gets banned, nobody can give it back.

![after sync](http://3.bp.blogspot.com/-69ay5OYMsYo/USnB_Xy0ngI/AAAAAAAADWQ/K5YI4SBrAiI/s1600/TVCAM%25E8%25A3%259D%25E7%25BD%25AE_20130224_145646.119.jpg)

After syncing, the trophies appear on your profile. A sync error during the process is common — the trophies usually show up regardless.

On Vita, confirming it actually hit PSN:

![VITA](http://3.bp.blogspot.com/-_Gn65OQVVX8/USnB_ZbHaeI/AAAAAAAADWI/xq-PS-BjwFk/s1600/2013-02-24-145001.jpg)

## Troubleshooting

**HTTP 403 / 500 when fetching timestamps** — Cloudflare is blocking the scrape. Update FlareSolverr to the latest from the [FlareSolverr releases page](https://github.com/FlareSolverr/FlareSolverr/releases) and replace the bundled folder.

## Features

- Edit individual trophy timestamps
- Resign trophy folders to a different account ID (drop a `*.SFO` into `profiles/`)
- Copy timestamps from a user on `psntrophyleaders.com`
- Import timestamps from a local JSON file (name-keyed, tolerant matching)
- Smart copy — add jitter and a time offset so copied profiles look less like a paste-job
- Instant Platinum with randomized timestamp
- DLC detection
- Platinum-aware: unlock the first trophy on platinum-less games without errors
- RPCS3 format support
- FlareSolverr Cloudflare bypass (optional)

## License

MIT — see [LICENSE](LICENSE). Original work © 2016 darkautism.

<!-- Link Definitions -->
[license-shield]: https://img.shields.io/badge/license-MIT-white?labelColor=black&style=flat-square
[license-link]: https://github.com/trippixn963/PS3TrophyIsGood/blob/main/LICENSE
[last-commit-shield]: https://img.shields.io/github/last-commit/trippixn963/PS3TrophyIsGood?color=c4f042&labelColor=black&style=flat-square
[last-commit-link]: https://github.com/trippixn963/PS3TrophyIsGood/commits/main
[github-stars-shield]: https://img.shields.io/github/stars/trippixn963/PS3TrophyIsGood?labelColor=black&style=flat-square&color=ffcb47
[github-stars-link]: https://github.com/trippixn963/PS3TrophyIsGood/stargazers
[github-issues-shield]: https://img.shields.io/github/issues/trippixn963/PS3TrophyIsGood?labelColor=black&style=flat-square&color=ff80eb
[github-issues-link]: https://github.com/trippixn963/PS3TrophyIsGood/issues
