# GENERAL_UI_STYLE.md

The general UI design language for FantasyMayor: principles, visual tokens, component patterns, and the procedure for designing any new panel.

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
> **Living visual example:** `design-mockups/index.html` (the hex info panel — three states). It is the
> reference render for everything described here. Open it when a verbal description is ambiguous.

---

## 1. Purpose & How To Use This Doc

This doc exists so the assistant can do two things:

1. **Reproduce the style** on new panels without re-deriving it — exact tokens, a component catalog, and a
   step-by-step design procedure.
2. **Explain and advise** the user on UI/UX — the principles and trade-offs behind each rule.

Ownership: **the assistant leads UI/UX** — propose a vision proactively, do not wait to be told. The user
gives feedback. When a UI decision is unstated, pick the option this doc implies and say why.

How to use it:
- Designing a new panel → follow **§3 Designing a New Panel** + pull from **§5 Component Catalog**.
- Styling → use the exact tokens in **§4 Visual Tokens**; never invent values.
- Building the panel in Unity → **§10 Panel Construction** + **§9 UI Toolkit / USS Mapping** (and its gotchas).
- Advising the user → **§2 Design Principles** + **§12 Anti-Patterns** carry the "why".

---

## 2. Design Principles

- **Map and panels for state, cards for agency.** Panels (and the map) only *show* state. Player *actions*
  are cards (the Mayor's agency). A state panel therefore carries **no action buttons** — see §7.
- **Progressive disclosure.** A panel is one card that **grows in sections** with the data available. An
  always-present header, then conditional sections appended only when their data exists. **Never render an
  empty section** as a placeholder — omit it.
- **Glanceability + predictable placement.** A panel the player opens constantly must appear in the **same
  place every time** so it becomes muscle memory. Predictability beats flexibility for state panels.
- **Cozy-fantasy tone.** Warm dark surface + gold accent. **Not** cold steel / sci-fi. The game tone is
  `cozy`; the UI must read warm and human, not industrial.
- **Color encodes meaning, not decoration.** A color always means the same thing (a role, a resource, a
  status). Do not reuse a role color for an unrelated accent, and do not recolor the same role per panel.

### Reference touchstones (what we borrow)

**Primary lens — Old World.** The closest structural twin to FantasyMayor's core loop, so it is the
DEFAULT reference for interaction and information architecture. Two of its signature systems ARE our core:
- its **Orders** economy (a small per-turn pool spent on actions) ≈ our **Action Points** scarcity;
- its **event / character cards** (decisions presented as choice cards, built around characters and their
  relationships) ≈ the **Mayor's agency + Important Citizens**.

It is also a clean, modern, warm turn-based UI — a realistic visual target. **When an interaction is
unspecified, resolve it the Old World way:** turn-based, card-driven, character-centric, readable.

**Subsystem touchstones (borrow the named idea only):**
- **Shadow Empire** — decisions-as-cards and personality-driven AI leaders. Borrow the model for the
  «Дії Мера» decision deck and Important-Citizen personalities. **Drop** its dense, hard-to-read UI.
- **Frostpunk 2** — faction pressure, district panels with prominent **workforce**, council-style
  decisions, strong sectioning. Borrow for the political layer and the district panel. **Drop** its cold
  industrial palette.
- **Civilization 6** — tile/hex **yields shown as icon + number**, terrain + feature + district stacked.
  Borrow the compact icon+value yield language.
- **Endless Legend** — ornate fantasy framing and per-tile breakdowns. Borrow the warm fantasy feel.
- **Mind Over Magic / Farthest Frontier** — a compact resource strip with hover **drill-down**, and the
  principle that not everything lives on the permanent HUD — secondary state opens **on demand**. Borrow
  this for the top resource bar + the left window-opener menu.

**Tone and scale are OURS, not borrowed.** None of these are cozy — FP2 is cold-industrial; Old World and
Shadow Empire are empire-scale. We borrow their SYSTEMS and information architecture, then apply our own
warm/gold **cozy** skin (§4) at the intimate scale of a single city. Never import a reference's mood.

---

## 3. Designing a New Panel — Procedure

Run this every time a new panel/window is requested. It is the repeatable path to a consistent result.

1. **Identify the entity and its data layers.** Separate what is *always present* from what is *conditional*
   (e.g. hex: terrain always; resource sometimes; district sometimes).
2. **Order by progressive disclosure.** Always-on header first. Then conditional sections, appended only when
   their data exists, ordered from most-general to most-specific.
3. **Map each datum to a component pattern** (see §5):
   - an enumerable set (resources, tags) → **chips**
   - a single attribute (owner, action) → **key–value row**
   - a status / badge → **pill**
   - a proportional breakdown (yield split) → **split bar + breakdown rows**
   - a value modified by a factor (yield reduced by labor) → **value-with-correction**
4. **Apply tokens** from §4. Color must encode meaning (role / resource / status), never decoration.
5. **Keep it read-only.** If the design wants an action, that action belongs on a **card**, not the panel.
6. **Place it** per §6 (anchored, capped height + inner scroll).
7. **Write the per-window doc** (§10): reference this file for tokens/components, specify only the new
   window's content model, states, and data bindings.

---

## 4. Visual Tokens

These are canonical. Use them verbatim. The values mirror `design-mockups/index.html`.

### Color

**Surfaces & structure**
- `--panel-bg: rgba(30, 25, 20, 0.93)` — panel background, warm near-black, translucent
- `--panel-border: rgba(217, 164, 65, 0.28)` — gold-tinted hairline border
- `--divider: rgba(217, 164, 65, 0.14)` — internal section dividers
- `--chip-bg: rgba(255, 255, 255, 0.05)` — chip / inset fill

**Text tiers** (three levels, never more)
- `--txt: #f1e7d6` — primary, warm off-white (titles, values)
- `--txt-dim: #a89a86` — secondary (row labels)
- `--txt-faint: #7d7264` — tertiary (section labels, captions, coordinates)

**Accent**
- `--gold: #e0b25a` — primary accent (title emphasis, level, AP cost)
- `--gold-soft: #d9a441` — accent line / hover

**Resource palette** (one hue per resource; reuse wherever that resource appears)
- forest `#6fa05b` · clay `#c2784f` · fish `#5b9ab0` · stone `#9b9387` · ore `#d6a44a`

**Role / share palette** (color = the recipient role; reuse everywhere a role or its share appears)
- city `#5b8bb0` (blue) · owner `#e0b25a` (gold) · operator `#6fa05b` (green)

**Status**
- positive / active: text `#bfe0a8` on `rgba(111, 160, 91, 0.20)`, border `rgba(111, 160, 91, 0.35)`
- warning / shortage: text `#e8a96a` on `rgba(214, 138, 74, 0.18)`, border `rgba(214, 138, 74, 0.40)`

### Typography

Two families, by role:
- **Serif** — titles and proper names (panel title, district name). Conveys the fantasy register.
- **Sans** — everything else (labels, data, captions). Conveys legibility for dense numbers.

Scale (px) and weight:
- panel title — 20, serif, weight 600
- proper name (e.g. district) — 16, serif, weight 600
- emphasis value (e.g. headline yield) — 15, sans
- body / rows — 13, sans, weight 400; values weight 500
- section label — 11, sans, **UPPERCASE**, letter-spacing ~1.4px, color `--txt-faint`
- caption / coordinate — 11–12, sans, color `--txt-faint`

### Spacing, Radius, Elevation

- **Padding:** header `16px 18px`; section `14px 18px`; key–value row `7px 0`.
- **Gaps:** chip row `8px`; header icon→text `14px`.
- **Radius:** panel `14px`; terrain icon `10px`; district icon `9px`; pill `10px`; chip `20px` (full-round);
  split bar `6px`; swatch `3px`.
- **Elevation:** drop shadow `0 10px 30px rgba(0,0,0,0.45)` + inset top highlight
  `inset 0 1px 0 rgba(255,255,255,0.04)`; panel backdrop blur `6px`.
- **Motif:** a 3px gold gradient **accent line** along the panel's top edge.

---

## 5. Component Catalog

Each entry: **use for** / **structure** / **anti-pattern**. Build new panels from these, not from scratch.

### Panel shell
- **Use for:** the outer container of any info panel.
- **Structure:** rounded (`14px`), `--panel-bg`, `--panel-border`, top accent line, drop shadow; vertical
  stack of header + sections separated by dividers; `overflow: hidden` so children clip to the radius.
- **Anti-pattern:** a square, opaque, shadowless box — reads as a debug overlay, not game UI.

### Section
- **Use for:** one logical group inside the panel.
- **Structure:** an UPPERCASE `--txt-faint` label, then the section body; `14px 18px` padding; a `--divider`
  separates it from the previous section.
- **Anti-pattern:** rendering the section when it has no data (breaks progressive disclosure — omit it).

### Header
- **Use for:** the always-present identity row at the top.
- **Structure:** square icon (terrain/entity sprite) + title (serif) + subtitle/caption (e.g. coordinate).
- **Anti-pattern:** burying the entity's identity below other data.

### Divider
- **Use for:** separating sections.
- **Structure:** 1px `--divider`, horizontally inset to match section padding.

### Chips
- **Use for:** an **enumerable set** of small items (hex resources, tags).
- **Structure:** full-round pill, `--chip-bg`, a colored icon dot (resource hue) + label; wraps to multiple
  rows.
- **Anti-pattern:** using chips for a single key attribute (use a key–value row) or for an action (use a card).

### Key–Value row
- **Use for:** a single attribute of the entity (owner, operator, active action, workforce).
- **Structure:** label left (`--txt-dim`), value right (`--txt`); thin top border between consecutive rows.
- **Anti-pattern:** packing a proportional breakdown into one row (use a split bar).

### Pills
- **Use for:** compact status badges inside a value.
- **Variants:** `active` (positive/green), `cost` (gold, e.g. `2 AP`), `warning` (amber, e.g. labor `75%`).
- **Anti-pattern:** a clickable-looking pill on a state panel (it implies an action — it must not be one).

### Role identity
- **Use for:** showing who fills a role (Owner / Operator: City / Mayor / Citizen).
- **Structure:** small round avatar/initial + name; the role's color (§4) where a swatch/accent is shown.
- **Anti-pattern:** showing the same role in different colors across panels.

### Split bar + breakdown rows
- **Use for:** a **proportional distribution** (yield split City / Owner / Operator).
- **Structure:** a thin stacked horizontal bar segmented by the role palette (fast proportion read) **plus**
  breakdown rows giving exact amount + % per recipient (precise read). Show both — bar for glance, rows for
  numbers. Bar segment widths must match the row percentages.
- **Anti-pattern:** a bar whose segments don't sum/visually match the listed numbers.

### Value-with-correction
- **Use for:** a number **modified by a factor** (yield reduced by a labor shortage).
- **Structure:** the **effective** value shown prominently; below it, a faint caption with the **potential**
  value struck through + the **reason** for the delta (e.g. `потенціал ~~+8~~ · −2 нестача праці`).
- **Anti-pattern:** showing only the raw effective number — the player can't see *why* it changed. Always
  surface the causality.

---

## 6. Layout & Placement

- **State panels: anchored bottom-right**, fixed margin (~24px). Same place every time.
- **Not draggable.** Dragging adds state and unpredictability with no benefit for a glanceable state panel.
- **Cap height** (~70% of screen) and **scroll the growing block** internally when a panel gets tall (e.g. a
  full district section). Never let it run off-screen.
- **Do not cover the map center**, where the action happens. The corner keeps the center clear.
- **No modal / centered management window.** Deep management would be agency → it lives in cards. If a future
  need for a modal appears, treat it as a deliberate exception and reconsider this rule then.

---

## 7. Interaction Model

- State panels are **read-only**. They show state; they have **no buttons, no inputs, no actions**.
- All player agency (build, negotiate, invest, intervene) is delivered through **Mayor cards**, not panels.
- Consequence: a pill/badge on a panel must never be clickable. If a design pressures you to add an action to
  a panel, that is a signal the action belongs on a card.

### Agency window archetypes (Old World lens)

Player agency is delivered through windows opened on demand (from the left window-opener menu), in **two
standardized compositions**. Both obey the Old World rule: **every choice shows its consequence (gain/loss
icons) and its AP cost** — the player never commits blind.

- **Action deck — master–detail.** The Mayor's free actions for the turn. A scrollable list of available
  actions (icon + name + AP cost) on the left; the selected action's detail on the right (title, what it
  does, effects, requirements, a confirm button carrying the AP cost). Use for: free choice under the AP
  budget (the «Дії Мера» window). **Anti-pattern:** a card fan/hand — rejected; it does not scale and reads
  as a toy.
