# WXPlayer Design System

## 1. Design Direction

WXPlayer uses a **Fluent + Cinematic hybrid** visual language. It combines the clarity, structure, and desktop usability of Fluent-inspired Windows interfaces with the artwork-led presentation expected from a premium media application.

Fluent principles should dominate:

- Navigation and application structure
- Settings and utility screens
- Dialogs and secondary windows
- Inputs, menus, filters, and system controls
- Focus, selection, validation, and accessibility states

Cinematic presentation should dominate:

- Home
- Movies and Series
- Artwork and poster presentation
- Hero areas
- Continue-watching experiences
- Playback and fullscreen overlays

The application should feel like a premium native Windows media application. It should remain calm, responsive, readable, and precise even when large libraries, dense EPG data, or playback controls are visible.

Visual styling must never compromise readability or interaction clarity. Artwork supports the content hierarchy; it must not obscure titles, metadata, focus, or controls.

WXPlayer may use established media applications as broad references for content presentation, but it must not directly imitate Netflix, Plex, or another product.

## 2. Core Principles

### Content First

Channels, programs, artwork, titles, playback state, and user actions are the primary content. Decorative surfaces should frame this content without competing with it.

### Clear Hierarchy

Every screen should make the current page, primary content, selected item, and next available action immediately understandable.

### Consistency

Equivalent controls and states should use the same tokens, proportions, and interaction feedback throughout the application.

### Native Desktop Usability

Layouts should prioritize mouse and keyboard use on medium and large Windows displays. Controls must be precise, focusable, and appropriately sized without adopting oversized mobile patterns.

### Subtle Depth

Use borders, tonal surface changes, overlays, and restrained shadows to communicate hierarchy. Depth should clarify relationships rather than decorate every element.

### Controlled Motion

Animation should explain state changes and improve orientation. It must remain short, subtle, and interruptible.

### Accessibility

Maintain sufficient contrast, visible focus, readable type, accessible names, keyboard navigation, and non-color indicators for state.

### Reusability

Prefer semantic resources, shared styles, and reusable visual patterns over screen-specific values.

### Visual Restraint

Avoid unnecessary gradients, glow, strong borders, excessive badges, and competing accent colors. Emphasize only what needs attention.

### Preserve Existing Functionality

Visual modernization must not change existing behavior unless a requested task explicitly includes a behavioral change.

## 3. Color System

All colors should be implemented as semantic resources. Components should reference token names rather than one-off hexadecimal values.

### Background and Surface Colors

| Token | Value | Use |
|---|---:|---|
| `BackgroundBase` | `#0B0F14` | Main application background and deepest canvas |
| `BackgroundSecondary` | `#10161F` | Secondary page regions and side panels |
| `Surface` | `#151C26` | Cards, grouped controls, and standard containers |
| `SurfaceElevated` | `#1B2430` | Popups, elevated cards, menus, and dialogs |
| `SurfaceHover` | `#202B38` | Hovered neutral surfaces |
| `SurfaceSelected` | `#183247` | Selected surfaces with restrained accent presence |

### Border Colors

| Token | Value | Use |
|---|---:|---|
| `BorderSubtle` | `#263241` | Default separators and low-emphasis outlines |
| `BorderStrong` | `#3A4A5E` | Focus-adjacent boundaries and emphasized separation |

### Text Colors

| Token | Value | Use |
|---|---:|---|
| `TextPrimary` | `#F4F7FA` | Primary titles, labels, and high-priority information |
| `TextSecondary` | `#B7C2CF` | Descriptions, supporting labels, and metadata |
| `TextMuted` | `#7F8C9B` | Tertiary metadata and low-priority information |
| `TextDisabled` | `#566271` | Disabled labels and unavailable values |

### Accent Colors

| Token | Value | Use |
|---|---:|---|
| `AccentPrimary` | `#42B8F5` | Primary actions, focus, selected indicators, and progress |
| `AccentHover` | `#62C7FA` | Hovered primary actions |
| `AccentPressed` | `#2398D4` | Pressed primary actions |
| `AccentSubtle` | `#1E3E52` | Low-emphasis accent backgrounds and selection fills |

The accent is a refined cool cyan-blue that reads clearly on dark surfaces while retaining a native Windows character. Use it sparingly. Large areas should remain neutral.

### Status Colors

