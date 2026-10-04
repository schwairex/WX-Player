# WXPlayer 1.7.12 utility-window visual translation

> Execution: superpowers:executing-plans, inline in the versioned source directory.

**Goal:** Translate the three supplied HTML designs into the existing WPF windows without changing player, timer, selection or data behavior.
**Architecture:** Scoped resources merge SettingsTheme/LiveTheme. Small display-only formatters and window visual helpers retain existing APIs, controls, handlers and masked addresses.
**Tech Stack:** C#, WPF, .NET 10, existing LibVLCSharp and static Manrope/SvgIcon assets.
**Spec:** User's 1.7.12 request; design/wx-player-istatistikler.html, design/wx-player-mini-oynatici.html, design/wx-player-ses-altyazi.html.

## Constraints
- Statistics: 670×730 constructor request, 1-second timer, original metrics/formats, SafeAddress only, no new thresholds.
- Tracks: 700×680, original readonly ComboBoxes, Choice/SelectedValue/handlers, RefreshTracks/Browse unchanged.
- Mini: APIs, callbacks, native HWND, Topmost, WindowChrome and 22px placement preserved; opaque window, no video overlays or new interop.
- No packages, new commands, feature logic or changes to PremiumWindow/core/settings/playback/fullscreen.
- Versioned source and delivery under the approved E: root.

## Review focus
- Long masked addresses/titles and minimum-width windows: readable without exposing credentials.
- No media/unknown values: original defaults and disabled picker rules.
- ComboBox template/ItemTemplate: original IDs and native keyboard selection.
- Mini HWND transfer: attached and visible, no transparent parent or WPF overlay.
- Timer close behavior and formatter fallbacks: no callbacks after close, unknown labels remain raw.

## Steps
- [x] Add scoped UtilityTheme.xaml, UtilityWindowAppearance.cs and display-only UtilityDisplayFormatter.cs.
- [x] StatisticsWindow.cs: fixed summary/metrics, ordered scroll groups, two-column rows, state/selected/danger visuals.
- [x] MiniPlayerWindow.cs: compact strips, title/quality formatting, shared play style, separate live pill, flat noninteractive progress.
- [x] TracksWindow.cs: split original captions, original pickers with shared Settings style, display templates, format chips and plain status.
- [x] Add utility-window smoke route covering actual playback, controls, layout, formats, empty states and timer shutdown.
- [x] Build and run regression/targeted/full smoke; inspect window captures and correct visual failures.
- [x] Review protected methods/file hashes and receive fresh read-only code review.
- [x] Publish/verify EXE, portable and source; write release report/checksums; advance current-source pointer.