- **Event decision card — focused.** A single forced decision: a portrait/illustration, the situation
  narrative, then a vertical list of choices, each with its consequences + AP cost. Use for: system events,
  requests, end-of-turn consequences — one at a time. **Anti-pattern:** burying a forced decision inside a
  list where it can be skipped.

**Negotiation is NOT one of these.** Negotiating with an Important Citizen is a separate, more complex
mechanic with its **own bespoke window** — design it on its own terms when that mechanic lands. Do not force
it into the action-deck or event-card shell.

---

## 8. Microcopy & Numbers

- **Section labels:** UPPERCASE, short, noun phrase (`РЕСУРСИ ГЕКСУ`, `ВИХІД ЦЬОГО ХОДУ`).
- **Signed quantities:** always show the sign for deltas/yields — `+6`, `−2`.
- **Resource amounts:** pair the number with the resource icon; keep icon/number order consistent within a
  given context (headline: icon → `+N` → unit name; dense rows: `+N` → icon → `%`).
- **Percentages:** shown alongside each share in a breakdown.
- **Modified values:** always pair effective + struck potential + reason (see Value-with-correction).
- **In-game strings are Ukrainian** (the player-facing language). Keep this doc's prose English (matches the
  repo's reference docs), but use real Ukrainian strings in examples.

