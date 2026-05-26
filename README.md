<div align="center">

<picture>
   <source media="(prefers-color-scheme: dark)" srcset="doc/images/header-dark.png" width="100%" height="auto">
   <img alt="PS3TrophyIsGood logo" src="doc/images/header-light.png" width="100%" height="auto">
</picture>

# PS3TrophyIsGood

A personal, English rewrite of an old PS3 trophy editor.

[![][license-shield]][license-link]
[![][platform-shield]][platform-link]
[![][framework-shield]][framework-link]
[![][last-commit-shield]][last-commit-link]
[![][stars-shield]][stars-link]

![Demo](http://4.bp.blogspot.com/-dMj1nom1pKc/USnCAcmDu6I/AAAAAAAADWg/UFiD6o3uguU/s1600/t1.png)

</div>

> [!IMPORTANT]
> **This is a personal hobby project.** It's published in case it's useful to anyone else, but:
> - **No support** is provided.
> - **No issues, pull requests, or feature requests** will be reviewed or accepted.
> - **No releases, binaries, or downloads** are published — build it yourself from source.
> - Use entirely **at your own risk.** See [Risks](#risks) before doing anything with this.

---

## What this is

A Windows trophy editor for the PlayStation 3. Open a trophy folder copied from a console, edit timestamps, optionally resign for a different account ID, copy it back.

The original project is in Chinese and hasn't been actively maintained in years. This fork is my own English rewrite for personal use — translated UI, translated identifiers, cleaned-up internals, a few quality-of-life additions.

## What's in this fork

- Full English UI, code identifiers, and comments
- Refactored internals
- Local-JSON timestamp import (in addition to the original web-scrape)
- FlareSolverr is now optional — the old startup path crashed without it
- `TROPHYParser`, `BigEndianTool`, `ListViewEx` sources vendored — no submodule dance
- Visual Studio MigrationBackup artifacts purged from the tree

## Build

Requires **Visual Studio 2019+** with the **.NET desktop development** workload. Target framework is **.NET Framework 4.8**.

```sh
git clone https://github.com/trippixn963/PS3TrophyIsGood.git
```

Open `PS3TrophyIsGood.sln` and build. Or from a Developer command prompt:

```sh
msbuild PS3TrophyIsGood.sln -t:restore
msbuild PS3TrophyIsGood.sln -p:Configuration=Release
```

No prebuilt binary is provided.

## Risks

Read this before touching anything.

- **Modifying trophies and syncing them to PSN can get your account permanently banned.**
- Trophies can become corrupted.
- The sync can fail mid-way and leave a profile in a bad state.
- Your console ID (IDPS) can be flagged.

If any of that happens: there is no recovery, no recourse, and no help available here. Don't run this on an account or console you care about.

## Usage

The program bundles `pfdtool` and handles encryption/decryption itself — **do not decrypt anything manually.** After each edit, close the file (or the program) so the trophies get re-encrypted.

Color legend:

| Color | Meaning |
|---|---|
| Gray | Not obtained |
| White | Obtained, not yet synced to PSN |
| Red | Already synced to PSN |

![Main view](http://1.bp.blogspot.com/-wl5TyzveZ3A/USnDCY--SzI/AAAAAAAADW8/UVHzFwXgaTo/s1600/T2.png)

The green circle is the trophy name; the red circle is the completion rate. Blue is the PSN XP value — what your account gains once synced.

### Opening a file

Drag the trophy folder onto the window. It loads automatically.

## Walkthrough

If the trophies have never been synced to PSN before, set the console ID and user ID correctly first. Console ID lives in `global.conf`.

**1.** Copy the trophy folder off the PS3 — it's at `/dev_hdd0/home/000000XX/trophy/`.

**2.** Open the program and drag the folder onto it.

**3.** Edit, then copy the trophy folder back to the console. **Back up first.**

**4.** Verify on the console:

![On console](http://2.bp.blogspot.com/-v8NAzSPKSHo/USnB_kbSsbI/AAAAAAAADWM/KKRthffJW2g/s1600/TVCAM%25E8%25A3%259D%25E7%25BD%25AE_20130224_145637.289.jpg)

Trophies will show some completion percentage. Quirk: even at 100%, individual trophies may still look locked in the UI — sync still works.

![Before sync](http://4.bp.blogspot.com/-yLq0hQb8b28/USnCAKajXSI/AAAAAAAADWY/8ovaRs6eQZ0/s1600/TVCAM%25E8%25A3%259D%25E7%25BD%25AE_20130224_145652.047.jpg)

**5.** Sync to PSN. **This is the dangerous step.** Re-read [Risks](#risks).

![After sync](http://3.bp.blogspot.com/-69ay5OYMsYo/USnB_Xy0ngI/AAAAAAAADWQ/K5YI4SBrAiI/s1600/TVCAM%25E8%25A3%259D%25E7%25BD%25AE_20130224_145646.119.jpg)

A sync error mid-process is common; trophies usually appear afterward anyway.

Showing up on Vita:

![On Vita](http://3.bp.blogspot.com/-_Gn65OQVVX8/USnB_ZbHaeI/AAAAAAAADWI/xq-PS-BjwFk/s1600/2013-02-24-145001.jpg)

## Features

- Edit individual trophy timestamps
- Resign trophy folders to a different account ID (drop a `*.SFO` into `profiles/`)
- Copy timestamps from a profile on `psntrophyleaders.com`
- Import timestamps from a local JSON file (name-keyed, tolerant matching)
- Smart copy — jitter and time offset so copied profiles look less identical to the source
- Instant Platinum with randomized timestamps
- DLC detection
- Platinum-aware: unlock the first trophy on platinum-less games without errors
- RPCS3 format support
- Optional FlareSolverr-based Cloudflare bypass

## Troubleshooting (one entry)

**HTTP 403 / 500 fetching timestamps** — Cloudflare is blocking the scrape. Drop in the latest FlareSolverr from its [releases](https://github.com/FlareSolverr/FlareSolverr/releases), replacing the bundled folder.

That's the only troubleshooting note. Anything else, you're on your own.

## License

MIT — see [LICENSE](LICENSE). Original work © 2016 darkautism.

<!-- Shields -->
[license-shield]: https://img.shields.io/badge/license-MIT-blue?style=flat-square
[license-link]: ./LICENSE
[platform-shield]: https://img.shields.io/badge/platform-Windows-0078D6?style=flat-square&logo=windows&logoColor=white
[platform-link]: #build
[framework-shield]: https://img.shields.io/badge/.NET%20Framework-4.8-512BD4?style=flat-square&logo=dotnet&logoColor=white
[framework-link]: #build
[last-commit-shield]: https://img.shields.io/github/last-commit/trippixn963/PS3TrophyIsGood?style=flat-square&color=success
[last-commit-link]: https://github.com/trippixn963/PS3TrophyIsGood/commits/main
[stars-shield]: https://img.shields.io/github/stars/trippixn963/PS3TrophyIsGood?style=flat-square&color=ffcb47
[stars-link]: https://github.com/trippixn963/PS3TrophyIsGood/stargazers
