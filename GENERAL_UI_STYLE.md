---
category: B
read: trigger
trigger: "before creating or changing UI (UI Toolkit, panels, tokens, USS)"
tags: [ui, style, reference]
related:
  - "[ARCHITECTURE](ARCHITECTURE.md)"
---

# GENERAL_UI_STYLE.md

The general UI design language for FantasyMayor: the global HUD layout model, design principles, visual
tokens, a component catalog, and the procedure for designing any new panel or window.

> **Read policy:** load on demand. Read this file **fully only when working on the UI / design part** of the
> project. Skip it for non-UI work.
>
> **Category:** B (Template / Reference) per `DOC_STANDARD.md` — this encodes convention, so it is **not**
> stripped. Keep it accurate and complete.
>
> **Primary reader:** a language model. Write explicit, literal rules — no implied reasoning chains.
>
> **Scope boundary:** this doc is **GENERAL** only. Per-window/panel specifics (exact content, states, data
> bindings of one concrete window) live in that window's **own** doc, which references this one. Do not put
> per-window content here. See "Per-Window Docs" below.
>
> **Living visual example:** `design-mockups/FantasyMayor-HUD.html` — the canonical HUD render (full-width
> top bar + full-width bottom panel with two sub-panels; the context sub-panel showing a selected hex+district
> with the Огляд / Будівлі / Дії tabs). It is the reference render for the layout and the skin described here.
> Open it when a verbal description is ambiguous. It renders the FILLED context state; the empty STATE is
> described in §4. (The earlier `index.html` / `main-screen.html` mockups were superseded by this render and
> removed — do NOT look for them.)
>
> **Layout drift since the render:** the render shows the resource pool in the TOP bar. The live layout moved
> resources to a permanent **LEFT-edge panel** and thinned the top bar (§4). For resource placement, §4 is
> authoritative over this render.

---

## 1. Purpose & How To Use This Doc

This doc exists so the assistant can do two things:

1. **Reproduce the style** on new panels/windows without re-deriving it — the layout model, exact tokens, a
   component catalog, and a step-by-step design procedure.
2. **Explain and advise** the user on UI/UX — the principles and trade-offs behind each rule.

Ownership: **the assistant leads UI/UX** — propose a vision proactively, do not wait to be told. The user
gives feedback. When a UI decision is unstated, pick the option this doc implies and say why.

How to use it:
- Placing anything on screen → **§4 Global HUD Layout** (where each region lives and what it may hold).
- Designing a new panel → **§5 Designing a New Panel** + pull from **§7 Component Catalog** (realized via App UI, §15).
- Styling → use the exact tokens in **§6 Visual Tokens**; never invent values.
- Building it in Unity → **§15 Component Foundation (App UI)** + **§12 Panel Construction** + **§11 UI Toolkit / USS Mapping** (and its gotchas).
- Advising the user → **§2 Design Principles** + **§3 Touchstones** + **§14 Anti-Patterns** carry the "why".

---

## 2. Design Principles

