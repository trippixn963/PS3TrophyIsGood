<div align="center">

# PS3TrophiesIsPerfect

<img src="PS3TrophiesIsPerfect/Assets/banner.png" alt="PS3TrophiesIsPerfect — PS3 save & trophies manipulation app" width="100%" />

**A modern PlayStation 3 trophy &amp; save editor for Windows — built from the ground up.**
Clone a real player's run, pace it like a human actually played it, and verify it side-by-side.

[![][license-shield]][license-link]
[![][platform-shield]][platform-link]
[![][framework-shield]][framework-link]
[![][last-commit-shield]][last-commit-link]
[![][stars-shield]][stars-link]

</div>

> [!IMPORTANT]
> **This is a personal hobby project.** It's published in case it's useful to anyone else, but:
> - **No support** is provided.
> - **No issues, pull requests, or feature requests** will be reviewed or accepted.
> - A **prebuilt release** is available under [Releases](https://github.com/trippixn963/PS3TrophiesIsPerfect/releases) — or build it yourself from source.
> - Use entirely **at your own risk.** See [Risks](#risks) before doing anything with this.

---

## Overview

PS3TrophiesIsPerfect is a Windows app for working with PlayStation 3 trophy data: open a trophy folder pulled off a console, edit unlock times, optionally re-sign it for another account, and write it back — all without touching the encryption by hand.

The application is **built entirely from the ground up** — a custom WPF interface, a PSNProfiles cloning workflow, a legit-pacing relocation engine, a donor comparison view, and a PSN-powered game browser. The one piece I didn't write is the low-level save-data engine (`TROPHYParser`) that reads and writes the encrypted trophy format; that reverse-engineering is darkautism's original work and is used unchanged. Everything else is mine. See [Credits](#credits).

---

## Features

### ◆ Edit your trophies

The main view is your full trophy list rendered with real in-game artwork. Each row is a card showing the trophy, its description, its type (bronze → platinum), and the exact unlock time with the gap from the trophy before it.

- **Set, change, or lock** any trophy — double-click to edit the time, right-click for more, or **Clear** them all at once.
- **Sort** by unlock order, name, type, or most recent; **filter** by name.
- **Re-sign** the folder for a different account — drop a `*.SFO` into `profiles/` and pick it when saving.
- Already-synced trophies are tinted and protected, so you can't accidentally break them.

<div align="center">
<img src="PS3TrophiesIsPerfect/Assets/screenshots/trophies.png" alt="Editing the trophy list" width="100%" />
<br><sub><b>Trophies</b> — real artwork, unlock times, and the spacing between each pop.</sub>
</div>

### ◆ Clone a real run — and prove it

**Copy from PSNProfiles** pulls a real player's earned trophies straight from their profile page (Cloudflare handled automatically) and matches them to your game **by name**, so cosmetic title differences don't break it. The relocation engine then rebuilds that run as believable nightly play sessions across a date window you choose:

- the **first trophy lands on your start date**, the **platinum pops today**;
- **bursts** (stacked / story pops) keep the donor's **exact** gaps;
- every other gap is **slower** than the donor, never faster;
- nothing is **ever dated in the future**.

The **Comparison** tab then diffs your applied run against the donor's, trophy by trophy, with a verdict on every gap — `✓ exact` for bursts, `+slower` where it's intended, and `⚠ faster` flagged in red so you can spot anything that shouldn't happen.

<div align="center">
<img src="PS3TrophiesIsPerfect/Assets/screenshots/comparison.png" alt="Comparing your run against the donor's" width="100%" />
<br><sub><b>Comparison</b> — your run vs. the donor's, gap by gap, with a verdict on each.</sub>
</div>

### ◆ Browse your PS3 library

**My PS3 Games** pulls your library straight from Sony's own trophy data — game art, completion, and per-type trophy counts, with a DLC marker on titles that have add-on trophies. Click into any game for its full trophy list with icons, descriptions, rarity, "earned X ago," and Base Game / DLC sections.

- **Sort** by completion, name, or most recently played; **filter** by title.
- Everything is cached on disk, so the tab opens instantly after the first load.

<div align="center">
<img src="PS3TrophiesIsPerfect/Assets/screenshots/library.png" alt="Browsing your PS3 library" width="100%" />
<br><sub><b>My PS3 Games</b> — your library from Sony's data: art, type counts, and completion.</sub>
</div>

<div align="center"><sub><i>Account and profile names are blurred in the screenshots above.</i></sub></div>

---

## Build

Requires the **.NET SDK** and the **.NET Framework 4.8** targeting pack (ships with Visual Studio 2019+ / Build Tools).

```sh
git clone https://github.com/trippixn963/PS3TrophiesIsPerfect.git
cd PS3TrophiesIsPerfect
dotnet build PS3TrophiesIsPerfect/PS3TrophiesIsPerfect.csproj -c Debug
```

`pfdtool` (encryption) is bundled. For **Copy from PSNProfiles**, a `flaresolverr/` folder must sit next to the `.exe` — grab it from [FlareSolverr releases](https://github.com/FlareSolverr/FlareSolverr/releases) (it's not bundled in the download). Launch the app **non-elevated** (a normal double-click) — running it as administrator blocks folder drag-and-drop.

A prebuilt Windows build is published under [Releases](https://github.com/trippixn963/PS3TrophiesIsPerfect/releases) if you'd rather not build it yourself.

---

## Risks

Read this before touching anything.

- **Modifying trophies and syncing them to PSN can get your account permanently banned.**
- Trophies can become corrupted.
- The sync can fail mid-way and leave a profile in a bad state.
- Your console ID (IDPS) can be flagged.
- Syncing a **future-dated** trophy is an instant flag — the relocation engine guards against it, but you are still responsible for what you sync.

If any of that happens: there is no recovery, no recourse, and no help available here. Don't run this on an account or console you care about.

---

## Usage

The program bundles `pfdtool` and handles encryption/decryption itself — **do not decrypt anything manually.** After editing, **Save** re-encrypts the folder.

| Row | Meaning |
|---|---|
| Dimmed | Not obtained |
| Normal | Obtained, not yet synced to PSN |
| Rose tint | Already synced to PSN (can't be edited) |

**Walkthrough:**

1. If the trophies have never been synced before, set the console ID and user ID correctly first (console ID lives in `global.conf`).
2. Copy the trophy folder off the PS3 — it's at `/dev_hdd0/home/000000XX/trophy/`.
3. Open the app and drag the folder onto it.
4. *(Optional)* **Copy from PSNProfiles**, relocate to a date window, and review the **Comparison** tab.
5. Edit as needed, **Save**, then copy the folder back to the console. **Back up first.**
6. Sync to PSN. **This is the dangerous step** — re-read [Risks](#risks). A sync error mid-process is common; trophies usually appear afterward anyway.

> **HTTP 403 / 500 fetching timestamps?** Cloudflare is blocking the scrape. Drop in the latest FlareSolverr from its [releases](https://github.com/FlareSolverr/FlareSolverr/releases), replacing the bundled `flaresolverr/` folder. That's the only troubleshooting note — anything else, you're on your own.

---

## Credits

**PS3TrophiesIsPerfect is built from the ground up by [trippixn963](https://github.com/trippixn963).** The application — the WPF interface, the PSNProfiles cloning workflow, the relocation engine, the comparison view, the PSN-powered library, and everything else — is original work.

The one borrowed component is the low-level save-data engine:

- **[darkautism](https://github.com/darkautism)** — `TROPHYParser`, the reverse-engineering of the PlayStation 3's encrypted trophy save format (the read/write logic). That is the genuinely hard part, and it is used here **unchanged.** © 2016 darkautism.

Bundled third-party tools (redistributed, not authored here):

- **flatz** — `pfdtool`, used for trophy encryption / decryption.
- **[FlareSolverr](https://github.com/FlareSolverr/FlareSolverr)** — Cloudflare bypass for the PSNProfiles scrape.

---

## License

MIT — see [LICENSE](LICENSE). The bundled `TROPHYParser` save-data engine is © 2016 darkautism; all other code is original work by trippixn963.

<!-- Shields -->
[license-shield]: https://img.shields.io/badge/license-MIT-blue?style=flat-square
[license-link]: ./LICENSE
[platform-shield]: https://img.shields.io/badge/platform-Windows-0078D6?style=flat-square&logo=windows&logoColor=white
[platform-link]: #build
[framework-shield]: https://img.shields.io/badge/.NET%20Framework-4.8-512BD4?style=flat-square&logo=dotnet&logoColor=white
[framework-link]: #build
[last-commit-shield]: https://img.shields.io/github/last-commit/trippixn963/PS3TrophiesIsPerfect?style=flat-square&color=success
[last-commit-link]: https://github.com/trippixn963/PS3TrophiesIsPerfect/commits/main
[stars-shield]: https://img.shields.io/github/stars/trippixn963/PS3TrophiesIsPerfect?style=flat-square&color=ffcb47
[stars-link]: https://github.com/trippixn963/PS3TrophiesIsPerfect/stargazers