---

## 9. UI Toolkit / USS Mapping

The game renders UI in **UI Toolkit** (UXML + USS). USS is a **subset** of CSS — some mockup CSS does **not**
translate. Treat the HTML mockup as the visual target, not a literal source.

Tokens become **USS custom properties**, assigned on a root/theme selector and read with `var()`:

```css
/* USS — define on a root class, e.g. .hex-info-panel or a shared theme element */
:root {
  --panel-bg: rgba(30, 25, 20, 0.93);
  --panel-border: rgba(217, 164, 65, 0.28);
  --divider: rgba(217, 164, 65, 0.14);
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
/* usage */
.panel { background-color: var(--panel-bg); border-color: var(--panel-border); }
```

### CSS → USS gotchas (do not translate these literally)

| Mockup CSS | USS reality | Do instead |
|---|---|---|
| `::before` / `::after` (accent line, hex texture) | USS has **no pseudo-elements** | Add a real child `VisualElement` (e.g. a 3px accent bar) or a background image |
| `box-shadow` (elevation) | **Not supported** in USS | Fake elevation with a 9-slice background sprite, or a subtle border; or omit |
| `backdrop-filter: blur()` | **Not supported** | Rely on `--panel-bg` alpha alone (no live blur) |
| `text-transform: uppercase` (section labels) | **Not supported** | Uppercase the **string** in data/binding |
| `linear-gradient(...)` background (accent line) | Limited / version-dependent | Prefer a solid `--gold-soft` element or a sprite; verify gradient support against the project's Unity version before using |
| gradient **border** | not supported in USS at all | per-side border colors (lighter top/left, darker bottom/right) give a faux-gradient bevel; for a true gradient use a 9-slice sprite or custom `generateVisualContent` |
| `font-family` stacks (serif/sans) | USS uses **font assets** | Assign a serif and a sans **font asset** via `-unity-font-definition`; the family stack does not apply |
| layout (grid) | UI Toolkit is **flexbox-only** | Use flex; there is no CSS grid |
| `position: fixed` (anchoring) | No `fixed` | Anchor with `position: absolute` inside a full-screen root element |
| rounded clipping of children | Children overflow the radius by default | Set `overflow: hidden` on the rounded parent |

