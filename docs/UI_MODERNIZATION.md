# WXPlayer UI Modernization Guidelines

## Purpose

The current goal for version 1.6.1 is to improve the existing WXPlayer interface so it becomes:

- More modern
- More visually consistent
- More aesthetically polished
- Easier to use
- Better suited to a desktop media and IPTV application

This is a visual and UX modernization of the existing application, not an application rewrite.

Future versions will continue to add new features, so UI modernization must not unnecessarily constrain the architecture.

## Technology

The current UI uses:

- WPF
- XAML
- C#
- `net10.0-windows`
- A custom Fluent-inspired dark theme
- Inter typography
- Custom SVG icons

No framework migration is planned.

Do not migrate to WinUI 3, Avalonia, Electron, React, or another UI framework as part of this modernization.

Do not add a third-party WPF design framework unless explicitly approved.

## Primary UI Areas

The existing visual surfaces include:

- `App.xaml` shared resources
- `MainWindow.xaml`
- `HomeView` and its related partial files
- Sidebar and navigation
- Live TV and library area
- Movie and series presentation
- EPG
- Player controls
- Fullscreen overlay
- `SettingsWindow`
- `SourceWindow`
- `EpisodeWindow`
- `TracksWindow`
- `StatisticsWindow`
- Update-related windows
- `PremiumWindow` shared visual shell
- `SvgIcon`
- `IconLabel`
- `ChannelLogo`

## Safe Visual Changes

Preferred visual changes include:

- Colors
- Typography
- Spacing
- Margins and padding
- Corner radii
- Borders
- Elevation and shadows
- Visual hierarchy
- Control templates
- Card appearance
- Navigation appearance
- Icons
- Hover states
- Selected states
- Focus states
- Disabled states
- Loading states
- Empty states
- Error states
- Subtle transitions and animations
- Responsive layout polish
- Artwork presentation

Prefer centralized and reusable visual resources over repeated hard-coded values.

## Behavior Preservation

Existing functionality must remain unchanged unless the user explicitly requests a behavioral change.

During UI-only work, preserve:

- `x:Name` identifiers
- Existing event handlers
- Bindings
- Callbacks
- Automation names
- Control ownership
- Expected visual-tree relationships used by code-behind

Do not rename or remove controls referenced from C# without first analyzing every usage.

## High-Risk UI Areas

### Fullscreen

Existing fullscreen logic temporarily moves `ControlsBorder` into a separate transparent window and later restores it.

Do not casually:

- Wrap it in new containers
- Replace it
- Change ownership
- Move it
- Change assumptions made by fullscreen code

### Playback UI

Visual changes to playback controls must not alter playback engine behavior.

Do not modify `PlaybackEngine.cs`, `LiveBuffer.cs`, `NativeVideoHost.cs`, or `FullscreenPlacement.cs` for purely visual changes.

### MainWindow

`MainWindow` currently mixes presentation and application orchestration.

Visual work must avoid accidentally changing:

- Provider importing
- SQLite operations
- EPG loading
- Stream resolution
- Recording
- Catch-up
- Updating
- Lifecycle cleanup

## Current Design Problems

Known issues identified from the current implementation are:

- Visual tokens are only partially centralized.
- Many colors are hard-coded.
- Spacing and corner radii are inconsistent.
- UI construction is split between XAML and imperative C#.
- Global implicit styles have a large impact radius.
- Dynamically created windows make global visual changes riskier.
- Navigation appearance is partly controlled from code-behind.
- Focus and accessibility styling is inconsistent.
- Responsive behavior uses manual thresholds.
- `HomeView` can rebuild portions of its visual tree during artwork updates.

These issues should guide modernization priorities, but should not trigger unrelated architectural refactoring.

## Modernization Strategy

Use this incremental sequence:

1. Establish design tokens.
2. Establish shared typography and spacing rules.
3. Improve shared control styles.
4. Improve shared secondary-window visuals.
5. Modernize the main application shell and navigation.
6. Modernize library and list presentation.
7. Modernize Home.
8. Modernize Live TV.
9. Modernize Movies and Series.
10. Modernize EPG.
11. Modernize player controls.
12. Modernize fullscreen presentation.
13. Add consistent loading, empty, error, hover, focus, and selected states.
14. Perform a final consistency and accessibility pass.

Do not redesign all surfaces in a single large change.

## Change Method

For every UI modernization task:

1. Inspect the relevant existing XAML and C# first.
2. Identify which behavior depends on the visual elements being changed.
3. Present the intended visual change before large modifications.
4. Modify the smallest reasonable surface.
5. Build the application.
6. Run relevant smoke and regression checks.
7. Verify the affected UI flow.
8. Report exactly what changed.

## Architecture Rule

The current application is not MVVM.

Do not introduce a full MVVM migration merely to modernize the interface.

Small presentation-focused extractions may be considered later when they directly improve maintainability for future features, but they must be treated as separate architectural work.

## Design System

`docs/DESIGN_SYSTEM.md` will define the actual reusable visual language, including:

- Color tokens
- Typography
- Spacing
- Corner radii
- Elevation
- Control sizing
- Iconography
- Interaction states
- Card patterns
- Navigation patterns
- Player-control patterns

UI work should follow `DESIGN_SYSTEM.md` once it exists.