| Token | Value | Use |
|---|---:|---|
| `Success` | `#55C88A` | Successful operations and healthy status |
| `Warning` | `#E8B45C` | Warnings and attention states |
| `Error` | `#F06A74` | Errors, destructive validation, and failed operations |
| `Info` | `#6AAFF6` | Neutral information and notices |

### Playback Colors

| Token | Value | Use |
|---|---:|---|
| `PlayerOverlay` | `#B30A0E14` | Dark translucent player overlay, approximately 70% opacity |
| `PlayerControlBackground` | `#CC151C26` | Floating player-control surfaces, approximately 80% opacity |
| `PlayerProgressTrack` | `#5969798A` | Unplayed progress track |
| `PlayerProgressFill` | `#42B8F5` | Played progress and active seek state |

Avoid excessive gradients. Gradients are appropriate for selective hero and artwork readability overlays, usually from transparent to `BackgroundBase`. Do not use gradients as decoration on standard controls, cards, settings, or lists.

## 4. Typography

WXPlayer uses **Inter** throughout the application. Keep the number of type sizes controlled so hierarchy comes from weight, spacing, and placement as well as size.

Line-height values below are targets. WPF implementations should use practical equivalents that preserve legibility and avoid clipping.

| Role | Size | Weight | Line height | Intended use |
|---|---:|---:|---:|---|
| `Display` | 32 px | 700 | 40 px | Hero titles and rare high-impact headings |
| `PageTitle` | 24 px | 650–700 | 32 px | Primary page headings |
| `SectionTitle` | 18 px | 600–650 | 26 px | Content-row and grouped-section headings |
| `CardTitle` | 14 px | 600 | 20 px | Movie, series, channel, and program titles |
| `Body` | 14 px | 400 | 21 px | Standard readable text and input content |
| `BodySecondary` | 13 px | 400 | 19 px | Supporting descriptions and secondary values |
| `Caption` | 12 px | 500 | 17 px | Compact labels and helper text |
| `Metadata` | 11 px | 500 | 16 px | Time, category, episode, and technical metadata |
| `ButtonText` | 13 px | 600 | 18 px | Button and compact action labels |

Guidelines:

- Use `Display` only when artwork and available space support it.
- Prefer one `PageTitle` per main page.
- Keep card titles to one or two lines and trim gracefully.
- Avoid using all caps for long labels. Short status chips may use uppercase with restrained letter spacing.
- Use `TextSecondary` or `TextMuted` rather than very small type to reduce emphasis.
- Do not introduce additional type sizes without a demonstrated layout need.

## 5. Spacing System

Use the following spacing scale:

| Token | Value | Typical use |
|---|---:|---|
| `Space4` | 4 px | Tight icon/text separation and compact internal gaps |
| `Space8` | 8 px | Related control parts, compact rows, and badge padding |
| `Space12` | 12 px | Standard control padding and compact card gaps |
| `Space16` | 16 px | Default container padding and related-item spacing |
| `Space20` | 20 px | Comfortable card padding and grouped content |
| `Space24` | 24 px | Section separation and standard page gutters |
| `Space32` | 32 px | Major section breaks and page-level grouping |
| `Space40` | 40 px | Large content transitions and spacious hero composition |
| `Space48` | 48 px | Rare top-level separation on large layouts |

Use 4–12 px for internal control relationships, 16–24 px for cards and groups, and 32–48 px for major sections. Do not introduce random spacing values unless required for compatibility with an existing layout or pixel alignment.

## 6. Corner Radius System

Use moderate Windows-style rounding:

| Token | Value | Use |
|---|---:|---|
| `RadiusSmall` | 4 px | Compact controls, progress tracks, and small chips |
| `RadiusMedium` | 8 px | Buttons, inputs, list selections, and standard controls |
| `RadiusLarge` | 12 px | Cards, grouped settings, menus, and secondary surfaces |
| `RadiusXL` | 16 px | Hero artwork, major panels, and modal shells |

Avoid excessive pill-shaped controls. Full pills are suitable for compact filters, status chips, and toggle-like elements where the shape communicates the interaction.

## 7. Elevation and Depth

Depth levels should remain subtle:

| Level | Visual treatment | Typical use |
|---|---|---|
| Flat surface | Tonal background difference or subtle border | Page canvas, lists, sidebar regions |
| Elevated card | Soft shadow plus `Surface` or `SurfaceElevated` | Hovered media cards and emphasized content |
| Popup/dialog | Stronger soft shadow plus `SurfaceElevated` | Menus, dialogs, and secondary windows |
| Overlay | Translucent dark layer with optional edge separation | Player and fullscreen controls |

