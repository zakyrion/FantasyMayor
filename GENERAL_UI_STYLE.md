---
category: B
read: trigger
trigger: "before creating or changing UI (UI Toolkit, panels, tokens, USS)"
tags: [ui, style, reference]
related:
  - "[MAIN_UI](Assets/Modules/MainUI/MAIN_UI.md)"
  - "[HEX_INFO_PANEL](Assets/Modules/MainUI/HexInfoPanel/HEX_INFO_PANEL.md)"
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
- Designing a new panel → **§5 Designing a New Panel** + pull from **§7 Component Catalog**.
- Styling → use the exact tokens in **§6 Visual Tokens**; never invent values.
- Building it in Unity → **§12 Panel Construction** + **§11 UI Toolkit / USS Mapping** (and its gotchas).
- Advising the user → **§2 Design Principles** + **§3 Touchstones** + **§14 Anti-Patterns** carry the "why".

---

## 2. Design Principles

- **Map and panels for state, cards for agency.** Panels (and the map) only *show* state. Player *actions*
  are cards (the Mayor's agency). A state panel therefore carries **no action buttons** — see §9. The one
  permitted button on the permanent HUD is **«Завершити хід»** (the turn commit), not a domain action.
- **UI on the edges, center always clean.** The map center is where play happens. All permanent HUD lives on
  the top edge and the bottom edge; the left and right edges stay free of permanent panels. Never cover the
  center with a permanent panel.
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
  cards** ≈ the **Mayor's agency + Important Citizens**. **When an interaction is unspecified, resolve it the
  Old World way:** turn-based, card-driven, character-centric, readable.
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
│  TOP BAR (full width)                                         │
│  [global windows]      [resource pool]      [system icons]    │
│                                                               │
│                                                               │
│                     MAP — center stays clean                  │
│                                                               │
│                                                               │
│  ┌──────────────┬──────────────────────────────────────────┐ │
│  │ TURN          │  CONTEXT — selected hex / district        │ │
│  │ (state+commit)│  (tabs → full window)                     │ │
│  └──────────────┴──────────────────────────────────────────┘ │
│  BOTTOM PANEL (full width, two sub-panels)                    │
└─────────────────────────────────────────────────────────────┘
```

### Regions

- **Top bar — full width, permanent.** Three zones:
  - **left — global window openers.** Buttons that open read-only full-screen state windows (Огляд,
    Населення, Економіка, Райони, …). These are global (not tied to a selection).
  - **center — resource pool, as an owner×resource matrix.** Rows = the pool owners (Мер, Місто); columns =
    the shared resource set (золото, їжа, колоди, дошки, …) as `icon + value`, with the Мер row also carrying
    its AP (⚖️) and a second leadership stat (🔥). Each owner reads its own value in the same column; values
    are role-colored where a role owns them. A trailing `⋯` expander opens the fuller resource window.
  - **right — system icons.** Events (with a count badge) and settings. **No season / turn-calendar here** —
    season and phase are not shown anywhere on the HUD.
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
    district's action **cards** (icon + name + AP cost) — agency stays card-shaped (§9), not inline buttons.
- **Left edge / right edge — free.** No permanent panels. (There is NO right-side hex inspector — its role is
  the bottom panel's right sub-panel.)
- **Center — the map.** Never covered by a permanent panel.

### State / agency mapping (the discipline line)

- **Top bar = global state.** **Bottom-left = turn state + the turn commit.** **Bottom-right = contextual
  state.** All read-only except «Завершити хід».
- **Agency stays card-shaped (§9).** The selected district's actions appear as **cards** in the context
  sub-panel's **Дії** tab (icon + name + AP cost). The Mayor's global free-action deck still opens as an
  on-demand window (§9). What stays banned: an inline action **button** on a state block, and a permanent
  «Дії мера» button docked in the HUD chrome — agency is never an inline state-panel control.

### The bottom panel is ALWAYS present

The bottom panel never appears/disappears — predictable placement. Its two sub-panels behave differently when
nothing is selected:

- **left sub-panel (turn):** unchanged — always shows turn state + End Turn.
- **right sub-panel (context):** shows an **empty STATE** — a centered cozy placeholder ("Виберіть гекс на
  мапі") with the district tabs kept visible but faint, so the player sees *where* info will appear. This is a
  deliberate exception to "never render an empty section": the panel SHELL is permanent for muscle memory; only
  its CONTENT is contextual.

### Full-screen windows

Deep state lives in **read-only full-screen windows** opened from the top bar (global) or a bottom-panel tab
(contextual). These are allowed and expected (Shadow Empire drill-down). They are NOT modal management dialogs
for agency — agency stays card/deck-shaped (§9). A full-screen window may cover the map because the player
deliberately opened it and is not looking at the map then; the "keep the center clean" rule governs the
*permanent* HUD, not an opened window.

---

## 5. Designing a New Panel — Procedure

Run this every time a new panel/window is requested.

1. **Place it first (§4).** Decide which region owns it: top-bar zone, a bottom-panel sub-panel, or an
   on-demand window (global from the top bar, contextual from a bottom tab). If it is agency, it is a
   card/deck window, not a panel (§9).
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
6. **Keep it read-only.** If the design wants an action, that action belongs on a **card**, not the panel
   (sole HUD exception: «Завершити хід»).
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
- **Anti-pattern:** a tab that fires an action itself (a tab only switches the view); inline action buttons
  inside a state block — agency is card-shaped and lives as cards in the Дії tab (§9).

### Divider
- **Use for:** separating sections (horizontal) or sub-panels (vertical).
- **Structure:** 1px `--divider`, inset to match padding.

### Chips
- **Use for:** an **enumerable set** of small items (hex resources, tags).
- **Structure:** full-round pill, `--chip-bg`, a colored icon dot (resource hue) + label; wraps to multiple
  rows.
- **Anti-pattern:** using chips for a single key attribute (use a key–value row) or for an action (use a card).

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

- Permanent panels are **read-only**; their only exception is the **«Завершити хід»** turn-commit button.
- All player agency (build, negotiate, invest, intervene) is delivered through **cards/decks**, never as
  inline buttons on a state block. A district's action cards are hosted in the context sub-panel's **Дії**
  tab; the Mayor's global free-action deck opens **on demand** as a window. Cards — not docked buttons — are
  the only form agency takes.
- A pill/badge on a state block must never be clickable. If a design pressures you to add an action to a
  state block, that is a signal the action belongs on a card (in the Дії tab or the on-demand deck).

### Agency window archetypes (Old World lens)

Player agency is delivered through windows opened on demand, in **two standardized compositions**. Both obey
the Old World rule: **every choice shows its consequence (gain/loss icons) and its AP cost** — the player
never commits blind.

- **Action deck — master–detail.** The Mayor's free actions for the turn. A scrollable list of available
  actions (icon + name + AP cost) on the left; the selected action's detail on the right (title, what it
  does, effects, requirements, a confirm button carrying the AP cost). Use for: free choice under the AP
  budget. **Anti-pattern:** a card fan/hand — rejected; it does not scale and reads as a toy.
- **Event decision card — focused.** A single forced decision: a portrait/illustration, the situation
  narrative, then a vertical list of choices, each with its consequences + AP cost. Use for: system events,
  requests, end-of-turn consequences — one at a time. **Anti-pattern:** burying a forced decision inside a
  list where it can be skipped.

**Negotiation is NOT one of these.** Negotiating with an Important Citizen is a separate, more complex
mechanic with its **own bespoke window** — design it on its own terms when that mechanic lands.

### Read-only full-screen windows

Deep STATE (full economy, full population, a district's full breakdown) opens as a read-only full-screen
window from a top-bar opener or a bottom-panel tab. These are NOT agency and NOT modal management dialogs —
they only show state. Distinguish them clearly from the agency windows above.

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

The game renders UI in **UI Toolkit** (UXML + USS). USS is a **subset** of CSS — some mockup CSS does **not**
translate. Treat the HTML mockup as the visual target, not a literal source.

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
| `box-shadow` (elevation) | **Not supported** in USS | Fake elevation with a 9-slice background sprite, or a subtle border; or omit |
| `backdrop-filter: blur()` | **Not supported** | Rely on `--panel-bg` alpha alone (no live blur) |
| `text-transform: uppercase` (section labels) | **Not supported** | Uppercase the **string** in data/binding |
| `linear-gradient(...)` background (accent line) | Limited / version-dependent | Prefer a solid `--gold-soft` element or a sprite; verify gradient support against the project's Unity version |
| gradient **border** | not supported in USS at all | per-side border colors (lighter top/left, darker bottom/right) for a faux bevel; or a 9-slice sprite |
| `font-family` stacks (serif/sans) | USS uses **font assets** | Assign a serif and a sans **font asset** via `-unity-font-definition` |
| layout (grid) | UI Toolkit is **flexbox-only** | Use flex; there is no CSS grid |
| `position: fixed` (anchoring) | No `fixed` | Anchor with `position: absolute` inside a full-screen root element |
| rounded clipping of children | Children overflow the radius by default | Set `overflow: hidden` on the rounded parent |

`letter-spacing` **is** supported in USS. When in doubt about a USS feature, verify against the project's Unity
version (Context7) rather than assuming CSS parity.

---

## 12. Panel Construction (UI Toolkit)

How to assemble any panel. This is the default; deviate only with a stated reason.

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
  resolve their view off it — see `Assets/Modules/MainUI/MAIN_UI.md`.)
- **One shared panel instance, not per-entity.** Selection is singular, so the context panel is reused.
- **Absence is not an error; a missing prerequisite is.** An optional block with no data → hide it (normal). A
  required prerequisite that must always exist → throw, per `ARCHITECTURE.md` fail-loud.
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

Current per-window docs live under `Assets/Modules/MainUI/*` (e.g. `HexInfoPanel/HEX_INFO_PANEL.md`,
`EndTurn/END_TURN.md`). NOTE: these docs describe the CURRENT implementation, which predates the §4 layout
model above (the End Turn cluster and hex panel are not yet merged into one bottom panel). They are updated
when the new layout is actually built — this file describes the TARGET, the per-window docs describe what
exists today.

---

## 14. Global Anti-Patterns

| Wrong | Why | Right |
|---|---|---|
| Put a permanent panel on the left/right edge or over the center | Eats the map; breaks "UI on the edges" | Top bar, bottom panel, or an on-demand window |
| Render a conditional section with no data | Dead space; breaks progressive disclosure | Omit it (except the bottom panel's designed empty STATE) |
| Make the bottom panel appear/disappear on selection | Layout jump; loses the muscle-memory anchor | Keep the shell permanent; swap only the context CONTENT |
| Dock a permanent «Дії мера» button, or an inline action button on a state block | Violates state-vs-agency | District actions are **cards** in the Дії tab; the global action deck opens on demand (§9) |
| Put an action button on a state panel | Muddies the mental model | Move it to a Mayor card (only HUD action = «Завершити хід») |
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
| Hide a block with `visibility: hidden` | Leaves an empty gap | Use `display: none` — it collapses layout |
