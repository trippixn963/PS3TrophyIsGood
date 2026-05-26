<div align="center">

<picture>
   <source media="(prefers-color-scheme: dark)" srcset="doc/images/header-dark.png" width="100%" height="auto">
   <img alt="PS3TrophyIsGood logo" src="doc/images/header-light.png" width="100%" height="auto">
</picture>

# PS3TrophyIsGood 

Best ps3 trophy editor ever
[![][github-stars-shield]][github-stars-link]
[![][github-issues-shield]][github-issues-shield-link]
[![][github-contributors-shield]][github-contributors-link]
[![][license-shield]][license-shield-link]
[![][last-commit-shield]][last-commit-shield-link]
![DEMO](http://4.bp.blogspot.com/-dMj1nom1pKc/USnCAcmDu6I/AAAAAAAADWg/UFiD6o3uguU/s1600/t1.png)

</div>

> **About this fork** — This is an up-to-date, **English** version of the original
> [darkautism/PS3TrophyIsGood](https://github.com/darkautism/PS3TrophyIsGood). The code
> comments and UI identifiers have been translated from Chinese to English, the codebase
> has been cleaned up and refactored, and a local-JSON timestamp import option has been
> added alongside the original web-scrape importer. Released under the original MIT
> license (© darkautism); see [LICENSE](LICENSE).

## Build Tutorial

This repository already bundles the `TROPHYParser`, `BigEndianTool`, and `ListViewEx`
sources, so no submodule setup is required:

	git clone https://github.com/trippixn963/PS3TrophyIsGood.git

Open `PS3TrophyIsGood.sln` in Visual Studio 2019+ (with the **.NET desktop development**
workload) and build. From a Developer command prompt you can instead run:

	msbuild PS3TrophyIsGood.sln -t:restore
	msbuild PS3TrophyIsGood.sln -p:Configuration=Release

The project targets .NET Framework 4.8.

## Download

[CI auto build](https://github.com/darkautism/PS3TrophyIsGood/actions)

![Download link](https://user-images.githubusercontent.com/3898040/108462066-e8c87e00-72b6-11eb-80e1-1447c9cc2c2a.png)

## 403/500 Error

If you has 403/500 error, you can update FlareSolverr version from [download](https://github.com/FlareSolverr/FlareSolverr/releases).
Just replace folder.


## Warning

- Syncing modified trophies to PSN is dangerous and may get your account banned.
- Trophies may become corrupted.
- Syncing may fail.

Even so, if you still want to modify them: in short, do it at your own risk.

Please make sure you have read the text above before clicking to continue.


## Change Log

	v1.3.9
	- Add a backend FlareSolverr proxy as cloudflare bypass.
	- Fix #47, #50

	v1.3.8
	- Add support RPCS3 format

	v1.3.7
	1.  I've added a Resign option, so that you can resign a trophy folder to your specified PARAM.SFO (containing the desired account ID.)
	    - To use this, you must place your own param.sfo in the directory on your computer, "profiles" with any name (as long as it keeps the extension). For example, you could use "DARKNACHO.SFO"
	    - Otherwise, if you do not decide to use the feature, the tool will be default to the account id of the provided trophy folder.

	2. You can copy timestamps of a user from https://psntrophyleaders.com/
	    - This lets you copy all the timestamps from a user's game.
	3. Smart copy option
	    - This allows you to add a few parameters to make the copy look more legitimate, and not just copy-and-paste of the user's profile.
	    - Allows you to add a specific time to bring it to make the times more recent.
	    - Also adds random delta variable to each trophy to make it different
	    - Note: This also detects trophies earned in the same interval, so it adds it the same delta.

	3. Detects DLC from games.
	4. You can unlock the platinum trophy without needing to timestamp any DLC trophies.
	5. Detects if the game has a platinum trophy or not.
	    - If the game does not have a platinum trophy, the user can unlock the first trophy without receiving an error message.

	V1.3.6
	- add random setting

	V1.2.6
	- Fixed a bug where the -8 time zone gave incorrect results.

	V1.2.5
	- Fixed the time zone problem (the +8 hours issue players reported).
	- Reorganized the localizations; you can now pick the language from the menu.
	- Changed the global.conf settings.

	V1.1.4
	- add random Timestamp feature
	- Instant Platinum now uses a random time.

	V1.0.3
	- Fixed a bug with editing times.

	V1.0.2
	- Fixed a major bug.

	V1.0.1
	- Added an application icon.
	- Added multi-language support.
	- The Instant Platinum feature now automatically places the platinum last (its time is the latest trophy + 1 second).
	- Removed unnecessary debug code; opening trophies is now faster.
	- The platinum must now be unlocked last.
	  (You still enter the time yourself, so set the platinum's time to be the latest; otherwise the platinum still won't sync.)

## Tutorial

P.S. This program already bundles pfdtool and handles encryption/decryption automatically, so do not decrypt anything yourself. After each edit you must close the file or close the program, otherwise the trophies won't be re-encrypted.

Gray means not obtained; red means the file has already been synced to PSN.

White means obtained but not yet synced to PSN.

![DEMO1](http://4.bp.blogspot.com/-dMj1nom1pKc/USnCAcmDu6I/AAAAAAAADWg/UFiD6o3uguU/s1600/t1.png)

The green circle is the trophy name; the red circle is the completion rate.

Blue is the PSN experience value — how much XP your PSN account gains once this trophy is synced to PSN.

At the very bottom is the blog address; you can click it to see the latest information on the blog.

![DEMO2](http://1.bp.blogspot.com/-wl5TyzveZ3A/USnDCY--SzI/AAAAAAAADW8/UVHzFwXgaTo/s1600/T2.png)

### Opening a File

If clicking through to select the folder feels like a hassle, you can drag the trophy folder straight into the window and it will load automatically.

## Walkthrough

If your trophies have never been synced to PSN before, set the console id and user id correctly. To change the console id, edit global.conf.

### Step 1

Copy the trophies off your PS3. They are located at /dev_hdd0/home/000000XX/trophy/

### Step 2

Open the program and drag the trophy folder you just copied onto it.

### Step 3

Copy the edited trophies back (please make a backup).

### Step 4

![outside](http://2.bp.blogspot.com/-v8NAzSPKSHo/USnB_kbSsbI/AAAAAAAADWM/KKRthffJW2g/s1600/TVCAM%25E8%25A3%259D%25E7%25BD%25AE_20130224_145637.289.jpg)

The trophies now show 2%.


But if you actually check, they're still locked. I don't know why this happens — if you unlock every trophy it will show 100% yet still appear fully locked when you open it. Even so, we can still sync to PSN.

![befor sync](http://4.bp.blogspot.com/-yLq0hQb8b28/USnCAKajXSI/AAAAAAAADWY/8ovaRs6eQZ0/s1600/TVCAM%25E8%25A3%259D%25E7%25BD%25AE_20130224_145652.047.jpg)

### Step 5

Warning: this step is very dangerous. Make sure you know what you are doing and understand the consequences. If you get banned, I cannot give you back your PSN account or console id (idps).

![AFTER SYNC](http://3.bp.blogspot.com/-69ay5OYMsYo/USnB_Xy0ngI/AAAAAAAADWQ/K5YI4SBrAiI/s1600/TVCAM%25E8%25A3%259D%25E7%25BD%25AE_20130224_145646.119.jpg)


After syncing, the trophies appear. At this step I hit a sync error, but the trophies showed up afterward.

Here is how it looks on the Vita — it really did sync to PSN.

![VITA](http://3.bp.blogspot.com/-_Gn65OQVVX8/USnB_ZbHaeI/AAAAAAAADWI/xq-PS-BjwFk/s1600/2013-02-24-145001.jpg)

## Support the Project

If this project has saved you time or helped you in your workflow, consider supporting its continued development. Your contribution helps me keep the project maintained and feature-rich!

[![][ko-fi-shield]][ko-fi-link]
[![][paypal-shield]][paypal-link]


<!-- Link Definitions -->
[release-shield]: https://img.shields.io/github/v/release/darkautism/PS3TrophyIsGood?color=369eff&labelColor=black&logo=github&style=flat-square
[release-link]: https://github.com/darkautism/PS3TrophyIsGood/releases
[license-shield]: https://img.shields.io/badge/license-apache%202.0-white?labelColor=black&style=flat-square
[license-shield-link]: https://github.com/darkautism/PS3TrophyIsGood/blob/main/LICENSE
[last-commit-shield]: https://img.shields.io/github/last-commit/darkautism/PS3TrophyIsGood?color=c4f042&labelColor=black&style=flat-square
[last-commit-shield-link]: https://github.com/darkautism/PS3TrophyIsGood/commits/main
[github-stars-shield]: https://img.shields.io/github/stars/darkautism/PS3TrophyIsGood?labelColor&style=flat-square&color=ffcb47
[github-stars-link]: https://github.com/darkautism/PS3TrophyIsGood
[github-issues-shield]: https://img.shields.io/github/issues/darkautism/PS3TrophyIsGood?labelColor=black&style=flat-square&color=ff80eb
[github-issues-shield-link]: https://github.com/darkautism/PS3TrophyIsGood/issues
[github-contributors-shield]: https://img.shields.io/github/contributors/darkautism/PS3TrophyIsGood?color=c4f042&labelColor=black&style=flat-square
[github-contributors-link]: https://github.com/darkautism/PS3TrophyIsGood/graphs/contributors
[ko-fi-shield]: https://img.shields.io/badge/Ko--fi-F16061?style=for-the-badge&logo=ko-fi&logoColor=white
[ko-fi-link]: https://ko-fi.com/kautism
[paypal-shield]: https://img.shields.io/badge/PayPal-00457C?style=for-the-badge&logo=paypal&logoColor=white
[paypal-link]: https://paypal.me/kautism