Recommended WPF shadow guidance:

- Elevated card: opacity 0.18, blur radius 12, depth 2
- Popup/dialog: opacity 0.28, blur radius 24, depth 6
- Overlay: favor translucent fill and border over a large shadow

Do not make the interface glossy or make every surface appear to float. Use depth to separate hierarchy and clarify temporary layers.

## 8. Layout Rules

### Page Margins

- Standard page margin: 24 px
- Compact-width page margin: 16 px
- Large presentation areas may use 32 px when space permits

### Section and Card Spacing

- Standard section separation: 32 px
- Related subsection separation: 20–24 px
- Standard card gap: 12–16 px
- Dense Live TV and EPG gaps: 8–12 px

### Sidebar

- Expanded width target: 224–248 px
- Collapsed width target: 64–72 px
- Keep navigation icons aligned between expanded and collapsed states.
- Collapse only when it improves usable content width or when the user explicitly chooses it.

### Density

- Home, Movies, and Series may use more breathing room and artwork.
- Live TV, EPG, history, and settings should carry more information without feeling crowded.
- Avoid showing more than two supporting metadata lines on a media card.

### Responsive Behavior

- Desktop is the priority.
- Adapt through card count, flexible columns, trimming, wrapping, and sidebar state.
- Preserve readable minimum widths for cards, rows, filters, and player controls.
- Avoid mobile-style single-column transformations unless the window is genuinely narrow.
- Use a small, documented set of layout thresholds instead of many local breakpoints.

### Interaction Targets

- Standard control height: at least 36 px
- Primary and prominent control height: 40 px
- Compact utility control height: at least 32 px
- Icon-only targets: at least 36 × 36 px
- Player controls: preferably 40–44 px targets

## 9. Navigation

Navigation should feel Fluent and native to Windows.

### Sidebar

- Use `BackgroundSecondary` as the base.
- Separate major groups with spacing or a subtle divider, not additional boxed panels.
- Keep icon and label alignment consistent.
- Use `RadiusMedium` for navigation item states.

### Default, Hover, and Active Items

- Default: transparent or neutral surface with `TextSecondary`.
- Hover: `SurfaceHover` with `TextPrimary`.
- Active: `SurfaceSelected`, `TextPrimary`, and a narrow `AccentPrimary` indicator.
- Avoid a bright accent fill across the entire active row.

### Icons and Labels

- Use 18–20 px SVG icons with consistent visual weight.
- Use filled variants only for active state when an appropriate pair exists.
- Keep labels concise and use `ButtonText` or `BodySecondary` typography.

### Collapse Behavior

- Preserve tooltips and accessible names when labels are hidden.
- Keep selection and hover states equally clear in collapsed mode.
- Do not change navigation destinations or event ownership as part of a visual collapse treatment.

## 10. Buttons and Inputs

All controls require default, hover, pressed, focused, and disabled states.

### Primary Button

- `AccentPrimary` background with high-contrast dark text.
- Use for the primary action in a surface; avoid multiple competing primary buttons.
- Hover uses `AccentHover`; pressed uses `AccentPressed`.

### Secondary Button

- `SurfaceElevated` background, `BorderSubtle` outline, and `TextPrimary` label.
- Hover uses `SurfaceHover` and a slightly stronger border.

### Ghost Button

- Transparent default surface.
- Hover uses `SurfaceHover`.
- Suitable for low-emphasis actions in toolbars and overlays.

### Icon Button

- Minimum 36 × 36 px target with a centered 18–20 px icon.
- Always provide a tooltip or accessible name.
- Avoid permanent circular backgrounds unless the context calls for a floating control.

### Destructive Button

- Use `Error` for the border, label, or restrained background.
- Reserve solid destructive fills for explicit, high-confidence destructive actions.

### TextBox and ComboBox

- Height: 36–40 px.
- Use `Surface`, `BorderSubtle`, and `TextPrimary`.
- Hover strengthens the border subtly.
- Focus uses a clear `AccentPrimary` border or focus ring.
- Placeholder text uses `TextMuted`.
- Validation uses an icon or message in addition to color.

### CheckBox

- Use a compact 18–20 px box with a clear focus state.
- Checked state uses `AccentPrimary` and a high-contrast glyph.
- Keep the label itself clickable.

### Slider