`letter-spacing` **is** supported in USS. When in doubt about a USS feature, verify against the project's Unity
version (Context7) rather than assuming CSS parity.

---

## 10. Panel Construction (UI Toolkit)

How to assemble any panel. This is the default; deviate only with a stated reason.

- **Author the full skeleton in one UXML.** All *known* blocks, in canonical (progressive-disclosure) order.
  The skeleton is the single source of visual structure.
- **One subsystem per block.** Each block has its own ECS system that (1) reads its slice of the selected
  entity's state, (2) populates its block's content, (3) toggles the block's visibility.
- **Show/hide via `display`.** Data present → `display: flex`; absent → `display: none`. `display: none`
  removes the block from layout so the panel collapses — true progressive disclosure. **Never** use
  `visibility: hidden` (it leaves an empty gap).
- **Do NOT insert blocks dynamically.** A subsystem toggles a *pre-authored* container; it does not create or
  insert the block into the tree at runtime. Dynamic insertion loses authored ordering, churns GC, and
  complicates teardown. Exception: a genuinely open-ended, unknown-at-author-time set of block *types* — then
  treat it as a deliberate, documented exception.
- **Dynamic instantiation only for variable child lists.** The one place runtime element creation is justified
  is variable-length content *inside* a block (resource chips, a buildings list). Build those children from a
  small item template and **pool** them to avoid per-update GC.
