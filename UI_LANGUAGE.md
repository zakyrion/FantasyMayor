---
category: C
read: trigger
trigger: "before creating or changing any UI — panels, USS, mockups, icons, layout"
tags: [ui, design-language, uss, unity]
related:
  - "[ARCHITECTURE](ARCHITECTURE.md)"
---

# UI_LANGUAGE

FantasyMayor's UI style language: what UI Toolkit can render, the confirmed material direction, the content discipline.

> **Responsibility — STYLE only.** What a surface is made of and how it reads. This file does not describe
> game mechanics and does not depend on the economic model; it carries no reference to one. A mechanic that
> needs an interface gets its own mechanic document, its own UI document and its own mockup — and each of
> those obeys this file.
>
> **Self-contained by design.** It is written to be usable with no prior conversation context — including by
> another model. It states the platform facts, the confirmed direction, the rules, and explicitly what was
> rejected and why, so a rejected approach is not proposed again.
>
> **Replaces `GENERAL_UI_STYLE.md`**, deleted 2026-08-31 by the owner as outdated and disliked, together
> with its two canonical renders. **Do not reconstruct that document or its look from git history.** The
> live UI under `Assets/Presentation/UI/**` still implements the old language — it is the thing being
> replaced, not the reference.
>
> **Status.** Blocks are `:confirmed` by the owner or marked `OPEN`. Nothing here is agent taste presented
> as settled.

---

## 1 · Platform — what UI Toolkit actually does

Verified 2026-08-31 against the Unity **6000.3** USS properties reference
(`docs.unity3d.com/6000.3/Documentation/Manual/UIE-USS-Properties-Reference.html`). Re-verify against the
project's Unity version rather than trusting this list forever.

```clojure
(def uss-capability
  {:supported #{background-image background-repeat background-size background-position
                -unity-slice-left -unity-slice-right -unity-slice-top -unity-slice-bottom
                -unity-slice-scale -unity-slice-type          ;; 9-slice, complete set
                -unity-background-image-tint-color            ;; one sprite, many colours
                text-shadow -unity-text-outline
                rotate scale translate transition
                letter-spacing border-radius opacity}
   :absent #{box-shadow "any USS gradient (linear-gradient etc.)"}
   :also-absent #{"CSS grid" "flex gap / row-gap / column-gap"
                  "text-transform" "::before / ::after"
                  ":first-child / :last-child / :nth-child / :not()"
                  "position: fixed"}
   :gotcha "USS flex-direction defaults to COLUMN, not row — set it explicitly on every flex container"
   :workarounds {"spacing" "margin, never gap"
                 "uppercase" "uppercase the STRING in data"
                 "rounded clipping" "overflow: hidden on the rounded parent"}})
```

```clojure
(def the-key-platform-fact  ;; 2026-08-31 — this is what unlocked the accepted direction
  {:rule "the gradient/shadow ban is on USS EFFECTS, not on painted art"
   :means "anything inside a SPRITE is unlimited — bevels, glow, ornament, gradients. Unity renders the image as authored"
   :therefore "depth and decoration move INTO sprites; USS only places and tints them"
   :why-it-matters "a mockup built from flat solid fills alone reads as a wireframe. That was a choice, never a platform limit"})
```

---

## 2 · Visual direction — CONFIRMED

The owner reviewed eight candidate directions and approved two, which were then merged: a carved 9-slice
frame (weight) and a tiling textured interior (grain). Neither was complete alone.

```clojure
(def material-language  ;; 2026-08-31 — confirmed
  {:surface-frame {:is "a 9-slice sprite — the panel's whole border"
                   :carries "depth, bevel, corner ornament — all painted IN the sprite"
                   :uss "-unity-slice-* ; mockups use CSS border-image … stretch, the exact analogue"}
   :interior      {:is "a tiling sprite — grain, linen, leather"
                   :uss "background-image + background-repeat: repeat"}
   :sockets       {:is "a SEPARATE, thinner 9-slice sprite for nested cells"
                   :size "~9px against the surface frame's ~20px"}
   :recolour      {:rule "one sprite, many tints — a new category is a colour, not new art"
                   :uss "-unity-background-image-tint-color"}
   :icons         {:is "Vector Graphics (SVG) assets"
                   :never "emoji — they read as placeholder clipart, not game UI"}
   :type          {:names "serif — panel titles, tier names, proper names, goods"
                   :data  "sans — labels, tags, captions"}})
```

```clojure
(def frame-scale-rule  ;; 2026-08-31 — confirmed by measuring, not by opinion
  {:rule "ONE 9-slice frame per SURFACE; nested elements take their own thinner socket sprite"
   :measured "a 20px frame eats 40px of width — acceptable on a 700px panel, fatal on a small widget"
   :never "reusing the surface frame on the elements inside it"})
```

```clojure
{:open :panel-temperature
 :question "dark (leather + bronze) or light (linen + ink) — the same construction, both drawn"
 :agent-position "dark for the permanent HUD, since a light panel becomes the brightest thing on a dark scene and pulls the eye off the map; light for full-screen windows, which cover the map anyway"
 :status "the owner has not chosen"}
```

---

## 3 · Content discipline — the rules that cost the most

Every rule below was won by having a mockup rejected. They are not style preferences; breaking one is what
made a panel unreadable.