- Use a visibly larger interactive hit area than the rendered track.
- Default track uses `PlayerProgressTrack` or `BorderStrong`.
- Filled track uses `AccentPrimary`.
- Thumb must remain clear during hover, drag, focus, and keyboard adjustment.

### Toggle-Like Controls

- Use only when the state is binary and persistent.
- Pair the visual state with a readable label.
- Do not rely on accent color alone to indicate on/off state.

### Disabled State

- Reduce contrast without making text unreadable.
- Use `TextDisabled`, a subdued border, and no elevation.
- Retain recognizable control shape.

## 11. Media Cards

Media-card families should share alignment and interaction behavior while adapting to content type.

### Movies and Series

- Poster-first portrait aspect ratio, preferably 2:3.
- Use consistent card widths and image heights within each row.
- Display a clear title below or in a restrained lower overlay.
- Show only essential secondary metadata such as year, season count, genre, or runtime.
- Use subtle hover elevation or scale, generally no more than 1.02–1.03.
- Show viewing progress as a compact bar near the bottom edge.
- Keep badges limited to meaningful states such as new, watched, or unavailable.

### Recently Watched and Continue Watching

- Give progress higher priority than category metadata.
- Preserve title, episode context, and remaining or elapsed state where available.
- Make the resume action clear without covering the artwork.

### Live TV

- Use a denser landscape or row-based pattern.
- Include channel logo, channel name, current program, and optional EPG timing/progress.
- Logos should use a consistent containment area and preserve aspect ratio.
- Provide a stable placeholder when a logo is loading or unavailable.

Avoid placing excessive metadata directly over artwork. Use overlays primarily for readability, status, or a concise action.

## 12. Hero Area

The Home hero should establish cinematic identity without consuming the entire window.

- Use a large backdrop or artwork region with `RadiusXL` where applicable.
- Apply a dark horizontal or vertical readability gradient toward the text area.
- Show one title, limited metadata, and a short supporting description when useful.
- Limit the area to one or two primary actions.
- Keep text width controlled so descriptions remain easy to scan.
- Reserve enough surrounding space for subsequent content rows to remain visible.
- Use a stable minimum and maximum height rather than scaling indefinitely with the window.

Do not overload the hero with technical metadata, multiple badges, or many actions.

## 13. Lists and Library Rows

Channel, EPG, content, favorites, and history rows should use:

- Consistent height within the same list type
- Strong column and baseline alignment
- Clear selected and keyboard-focused states
- Subtle hover feedback
- Readable secondary information
- Trimming with tooltips when text cannot fit

Recommended heights:

- Compact utility row: 40 px
- Standard library row: 52–56 px
- Rich channel or program row: 64–72 px

Use spacing and tonal grouping instead of strong borders between every row. Dividers should be subtle and used only when alignment alone is insufficient.

## 14. EPG

EPG should favor Fluent productivity and information clarity over cinematic styling.

- Keep time headers fixed or visually persistent where practical.
- Give current time a clear but restrained accent indicator.
- Emphasize the current program through `SurfaceSelected`, progress, and typography.
- Use a visible progress bar for the current program.
- Use restrained separators to maintain grid readability.
- Align program blocks precisely with the time scale.
- Provide distinct hover, selected, focused, unavailable, and catch-up states.
- Keep channel logos and names consistently aligned.
- Use `TextMuted` for past or lower-priority program information without making it illegible.

Selection must remain clear through more than color alone, such as a focus outline, border, or icon.

## 15. Player UI

The player should feel cinematic and minimal.

### Overlay

- Place controls over `PlayerOverlay` or `PlayerControlBackground`.
- Use a soft gradient when controls sit directly over video.
- Hide nonessential controls after inactivity where existing behavior supports it.

### Primary Controls

- Give play/pause the strongest control emphasis.
- Keep seek, volume, fullscreen, tracks, subtitles, and related actions consistently aligned.
- Use 40–44 px interaction targets.
- Show hover, pressed, focus, and active states without strong glow.

### Progress and Volume

- Provide a forgiving hit area around thin rendered tracks.
- Keep elapsed, duration, live position, and buffer state readable.
- Distinguish available buffer from played progress where supported.

### Media Information

- Show the current title and concise episode, channel, or program context.
- Avoid persistent technical information unless the user opens statistics.

### Tracks and Subtitles

- Use a structured popup or secondary surface with clear selected states.
- Keep language and track labels readable and aligned.

Playback functionality, event handling, control ownership, and existing visual-tree assumptions must not change during purely visual work.