- **Map and panels for everything; agency opens as submenus.** The map and panels *show* state **and** carry
  the player's actions. An action is a **button on a panel** that opens a **submenu / detail subpanel / modal**
  (the Mayor's agency) — never a floating card. There is no read-only restriction on panels: any panel may host
  actions next to the state it shows — see §9. The **«Завершити хід»** turn-commit is just the most prominent
  such action, not a special exception.
- **UI on the edges, center always clean.** The map center is where play happens. All permanent HUD lives on
  the top edge, the bottom edge, and the **left edge** (the resource panel); the **right edge** stays free of
  permanent panels. Never cover the center with a permanent panel.
- **Progressive disclosure.** A panel is one surface that **grows in sections** with the data available. An
  always-present part first, then conditional sections appended only when their data exists. **Never render an
  empty section** as a placeholder — omit it (the bottom panel's empty STATE is a deliberate, designed
  exception, see §4).
- **Glanceability + predictable placement.** A surface the player uses constantly must appear in the **same
  place every time** so it becomes muscle memory. Predictability beats flexibility for permanent HUD.
- **Cozy-fantasy tone.** Warm dark surface + gold accent. **Not** cold steel / sci-fi. The game tone is
  `cozy`; the UI must read warm and human, not industrial.
- **Color encodes meaning, not decoration.** A color always means the same thing (a role, a resource, a
  status). Do not reuse a role color for an unrelated accent, and do not recolor the same role per panel.

---

## 3. Reference Touchstones

We borrow SYSTEMS and information architecture from these games, then apply our own warm/gold **cozy** skin
(§6) at the intimate scale of a single city. **Never import a reference's mood** — all of these are
empire-scale or cold-industrial; tone and scale are OURS.

- **Old World — primary lens for interaction.** The closest structural twin to our core loop. Its **Orders**
  economy (a small per-turn pool spent on actions) ≈ our **Action Points** scarcity; its **event / character
  interactions** ≈ the **Mayor's agency + Important Citizens**. **When an interaction is unspecified, resolve it
  the Old World way:** turn-based, character-centric, readable. We keep its systems (AP economy, character-driven
  events); we render them as **panels + submenus**, not cards.
- **Shadow Empire — adopted for the bottom panel.** Its model of a **wide bottom panel with tabs, where a tab
  drills into a full-screen window**, is our chosen shape for the contextual bottom panel and for deep
  management views (FantasyMayor is management-heavy; a wide tabbed hub fits better than Old World's narrow
  side list). **Drop** its dense, hard-to-read UI — we keep the architecture, not the clutter.
- **Frostpunk 2** — faction pressure, district panels with prominent **workforce**, strong sectioning. Borrow
  for the political layer and the district panel. **Drop** its cold industrial palette.
- **Civilization 6** — tile/hex **yields shown as icon + number**. Borrow the compact icon+value yield
  language.
- **Endless Legend** — ornate fantasy framing and per-tile breakdowns. Borrow the warm fantasy feel.
- **Mind Over Magic / Farthest Frontier** — a compact resource strip with hover **drill-down**, and the
  principle that not everything lives on the permanent HUD — secondary state opens **on demand**. Borrow for
  the top resource strip and the global window openers.

---

## 4. Global HUD Layout

This is the canonical map of the permanent HUD. Every new UI element must be placed into one of these
regions. The render reference is `design-mockups/FantasyMayor-HUD.html`.

```
┌─────────────────────────────────────────────────────────────┐
│  TOP BAR (full width, thin)  [global windows]  [system icons] │
│ ┌────────┐                                                    │
│ │RESOURCE│                                                    │
│ │ PANEL  │              MAP — center stays clean              │
│ │ (left  │                                                    │
│ │  edge) │                                                    │
│ │City/Мер│                                                    │
│ └────────┘                                                    │
│  ┌──────────────┬──────────────────────────────────────────┐ │
│  │ TURN          │  CONTEXT — selected hex / district        │ │
│  │ (state+commit)│  (tabs → full window)                     │ │
│  └──────────────┴──────────────────────────────────────────┘ │
│  BOTTOM PANEL (full width, two sub-panels)                    │
└─────────────────────────────────────────────────────────────┘
```

### Regions

- **Top bar — full width, permanent, THIN.** Two zones (the resource pool moved OUT to the left panel, so the
  bar is now a thin strip):
  - **left — global window openers.** Buttons that open read-only full-screen state windows (Огляд,
    Населення, Економіка, Райони, …). These are global (not tied to a selection).
  - **right — system icons.** Events (with a count badge) and settings. **No season / turn-calendar here** —
    season and phase are not shown anywhere on the HUD.
- **Left edge — resource panel, permanent.** The two inventory pools (City `Місто` / Mayor `Мер`) as a
  **vertical scroll list**: one row per resource (`icon + City value + Mayor value`, role-colored — city blue,
  mayor gold), with a static owner header (`Місто` / `Мер`) above the scroll area. The icon is the unified
  square plate (§7). Resources-only: the Mayor's AP / leadership stats live in the turn sub-panel, not here.
  This panel eats map WIDTH (not height), so it sits outside the vertical HUD-height budget.
- **Bottom panel — full width, permanent, two sub-panels** divided by a vertical divider:
  - **left sub-panel — turn (state + commit).** «Хід N», two AP tiles (`Дії зараз` / `наст. хід`), and the
    **«Завершити хід»** button pinned to the bottom (`margin-top: auto`). This is the player's turn corner;
    the End Turn button is the single hero CTA on the whole HUD. **No season / phase here.**
  - **right sub-panel — context.** The selected hex / district detail, organized as a **tab row**
    (Огляд / Будівлі / Дії) with a trailing faint drill-in `↗ повне вікно району`. Tab content is a set of
    **discrete bordered blocks** (the Shadow Empire density lens, see §7) — stat-tiles and key–value tables,
    **never** progress bars / sliders. Canonical Огляд blocks: **ГЕКС** (terrain icon + type as a compact
    line + resource chips), **РАЙОН** (serif name + level + owner / operator / buildings / specialization
    k-v), **ПРАЦЯ ТА ВИРОБНИЦТВО** (one labor line + production table). The **Дії** tab hosts the selected
    district's actions as a **list of action buttons** (icon + name + AP cost); clicking one opens its
    submenu / detail subpanel (§9).
- **Left edge — the resource panel** (see above). **Right edge — free.** No permanent panel on the right.
  (There is NO right-side hex inspector — its role is the bottom panel's right sub-panel.)
- **Center — the map.** Never covered by a permanent panel.

### Region roles

- **Top bar = global state + global openers.** **Bottom-left = turn state + the turn commit.**
  **Bottom-right = contextual state + the selection's actions.**
- **Agency opens as submenus (§9).** A panel hosts its actions as **buttons** beside the state it shows;
  clicking one opens a submenu / detail subpanel / modal. The selected district's actions live as a button
  list in the context sub-panel's **Дії** tab; the Mayor's global actions open from a top-bar opener. Inline
  action buttons and docked action openers are the normal shape of agency — nothing here is banned except
  cards (there are none).

### The bottom panel is ALWAYS present

The bottom panel never appears/disappears — predictable placement. Its two sub-panels behave differently when
nothing is selected:

- **left sub-panel (turn):** unchanged — always shows turn state + End Turn.
- **right sub-panel (context):** shows an **empty STATE** — a centered cozy placeholder ("Виберіть гекс на
  мапі") with the district tabs kept visible but faint, so the player sees *where* info will appear. This is a
  deliberate exception to "never render an empty section": the panel SHELL is permanent for muscle memory; only
  its CONTENT is contextual.

### Full-screen windows

Deep state lives in **full-screen windows** opened from the top bar (global) or a bottom-panel tab
(contextual). These are allowed and expected (Shadow Empire drill-down). A window may be read-only (a state
breakdown) or carry its own actions (buttons → submenus, §9). A full-screen window may cover the map because the
player deliberately opened it and is not looking at the map then; the "keep the center clean" rule governs the
*permanent* HUD, not an opened window.

---

## 5. Designing a New Panel — Procedure

Run this every time a new panel/window is requested.

1. **Place it first (§4).** Decide which region owns it: top-bar zone, a bottom-panel sub-panel, or an
   on-demand window (global from the top bar, contextual from a bottom tab). If it is agency, it is an
   action panel / submenu / modal (§9), opened from a button on its host panel.
2. **Identify the entity and its data layers.** Separate what is *always present* from what is *conditional*
   (e.g. hex: terrain always; resource sometimes; district sometimes).
3. **Order by progressive disclosure.** Always-on header first. Then conditional sections, appended only when
   their data exists, ordered from most-general to most-specific.
4. **Map each datum to a component pattern** (§7):
   - an enumerable set (resources, tags) → **chips**
   - a single attribute (owner, action) → **key–value row**
   - a status / badge → **pill**
   - a multi-good distribution (production split City / Owner / Operator) → **production table** (row per
     good, role-colored share columns) — NOT a stacked split bar
   - a labor-limited output → state the limit ONCE on the **labor line** (`N / M груп · X% · виробництво
     обмежено`, amber when short); do not also strike through each produced value
5. **Apply tokens** from §6. Color must encode meaning (role / resource / status), never decoration.
6. **Place actions as buttons → submenus.** A panel may host actions next to its state. Render each action as a
   button that opens a submenu / detail subpanel / modal (§9) — never a floating card.
7. **Write the per-window doc** (§13): reference this file for tokens/components/layout, specify only the new
   window's content model, states, and data bindings.

---

## 6. Visual Tokens

These are canonical. Use them verbatim. The values mirror `design-mockups/FantasyMayor-HUD.html`.

### Color

**Surfaces & structure**
- `--panel-bg: rgba(30, 25, 20, 0.93)` — panel background, warm near-black, translucent
- `--panel-border: rgba(217, 164, 65, 0.28)` — gold-tinted hairline border
- `--divider: rgba(217, 164, 65, 0.14)` — internal section dividers
- `--block-border: rgba(217, 164, 65, 0.16)` — outline of a discrete context-tab block (Shadow Empire lens)
- `--chip-bg: rgba(255, 255, 255, 0.05)` — chip / inset fill

**Text tiers** (three levels, never more)
- `--txt: #f1e7d6` — primary, warm off-white (titles, values)
- `--txt-dim: #a89a86` — secondary (row labels)
- `--txt-faint: #7d7264` — tertiary (section labels, captions)

**Accent**
- `--gold: #e0b25a` — primary accent (title emphasis, level, AP cost, turn number)
- `--gold-soft: #d9a441` — accent line / hover / the End Turn button fill

**Resource palette** (one hue per resource; reuse wherever that resource appears)
- forest `#6fa05b` · clay `#c2784f` · fish `#5b9ab0` · stone `#9b9387` · ore `#d6a44a`

**Role / share palette** (color = the recipient role; reuse everywhere a role or its share appears)
- city `#5b8bb0` (blue) · owner `#e0b25a` (gold) · operator `#6fa05b` (green)

**Status**
- positive / active: text `#bfe0a8` on `rgba(111, 160, 91, 0.20)`, border `rgba(111, 160, 91, 0.35)`
- warning / shortage: text `#e8a96a` on `rgba(214, 138, 74, 0.18)`, border `rgba(214, 138, 74, 0.40)`

### Typography

Two families, by role:
- **Serif** — titles and proper names (panel title, district name, turn number). Conveys the fantasy register.
- **Sans** — everything else (labels, data, captions). Conveys legibility for dense numbers.

Scale (px) and weight:
- panel title / turn number — 20–22, serif, weight 600
- proper name (e.g. district) — 15–16, serif, weight 600
- emphasis value (e.g. headline yield) — 15, sans
- body / rows — 12–13, sans, weight 400; values weight 500
- section label — 11, sans, **UPPERCASE**, letter-spacing ~1.4px, color `--txt-faint`
- caption — 11–12, sans, color `--txt-faint`

### Spacing, Radius, Elevation

- **Padding:** header `12px 16px`; section `13px 16px`; key–value row `4px–7px 0`.
- **Gaps:** chip row `8px`; header icon→text `10–14px`.
- **Radius:** panel `13–14px`; context-tab block `9px`; terrain icon `9–10px`; pill `10px`; chip
  `20px` (full-round); swatch `3px`.
- **Elevation:** drop shadow `0 10px 30px rgba(0,0,0,0.45)` + inset top highlight
  `inset 0 1px 0 rgba(255,255,255,0.04)`; panel backdrop blur `6px` (mockup only — see §11 USS gotchas).
- **Motif:** a 3px gold gradient **accent line** along a floating panel's top edge (the bottom panel and
  full-screen windows). The top bar omits it.

---

## 7. Component Catalog

Each entry: **use for** / **structure** / **anti-pattern**. Build new panels from these, not from scratch.

### Panel shell
- **Use for:** the outer container of any floating panel or window.
- **Structure:** rounded (`13–14px`), `--panel-bg`, `--panel-border`, top accent line, drop shadow; a stack of
  header + sections separated by dividers; `overflow: hidden` so children clip to the radius.
- **Anti-pattern:** a square, opaque, shadowless box — reads as a debug overlay, not game UI.

### Sub-panel (split panel)
- **Use for:** the bottom panel — one shell split into two sub-panels by a vertical divider (turn | context).
- **Structure:** a single panel shell; an internal `border-right`/`border-left` `--divider` separates the
  sub-panels; each sub-panel owns its own header/sections.
- **Anti-pattern:** two separate floating panels pretending to be one — keep it a single shell so the radius,
  accent line, and elevation are unified.

### Section
- **Use for:** one logical group inside a panel.
- **Structure:** an UPPERCASE `--txt-faint` label, then the section body; `13px 16px` padding; a `--divider`
  separates it from the previous section.
- **Bordered-block variant (context tabs):** inside the context sub-panel's tab content, sections become
  **discrete bordered blocks** — own thin gold outline (`--block-border`, radius 9px) + UPPERCASE label —
  packed densely (Shadow Empire lens). Each block must be **densely filled**, no half-empty block with air at
  the bottom. Use this variant inside the context tabs; use divider-separated sections elsewhere.
- **Anti-pattern:** rendering the section when it has no data (omit it — except the bottom panel's empty STATE).

### Header
- **Use for:** the always-present identity row at the top of a panel or block.
- **Structure:** square icon (terrain/entity sprite) + name. **Serif is reserved for proper names**
  (the district name); a terrain TYPE is a compact sans line, not a big serif headline. No coordinate caption.
- **Anti-pattern:** burying the entity's identity below other data; rendering a hex's terrain type as a large
  serif headline (that weight is for the district name).

### Tabs
- **Use for:** switching views inside the context sub-panel (Огляд / Будівлі / Дії; Shadow Empire model);
  a tab may drill into a full window via the trailing `↗ повне вікно району`.
- **Structure:** a horizontal row of text tabs; the active tab carries a 2px gold bottom border (`--gold`);
  a trailing faint `↗ повне вікно району` affordance signals the full-screen drill-down.
- **Anti-pattern:** a tab that fires an action itself (a tab only switches the view). Actions live as buttons
  inside the tab's content (e.g. the Дії tab), not on the tab control.

### Divider
- **Use for:** separating sections (horizontal) or sub-panels (vertical).
- **Structure:** 1px `--divider`, inset to match padding.

### Chips
- **Use for:** an **enumerable set** of small items (hex resources, tags).
- **Structure:** full-round pill, `--chip-bg`, a colored icon dot (resource hue) + label; wraps to multiple
  rows.
- **Anti-pattern:** using chips for a single key attribute (use a key–value row) or for an action (use an action button → submenu, §9).

### Key–Value row
- **Use for:** a single attribute of the entity (owner, operator, active action, workforce, season, phase).
- **Structure:** label left (`--txt-dim`), value right (`--txt`, weight 500). Role values carry the role color
  (city blue, operator green). Group related rows into columns when horizontal room allows.
- **Anti-pattern:** packing a multi-good distribution into one row (use a production table).

### Pills
- **Use for:** compact status badges inside a value.
- **Variants:** `active` (positive/green), `cost` (gold, e.g. `2 AP`), `warning` (amber, e.g. labor `75%`).
- **Anti-pattern:** a clickable-looking pill on a state panel (it implies an action — it must not be one).

### Role identity
- **Use for:** showing who fills a role (Owner / Operator: City / Mayor / Citizen).
- **Structure:** small round avatar/initial or role icon + name; the role's color (§6) where a swatch/accent
  is shown.
- **Anti-pattern:** showing the same role in different colors across panels.

### Production table
- **Use for:** a district's **production split** across recipients (City / Owner / Operator), possibly for
  **several goods** at once.
- **Structure:** a compact table — one **row per good** (`icon + name + total`) and three role-colored
  columns (`Місто` blue / `Власн.` gold / `Опер.` green) holding each recipient's signed share; a small role
  swatch heads each column. No stacked / segmented bar.
- **Anti-pattern:** a stacked split-bar (rejected — does not scale to multiple goods and reads as decoration);
  using the word «вихід» (the term is **«виробництво»**).

### Labor line
- **Use for:** the single labor / utilization readout of a district (replaces the old struck-through
  "value-with-correction" — the limit is stated ONCE, here, not on each produced value).
- **Structure:** one line — `N / M груп · X% · виробництво обмежено` — in an **amber** bordered inset
  (`--warn`) when labor is short, plain when fully staffed. This is the single place the shortage and its
  effect on production are stated.
- **Anti-pattern:** splitting labor into two readouts («групи праці» + «завантаження» — the same fact twice);
  ALSO striking through each produced value with `потенціал ~~+8~~ −2` (the labor line already said why).

---

## 8. Panel-Level Layout Rules

Region placement is §4. These are the per-panel rules once a panel is placed.

- **Permanent panels are not draggable.** Dragging adds state and unpredictability with no benefit.
- **Cap height** and **scroll the growing block** internally when a panel/window gets tall. Never let it run
  off-screen.
- **The bottom panel sub-panels stay aligned to the bottom edge.** The two sub-panels share one shell and a
  common bottom; the turn sub-panel pins «Завершити хід» to the bottom with `margin-top: auto`.
- **Full-screen windows** are centered/edge-to-edge as their content needs, opened and closed deliberately;
  they are the only surfaces permitted to cover the center, and only while open.

---

## 9. Interaction Model

- **Panels carry both state and actions.** There is no read-only restriction. A panel shows its entity's state
  and may host that entity's **actions** beside it.
- **An action is a button → submenu.** Player agency (build, negotiate, invest, intervene) is rendered as a
  **button on a panel** that opens a **submenu / detail subpanel / modal**. There are no cards or decks
  anywhere in the game.
- A district's actions live as a button list in the context sub-panel's **Дії** tab; the Mayor's global actions
  open from a top-bar opener. The **«Завершити хід»** turn-commit is the most prominent action button, not a
  special case.
- A pill/badge that is purely a status must not be clickable — make the affordance an explicit action button,
  so "this is clickable" is never ambiguous.

### Agency archetypes (Old World lens)

Player agency uses **two standardized compositions**, both **panels** (opened from an action button). Both obey
the Old World rule: **every choice shows its consequence (gain/loss icons) and its AP cost** — the player never
commits blind.

- **Action panel — master–detail.** A list of actions (icon + name + AP cost) on the left; the selected
  action's detail on the right (title, what it does, effects, requirements, a confirm button carrying the AP
  cost). Use for: the Mayor's free actions under the AP budget, or any "pick one of several actions" choice
  (e.g. **pick a district to build** — the «+» district affordance opens this). **Anti-pattern:** a card
  fan/hand — there are no cards.
- **Event decision — focused modal.** A single forced decision as a **modal panel-dialog**: a
  portrait/illustration, the situation narrative, then a vertical list of choices, each with its consequences
  + AP cost. Use for: system events, requests, end-of-turn consequences — one at a time. **Anti-pattern:**
  burying a forced decision inside a list where it can be skipped.

**Negotiation is NOT one of these.** Negotiating with an Important Citizen is a separate, more complex
mechanic with its **own bespoke window** — design it on its own terms when that mechanic lands.

### Full-screen state windows

Deep STATE (full economy, full population, a district's full breakdown) opens as a full-screen window from a
top-bar opener or a bottom-panel tab. These are primarily for reading state, but — like any panel — may carry
their own action buttons (§9). They differ from the focused agency archetypes above by being broad, browsable
breakdowns rather than a single decision.

---

## 10. Microcopy & Numbers

- **Section labels:** UPPERCASE, short, noun phrase (`РЕСУРСИ`, `ПРАЦЯ ТА ВИРОБНИЦТВО`).
- **Term is «виробництво», never «вихід».** Remove «вихід» from every UI string.
- **Signed quantities:** always show the sign for deltas/yields — `+6`, `−2`.
- **Resource amounts:** pair the number with the resource icon; keep icon/number order consistent within a
  given context (headline: icon → `+N` → unit name; dense rows: `+N` → icon → `%`).
- **Percentages:** shown alongside each share in a breakdown.
- **Labor-limited production:** state the limit ONCE on the labor line (`N / M груп · X% · виробництво
  обмежено`); do not strike through each produced value.
- **In-game strings are Ukrainian** (the player-facing language). Keep this doc's prose English (matches the
  repo's reference docs), but use real Ukrainian strings in examples.

---

## 11. UI Toolkit / USS Mapping

The game renders UI in **UI Toolkit** (UXML + USS) on **Unity 6.4**, with **Unity App UI** as the component
foundation (§15). USS is a **subset** of CSS. **Author mockups already inside that subset** — a mockup must be a
faithful preview of what Unity will render, not aspirational CSS to be "mapped later". Stay within the techniques
below from the start; the only exception is the rendered game scene (camera output behind the panels), which is
not UI Toolkit. The capability notes below are verified against Unity 6.4's USS.

Tokens become **USS custom properties**, assigned on a root/theme selector and read with `var()`:

```css
/* USS — define on a root class, e.g. a shared theme element */
:root {
  --panel-bg: rgba(30, 25, 20, 0.93);
  --panel-border: rgba(217, 164, 65, 0.28);
  --divider: rgba(217, 164, 65, 0.14);
  --block-border: rgba(217, 164, 65, 0.16);
  --txt: #f1e7d6;
  --txt-dim: #a89a86;
  --txt-faint: #7d7264;
  --gold: #e0b25a;
  --gold-soft: #d9a441;
  /* resources */
  --forest: #6fa05b; --clay: #c2784f; --fish: #5b9ab0; --stone: #9b9387; --ore: #d6a44a;
  /* roles / shares */
  --share-city: #5b8bb0; --share-owner: #e0b25a; --share-oper: #6fa05b;
}
.panel { background-color: var(--panel-bg); border-color: var(--panel-border); }
```

### CSS → USS gotchas (do not translate these literally)

| Mockup CSS | USS reality | Do instead |
|---|---|---|
| `::before` / `::after` (accent line, hex texture) | USS has **no pseudo-elements** | Add a real child `VisualElement` (e.g. a 3px accent bar) or a background image |
| `box-shadow` (elevation) | **Not supported** in USS (still absent in 6.4; App UI fakes it too) | 9-slice sprite or a Vector Graphics (SVG) asset; or a subtle border; or omit. `text-shadow` covers TEXT glow only |
| `backdrop-filter: blur()` | **Not supported** | Rely on `--panel-bg` alpha alone (no live blur) |
| `text-transform: uppercase` (section labels) | **Not supported** | Uppercase the **string** in data/binding |
| `linear-gradient(...)` background (accent line) | **Not supported** in USS (verified through Unity 6.4) | A Vector Graphics (SVG) gradient asset, a 9-slice / gradient sprite, or a solid `--gold-soft` element |
| gradient **border** | not supported in USS at all | per-side border colors (lighter top/left, darker bottom/right) for a faux bevel; or a 9-slice sprite |
| `font-family` stacks (serif/sans) | USS uses **font assets** | Assign a serif and a sans **font asset** via `-unity-font-definition` |
| layout (grid) | UI Toolkit is **flexbox-only** | Use flex; there is no CSS grid |
| flex `gap` / `row-gap` / `column-gap` | **Not supported** (Yoga subset; absent from the USS supported-properties list) | Space items with `margin` |
| structural selectors `:last-child` / `:first-child` / `:nth-child` / `:not()` | **Not supported** (USS has type/class/name/`*`/`>`/descendant + pseudo-**states** only) | Add a marker class, or accept trailing margin absorbed by padding |
| implicit `flex-direction: row` | USS default is **`column`**, not `row` | Set `flex-direction` explicitly on every flex container |
| `position: fixed` (anchoring) | No `fixed` | Anchor with `position: absolute` inside a full-screen root element |
| rounded clipping of children | Children overflow the radius by default | Set `overflow: hidden` on the rounded parent |

**Verified supported in USS (Unity 6.4):** `letter-spacing`, `word-spacing`, `text-shadow` (+ offset-x / offset-y /
blur-radius / color; SDF fonts only), `-unity-text-outline`, `opacity`, `rotate` / `scale` / `translate` and
`transition` (animate hover / active / processing states), the full `background-*` set (`background-position` /
`-repeat` / `-size`, `-unity-background-image-tint-color`, `-unity-background-scale-mode`), and `-unity-slice-*`
(9-slice). There is **no** `text-transform` — uppercase the string in data. When in doubt about a USS feature,
verify against the project's Unity version (the official USS reference) rather than assuming CSS parity.

---

## 12. Panel Construction (UI Toolkit)

How to assemble any panel. This is the default; deviate only with a stated reason.

- **Build from App UI components (§15), reskinned.** Where App UI provides a control (button, dropdown, modal,
  popover, menu, list, tooltip), use it — do not re-author it from raw `VisualElement`s. The whole Main UI tree
  lives under one `appui:Panel` root (it provides the theme + the popup/tooltip layer). Author bespoke markup
  only for structure App UI has no control for (the HUD shell, the discrete context blocks).
- **Author the full skeleton in one UXML.** All *known* blocks, in canonical (progressive-disclosure) order.
  The skeleton is the single source of visual structure. The bottom panel is **one shell with two pre-authored
  sub-panels** (turn + context).
- **One subsystem per block.** Each block has its own ECS system that (1) reads its slice of the relevant
  state, (2) populates its block's content, (3) toggles the block's visibility. The two bottom sub-panels are
  driven by independent systems (turn state vs selection context).
- **Show/hide via `display`.** Data present → `display: flex`; absent → `display: none` (removes from layout so
  the panel collapses). **Never** use `visibility: hidden` (it leaves an empty gap). Exception: the bottom
  panel SHELL is always `display: flex`; only its context CONTENT swaps between the filled blocks and the empty
  placeholder.
- **Do NOT insert blocks dynamically.** A subsystem toggles a *pre-authored* container; it does not create or
  insert the block at runtime. Exception: a genuinely open-ended, unknown-at-author-time set of block *types*.
- **Dynamic instantiation only for variable child lists** (resource chips, a buildings list). Build those
  children from a small item template and **pool** them to avoid per-update GC.
- **A root/controller system owns the shared instance.** It loads the panel (addressables), owns the instance
  and handle (dispose per `ADDRESSABLE_PATTERNS.md`), and shows/hides the whole panel. Block systems own only
  their own block. (Current idiom: `MainUISpawnSystem` instantiates one `UI/MainUI` prefab; spawn subsystems
  resolve their view off it — see `Assets/Presentation/UI/MAIN_UI.md`.)
- **One shared panel instance, not per-entity.** Selection is singular, so the context panel is reused.
- **Absence is not an error; a missing prerequisite is.** An optional block with no data → hide it (normal). A
  required prerequisite that must always exist → throw, per `ECS_CONVENTIONS.md` fail-loud.
- **Placeholder / SCAFFOLD blocks.** A block whose backing ECS components do not exist yet is driven by a
  **placeholder system** that supplies stub data or keeps the block hidden, until the real components land.
  Mark such blocks `SCAFFOLD` in the window's own doc (current SCAFFOLD: the District block and the
  production table — both wait on the District-economy data).

---

## 13. Per-Window Docs

This file is the **general** language. Every concrete window/panel gets its **own** MD doc that:

- **references this file** for tokens, components, layout regions, principles, and construction;
- specifies **only** that window's own concerns: its content model, its progressive-disclosure states, and its
  data bindings (which entity components feed which rows);
- does **not** restate tokens, the component catalog, or principles.

**Rule:** every **new** UI window gets its own design doc. The current TerrainGenerator / generation overlay is
**temporary UI** — do not write a design doc for it and do not treat it as a style reference.

Current per-window docs live under `Assets/Presentation/UI/*` (e.g. `HexInfoPanel/HEX_INFO_PANEL.md`,
`EndTurn/END_TURN.md`). NOTE: these docs describe the CURRENT implementation, which predates the §4 layout
model above (the End Turn cluster and hex panel are not yet merged into one bottom panel). They are updated
when the new layout is actually built — this file describes the TARGET, the per-window docs describe what
exists today.

---

## 14. Global Anti-Patterns

| Wrong | Why | Right |
|---|---|---|
| Put a permanent panel on the RIGHT edge or over the center | Eats the map; breaks "UI on the edges" | Top bar, bottom panel, the left resource panel, or an on-demand window |
| Render a conditional section with no data | Dead space; breaks progressive disclosure | Omit it (except the bottom panel's designed empty STATE) |
| Make the bottom panel appear/disappear on selection | Layout jump; loses the muscle-memory anchor | Keep the shell permanent; swap only the context CONTENT |
| Use a card / card-deck / card-fan for any agency | Cards are removed from the design | Action button → submenu / detail subpanel / modal (§9) |
| Make an action open as a floating standalone card | No cards; breaks the panels+submenus model | Open it as a submenu/subpanel of its host panel, or a focused modal |
| Strike through each produced value (`потенціал ~~+8~~ −2`) | Duplicates the cause already on the labor line | State the shortage ONCE on the labor line |
| Use the word «вихід» in any UI string | Wrong term | Use «виробництво» |
| A stacked split-bar for the production split | Does not scale to multiple goods; reads as decoration | Production table (row per good, role columns) |
| Show season / phase anywhere on the HUD | Removed from the HUD this pass | Omit it — no season/phase on the top bar or the turn corner |
| Invent a new accent color per panel | Breaks visual unity | Reuse the token palette |
| Recolor the same role across panels | Breaks the role-color language | city=blue, owner=gold, operator=green everywhere |
| Cold / steel palette | Wrong tone for a `cozy` game | Warm dark + gold |
| Two floating boxes faking one bottom panel | Breaks the unified shell | One shell, vertical divider, two sub-panels |
| Restate a window's content here | This doc is GENERAL | Put it in that window's own doc |
| Translate `box-shadow` / `::before` / blur straight to USS | USS does not support them | Use the §11 gotcha workarounds |
| Insert each block dynamically per subsystem | Loses authored order; churns GC | Author the skeleton; toggle `display: none` (§12) |
| Rebuild an overlay / list / menu / dropdown from raw elements when App UI has it | Reinvents a maintained, themed, accessible control | Use the App UI control (§15), reskinned via `appui--cozy` |
| Expect App UI to add `box-shadow` / gradients | App UI is components + theming, not new pixel effects | Gradients/elevation via Vector Graphics (SVG) or 9-slice (§11, §15) |
| Hide a block with `visibility: hidden` | Leaves an empty gap | Use `display: none` — it collapses layout |

---

## 15. Component Foundation — Unity App UI

The project's component layer is **Unity App UI** (`com.unity.dt.app-ui`, already a dependency). App UI is the
official Unity design system **on top of** UI Toolkit: a maintained library of controls + a theming system + an
overlay/focus/layering layer. It is the **DEFAULT source of controls** — do not rebuild a control App UI already
provides.

### Non-negotiables
- **All App UI markup lives under one `appui:Panel` root.** The Panel propagates the theme, language, and layout
  direction, and owns the layering for popups / menus / tooltips / toasts. No Panel → components render wrong.
  UXML declares the namespace `xmlns:appui="Unity.AppUI.UI"`; controls are `<appui:Button title="…" />`, etc.
- **No MVVM.** We do NOT use App UI's MVVM app-builder. Components are plain `VisualElement`s; our **ECS systems
  drive them** (resolve by name, write via the control's API) exactly like the current views. App UI's
  MVVM / state / localization layers are out of scope unless explicitly adopted later.
- **Reskin via one theme, `appui--cozy`.** App UI ships Material-ish defaults. Map App UI's design tokens
  (`--appui-*`, e.g. `--appui-primary-100`, `--appui-spacing-100`) onto OUR tokens (§6) in a single
  `.appui--cozy` theme (`.tss` referencing a `.uss`). Per-control tweaks override the control class
  (`.appui-button { … }`). Our tokens (§6) stay the source of truth; the cozy theme is the bridge.

### Use App UI for
- **overlays:** Modal / Dialog / AlertDialog, Popover, Tray, Drawer — our submenus and the district-build modal;
- **navigation / commands:** Menu / ContextMenu, Tabs — the context tab row, action menus;
- **inputs:** Button / ActionButton / IconButton, Dropdown / Picker, Slider, Toggle, TextField, Stepper;
- **data:** ListView / GridView (virtualized) — action lists, building lists, the picker's master list;
- **feedback / decor:** Toast / Notification, Tooltip, ProgressBar, Chip, Badge, Avatar, Divider, Icon.

App UI's **Components manual is the canonical catalog** — confirm the exact control exists there before authoring.

### App UI does NOT change the USS limits (§11)
App UI is components + theming, not new pixel effects. `box-shadow`, background gradients, and `backdrop-filter`
are STILL absent (App UI itself fakes elevation with sprites). The cozy elevation, the gold accent line, soft
glows, and gradient fills are produced with **Vector Graphics (SVG, `com.unity.modules.vectorgraphics`) or
9-slice sprites**; text glow uses `text-shadow` (§11). Do not expect App UI to supply these.

### Bespoke vs App UI
- **Bespoke (hand-authored UXML/USS):** the HUD shell + region layout (top bar, the one bottom-panel shell with
  its two sub-panels, the discrete context blocks) — App UI has no "our HUD" control.
- **App UI:** every actual control inside that layout. About to write a button, list, dropdown, menu, popover,
  modal, or tooltip from raw elements? Stop — use App UI.
