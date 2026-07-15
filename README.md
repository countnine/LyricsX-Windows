<div align="center">

<img src="docs/img/icon.png" width="88" alt="LyricsX logo">

<h1>LyricsX for Windows</h1>

<b>Real-time, bilingual lyrics on your Windows desktop.</b>

<b>English</b> · <a href="README.ko.md">한국어</a>

<img src="docs/img/demo.gif" width="480" alt="LyricsX demo">

🌐 <a href="https://countnine.github.io/lyricsx-home/"><b>Homepage &amp; demo</b></a> · ⬇️ <a href="https://github.com/countnine/LyricsX-Windows/releases/latest"><b>Download</b></a>

</div>

LyricsX automatically finds the lyrics of the song you're playing, syncs them line-by-line
(and word-by-word), and shows the **original text with its translation** as a transparent
desktop overlay. It's a Windows-native rewrite of the macOS app
[LyricsX](https://github.com/ddddxxx/LyricsX).

## Features

- **Automatic playback detection** — via Windows SMTC. Works with Spotify, Apple Music,
  YouTube Music, and any player that supports media keys.
- **Multi-source lyrics search** — LRCLIB, NetEase, Kugou (酷狗), and QQ Music (QQ音乐),
  merged and auto-ranked by quality.
- **Word-level karaoke** — character-by-character fill for sources that provide inline timing
  (Kugou / QQ / NetEase), with line-level fallback otherwise.
- **Bilingual display** — original and translated lines stacked together. Uses the source's own
  translation first, and falls back to **DeepL machine translation** when needed (your API key).
- **Desktop overlay** — transparent, click-through, always-on-top. Move/resize, fade in/out,
  optional background panel, auto-hide on fullscreen apps / pause / mouse-over.
- **Edit & export** — fix the current lyrics in-app (lossless), export to `.lrc`, or mark wrong
  lyrics to suppress them.
- **Offline cache** — found lyrics (with translations) are stored in SQLite for instant, offline replay.
- **Localized UI — 19 languages** — the interface follows your system language (English fallback),
  selectable in Settings. [Help translate »](TRANSLATING.md)
- **Privacy** — your DeepL API key is stored **encrypted (Windows DPAPI)** and masked in the UI.
- **Automatic updates** — Velopack delta updates from GitHub Releases.

## Download & install

Get the latest build from **[Releases](https://github.com/countnine/LyricsX-Windows/releases/latest)**:

- **`LyricsX-win-Setup.exe`** — installs with automatic updates (recommended).
- **`LyricsX-win-Portable.zip`** — no installation; unzip and run.

> The app isn't code-signed yet, so Windows SmartScreen may warn on first launch — choose
> *More info → Run anyway*.

### Requirements

- Windows 10 version 2004 (20H1) or later, or Windows 11
- No .NET install required (builds are self-contained)

## Usage

1. Run `LyricsX.exe` → a green **L** icon appears in the tray (the hidden-icons `^` area).
2. Play music → lyrics appear automatically near the bottom of the screen.
3. Right-click the tray icon:
   - **Search lyrics…** — search and replace when auto-matching is wrong
   - **Edit current lyrics… / Export (.lrc)…**
   - **Move/resize overlay** — drag to reposition, then toggle off
   - **Settings…** — display language, DeepL API key, translation target language, overlay style
4. Overlay only (no playback): `LyricsX.exe --demo`

### DeepL translation (optional)

Get a free [DeepL API](https://www.deepl.com/pro-api) key (500k characters/month) and enter it in
**Settings**. When a lyrics source lacks a translation in your target language, lines are machine-
translated and cached per line (each song is translated only once). The key is stored encrypted.

## Translating the UI

The interface ships in 19 languages (English + Korean hand-translated, plus DeepL seeds). Anyone can
improve translations directly on GitHub — see **[TRANSLATING.md](TRANSLATING.md)**
([English](TRANSLATING.en.md)).

## Build

```powershell
dotnet build src/LyricsX.App          # dev build
dotnet test  tests/LyricsX.Core.Tests # unit tests
```

Releasing (Velopack + GitHub Releases) is documented in
**[RELEASING.md](RELEASING.md)**.

## Project structure

```
src/LyricsX.Core/   # UI-agnostic engine: LRC parsing, providers, ranking, translation, cache
src/LyricsX.App/    # WPF app: SMTC detection, sync, overlay, tray, settings, i18n
tools/              # mt-bootstrap.ps1 (DeepL translation seeds)
spikes/             # technical spikes (SMTC / overlay / search)
```

## License

Derived from the original LyricsX (GPLv3). Lyrics-search logic is ported from
[LyricsKit](https://github.com/MxIris-LyricsX-Project/LyricsKit).