- **A root/controller system owns the shared instance.** It loads the panel (addressables), owns the instance
  and handle (dispose per `ADDRESSABLE_PATTERNS.md`), subscribes to the selection signal, and shows/hides the
  whole panel on select/deselect. Mirrors the existing `ShowHexesUISystem` idiom. Block systems own only their
  own block.
- **One shared panel instance, not per-entity.** Selection is singular, so one panel is reused. (Per-entity
  world-space badges are a different concern — that is HexIcons.)
- **Absence is not an error; a missing prerequisite is.** An optional block with no data → hide it (normal). A
  required prerequisite that must always exist (e.g. the entity has no terrain type at all) → throw, per
  `ARCHITECTURE.md` fail-loud. Keep the two paths distinct.
- **Placeholder blocks.** A block whose backing ECS components do not exist yet is driven by a **placeholder
  system** that supplies stub data or keeps the block hidden, until the real components land. Mark such blocks
  `SCAFFOLD` in the window's own doc.

---

## 11. Per-Window Docs

This file is the **general** language. Every concrete window/panel gets its **own** MD doc that:

- **references this file** for tokens, components, principles, placement, and construction;
- specifies **only** that window's own concerns: its content model, its progressive-disclosure states, and its
  data bindings (which entity components feed which rows);
- does **not** restate tokens, the component catalog, or principles.

**Rule:** every **new** UI window gets its own design doc. The current TerrainGenerator / generation overlay is
**temporary UI** — do not write a design doc for it and do not treat it as a style reference.

Example split: the hex info panel's exact states (empty / resource / district) and field-to-component bindings
live in `Assets/Modules/HexesUI/HEX_INFO_PANEL.md` — not here.

---

## 12. Global Anti-Patterns

| Wrong | Why | Right |
|---|---|---|
| Render a section with no data | Dead space; breaks progressive disclosure | Omit the section entirely |
| Put an action button on a state panel | Violates state-vs-agency; muddies the mental model | Move the action to a Mayor card |
| Show a modified number raw (`+6` only) | Hides causality — player can't tell why it dropped | Effective + struck potential + reason |
| Invent a new accent color per panel | Breaks visual unity | Reuse the token palette |
| Recolor the same role across panels | Breaks the role-color language | city=blue, owner=gold, operator=green everywhere |
| Cold / steel palette | Wrong tone for a `cozy` game | Warm dark + gold |
| Draggable / centered state panel | Adds state; unpredictable | Anchor bottom-right |
| Restate a window's content here | This doc is GENERAL | Put it in that window's own doc |
| Translate `box-shadow` / `::before` / blur straight to USS | USS does not support them | Use the §9 gotcha workarounds |
| Insert each block dynamically per subsystem | Loses authored order; churns GC; messy teardown | Author the skeleton; toggle `display: none` (§10) |
| Hide a block with `visibility: hidden` | Leaves an empty gap; breaks progressive disclosure | Use `display: none` — it collapses layout |