```clojure
(def content-rules  ;; 2026-08-31 — all confirmed by review
  [{:rule :one-thing-per-cell
    :says "a cell shows EITHER what fills it, OR what it needs. Never both. Never neither"
    :because "a filled cell and an empty cell were equally dense, differing only in colour — the eye could not separate «have» from «missing», and missing is the whole point"}

   {:rule :no-derived-counters
    :says "never print what is visible right beside it"
    :examples #{"«6 груп · 2 щаблі відкрито» in a header above the tiers that show exactly that"
                "«4 ГРУПИ» next to four group markers"}
    :test "would the player know this without the label? then cut the label"}

   {:rule :count-is-a-numeral
    :says "a count is a NUMBER. A row of pictograms is banned"
    :because "replacing the text «4 ГРУПИ» with four person icons is the same duplicate, drawn"
    :corollary "next to the number put its DELTA — the part the layout cannot show"}

   {:rule :never-render-an-empty-section
    :says "a locked tier, an unavailable block, a not-yet-open row — omit entirely"
    :because "six cells announcing «nothing here» is six cells of nothing"}

   {:rule :no-placeholder-marks
    :says "no «—» inside an empty cell; an empty cell is already empty"}

   {:rule :structure-can-be-layout
    :says "let the arrangement carry the rule instead of a caption"
    :example "the tier ladder's «3 inherited + 3 new» reads from a visual gap alone — the caption «успадковані / нові» is unnecessary"}

   {:rule :hover-is-the-density-valve
    :says "detail that is not needed at a glance lives in hover, not in permanent text"
    :hard-case "WHICH good can fill an empty slot MUST stay a hover — in the market phase several goods will qualify, so naming one permanently would be a lie"
    :limit "hover does not solve COMPARISON — comparing several entities needs a table, and a tooltip cannot"}

   {:rule :two-areas-per-decision
    :says "spread by QUESTION, but keep any single decision within at most two areas of the screen"
    :because "prior art: strategy UIs are criticised precisely for scattering one decision across the edges of the screen"}])
```

---

## 4 · Rejected — do not propose again

```clojure
(def rejected  ;; each was built, shown, and refused
  [{:approach "hairline wireframe: 1px borders, 11–13px muted text throughout, no hierarchy, emoji icons"
    :verdict "«виглядає блювотно і не як UI для гри, а якась мерзость»"
    :at "2026-08-31"
    :lesson "flatness was the agent's choice, not a USS limit — see (def the-key-platform-fact)"}

   {:approach "the previous design language (GENERAL_UI_STYLE.md) and its two canonical renders"
    :verdict "deleted by the owner as outdated and disliked"
    :at "2026-08-31"
    :lesson "faithfully reproducing it is what produced the rejected mockup above"}

   {:approach "a full mockup padded with derived counters, duplicate labels, a locked tier of placeholder cells"
    :verdict "«насичене абсурдно дурними деталями... просто хуй пойми про що»"
    :at "2026-08-31"
    :lesson "the material language survived; only the filling was refused. Check a mockup AGAINST the rules in §3 before showing it"}

   {:approach "a row of person pictograms as the group count"
    :verdict "«А ось цю хуйню ти кому залишив?»"
    :at "2026-08-31"
    :lesson "cutting a redundant label and drawing the same redundancy is not a fix"}])
```

---

## 5 · Reference set

The owner's named visual references: **Frostpunk 2, Old World, Civilization VI, Offworld Trading Company**.

```clojure
{:observation "three of the four share a lineage — Civ VI descends from Civ IV, and Civ IV, Old World and Offworld Trading Company were all designed by Soren Johnson"
 :verified-by "read outside the project (Wikipedia — Soren Johnson / Mohawk Games)"
 :reading "the through-line is LEGIBILITY: exact numbers on screen, strong iconography, no hidden math. Frostpunk 2 is the outlier and supplies the coat — weight, atmosphere, strong typography"
 :one-line "Soren Johnson's legibility wearing Frostpunk 2's coat"
 :caution "borrow systems and information architecture; the tone is this project's own"}
```

---

## 6 · Open

```clojure
[{:open :panel-temperature
  :question "dark (leather + bronze) or light (linen + ink) — the same construction, both drawn; see §2"
  :agent-position "dark for the permanent HUD, light for full-screen windows"}]
```

Open questions that belonged to a MECHANIC rather than to style — screen architecture, labour-by-category,
the elite-profile surface — moved to `Flows/FLOW_BUILD_UX_DECONGESTION.md` on 2026-09-02. They return here
only as style questions, if at all; their homes are the per-mechanic UI documents.

---

## Provenance

The language was established on **2026-08-31** and extended on **2026-09-02**, recorded live in
`Flows/FLOW_BUILD_UX_DECONGESTION.md` — which holds the reasoning, the sources, the disproven hypotheses and
every rejected approach with its cost (it moves to `Flows/Archive/` when it closes). On 2026-09-02 this file
was narrowed to style: the model-derived surface rules it used to carry moved out, because a style document
that also states mechanics rots at two different speeds.

Mockups: `design-mockups/FantasyMayor-Directions.html` (round 1, four skins), `FantasyMayor-Directions2.html`
(round 2, four capability-led), `FantasyMayor-Material.html` and `FantasyMayor-Material2.html` (the merged
direction, before and after the content cleanup). `FantasyMayor-SlotRow.html` is the rejected slot row from
§4 — kept only as the record of what was refused. A full-screen mockup existed and was deleted on 2026-09-02
as no longer matching the model; the FLOW records why.