## 16. Fullscreen

Fullscreen presentation should use:

- Temporary overlays
- Strong text and icon readability
- Large interaction targets
- Minimal persistent chrome
- Clear channel, episode, or media context when controls appear

Controls should disappear after inactivity where existing behavior permits and return predictably with mouse or keyboard input.

Preserve the existing `ControlsBorder` ownership behavior documented in `UI_MODERNIZATION.md`. Do not casually wrap, replace, move, or change ownership of that element.

## 17. Settings and Dialogs

Settings and dialogs should lean strongly Fluent.

### Settings

- Use left navigation for stable top-level categories when the number of sections justifies it.
- Group related settings under clear `SectionTitle` headings.
- Pair labels with short descriptions when behavior is not self-evident.
- Use cards or surfaces selectively for meaningful groups, not for every individual option.
- Align switches, inputs, and actions consistently.

### Dialogs and Secondary Windows

- Use `SurfaceElevated`, `RadiusXL`, and restrained popup elevation.
- Keep titles, messages, content, and action rows clearly separated.
- Place the primary action consistently and avoid multiple primary treatments.
- Keep cancel or close actions immediately discoverable.
- Avoid cinematic artwork inside normal settings, maintenance, confirmation, or error dialogs.

The shared `PremiumWindow` shell should provide consistent chrome, padding, title treatment, and action placement across secondary windows.

## 18. Icons

Continue using the existing SVG system, including `SvgIcon` and related reusable controls.

- Prefer simple outline or glyph-style icons.
- Maintain consistent stroke weight, optical size, and alignment.
- Use recognizable Windows and media metaphors.
- Use filled icons selectively for active states.
- Standard navigation icon size: 18–20 px.
- Standard compact action icon size: 16–18 px.
- Standard player icon size: 20–24 px within a larger target.
- Do not mix several unrelated icon families or visual styles.
- Provide tooltips and accessible names for icon-only controls.

## 19. Interaction States

Every interactive component should define:

| State | Visual guidance |
|---|---|
| Default | Neutral surface, readable content, and stable shape |
| Hover | Small tonal lift, border change, or icon/text emphasis |
| Pressed | Slightly darker or compressed response with immediate feedback |
| Selected | `SurfaceSelected`, restrained accent indicator, and clear content emphasis |
| Focused | Visible `AccentPrimary` focus ring or border independent of hover |
| Disabled | Reduced contrast with recognizable structure and no hover response |
| Loading | Stable layout with compact progress or placeholder feedback |

State transitions should be subtle but obvious. Keyboard focus must remain visible even when the pointer is elsewhere.

## 20. Motion

Motion should be subtle, fast, and responsive.

| Token | Duration | Use |
|---|---:|---|
| `MotionFast` | 120 ms | Hover, press, icon, and compact state changes |
| `MotionStandard` | 160 ms | Selection, card elevation, and common transitions |
| `MotionSurface` | 220 ms | Page, popup, and larger surface transitions |

Recommended ranges:

- Hover: approximately 100–150 ms
- Selection transitions: approximately 120–180 ms
- Page and surface transitions: approximately 180–250 ms

Prefer opacity, 2–6 px translation, and subtle scale. Use easing that settles quickly and does not overshoot noticeably.

Avoid:

- Large bouncing motion
- Long animations
- Animation on every element
- Repeated ambient movement
- Animations that block interaction
- Large animated blur or shadow effects

WPF rendering performance must remain a priority, especially around video, large lists, artwork loading, and fullscreen transitions.

## 21. Loading States

- Use skeletons for poster grids, hero metadata, and structured content when they reduce perceived layout movement.
- Use compact progress indicators for refresh, import, and background operations.
- Keep already available content visible while additional content loads.
- Use stable artwork placeholders with the same aspect ratio as final content.
- Use stable channel-logo placeholders within the same containment box as final logos.
- Avoid replacing the entire screen with a spinner when partial content can remain usable.
- Prevent placeholder-to-content transitions from changing card dimensions.

## 22. Empty and Error States

### Empty State

Use:

- One short title
- One concise explanation
- One appropriate action when useful
- Optional restrained icon or illustration

Explain what the user can do next. Do not fill empty states with decorative content that competes with the action.

### Error State

Communicate:

- What failed
- A concise user-facing explanation
- A retry or corrective action when possible

Use technical error details only where they help troubleshooting, preferably behind an expandable details action. Do not expose raw exceptions in normal user-facing surfaces.

## 23. Accessibility

All modernized surfaces must provide:

- Sufficient text, icon, border, and focus contrast
- Visible keyboard focus independent of hover and selection
- Readable default text sizes
- Tooltips or accessible names for icon-only controls
- State indicators that do not rely on color alone
- Preserved keyboard navigation and logical tab order
- Clear disabled and unavailable states
- Text trimming that retains access to the full value where necessary
- Predictable Escape, Enter, Space, and arrow-key behavior where established

Do not remove automation names or accessibility relationships during visual work.

## 24. Design Tokens

These tokens are the consolidated implementation reference. They should later be translated into centralized WPF resources and styles using types appropriate to each property.

### Colors

| Token | Value |
|---|---:|
| `BackgroundBase` | `#0B0F14` |
| `BackgroundSecondary` | `#10161F` |
| `Surface` | `#151C26` |
| `SurfaceElevated` | `#1B2430` |
| `SurfaceHover` | `#202B38` |
| `SurfaceSelected` | `#183247` |
| `BorderSubtle` | `#263241` |
| `BorderStrong` | `#3A4A5E` |
| `TextPrimary` | `#F4F7FA` |
| `TextSecondary` | `#B7C2CF` |
| `TextMuted` | `#7F8C9B` |
| `TextDisabled` | `#566271` |
| `AccentPrimary` | `#42B8F5` |
| `AccentHover` | `#62C7FA` |
| `AccentPressed` | `#2398D4` |
| `AccentSubtle` | `#1E3E52` |
| `Success` | `#55C88A` |
| `Warning` | `#E8B45C` |
| `Error` | `#F06A74` |
| `Info` | `#6AAFF6` |
| `PlayerOverlay` | `#B30A0E14` |
| `PlayerControlBackground` | `#CC151C26` |
| `PlayerProgressTrack` | `#5969798A` |
| `PlayerProgressFill` | `#42B8F5` |

### Typography

| Token | Size | Weight | Target line height |
|---|---:|---:|---:|
| `Display` | 32 px | 700 | 40 px |
| `PageTitle` | 24 px | 650–700 | 32 px |
| `SectionTitle` | 18 px | 600–650 | 26 px |
| `CardTitle` | 14 px | 600 | 20 px |
| `Body` | 14 px | 400 | 21 px |
| `BodySecondary` | 13 px | 400 | 19 px |
| `Caption` | 12 px | 500 | 17 px |
| `Metadata` | 11 px | 500 | 16 px |
| `ButtonText` | 13 px | 600 | 18 px |

### Spacing

| Token | Value |
|---|---:|
| `Space4` | 4 px |
| `Space8` | 8 px |
| `Space12` | 12 px |
| `Space16` | 16 px |
| `Space20` | 20 px |
| `Space24` | 24 px |
| `Space32` | 32 px |
| `Space40` | 40 px |
| `Space48` | 48 px |

### Corner Radii

| Token | Value |
|---|---:|
| `RadiusSmall` | 4 px |
| `RadiusMedium` | 8 px |
| `RadiusLarge` | 12 px |
| `RadiusXL` | 16 px |

### Control Heights

| Token | Value | Use |
|---|---:|---|
| `ControlHeightCompact` | 32 px | Dense utility controls |
| `ControlHeightStandard` | 36 px | Standard inputs and buttons |
| `ControlHeightProminent` | 40 px | Primary actions and prominent inputs |
| `ControlHeightPlayer` | 44 px | Player interaction targets |
| `RowHeightStandard` | 56 px | Standard library rows |
| `RowHeightRich` | 68 px | Rich channel and program rows |

### Animation Durations

| Token | Value | Use |
|---|---:|---|
| `MotionFast` | 120 ms | Hover and press feedback |
| `MotionStandard` | 160 ms | Selection and common transitions |
| `MotionSurface` | 220 ms | Popup, page, and surface transitions |

## 25. Implementation Rule

This document defines the desired visual language for WXPlayer.

It does not authorize:

- Architecture rewrites
- Playback changes
- Provider changes
- Database changes
- Framework migration

Implementation must follow:

- `AGENTS.md`
- `docs/UI_MODERNIZATION.md`
- This `docs/DESIGN_SYSTEM.md`

Modernization must be incremental and validated after each stage. Existing behavior, identifiers, handlers, bindings, accessibility relationships, control ownership, and code-behind assumptions must be preserved unless a requested task explicitly requires a behavioral change.
