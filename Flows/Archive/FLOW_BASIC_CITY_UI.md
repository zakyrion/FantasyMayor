---
category: A
read: archive
status: implemented
tags: [ui, mockup, basic-city]
related:
  - "[FLOW_VISION_UNIFICATION](FLOW_VISION_UNIFICATION.md)"
---

# FLOW_BASIC_CITY_UI

HTML menu prototype for resources, population, needs, district specialization and mayor orders; the world stays blank.

# 1 · Request

## User request — 2026-09-04 (verbatim)

> Треба почати з дуже базових речей:
> 1 - ресурси міста
> 2 - населення міста
> 3 - потреби населення
> 4 - екран спеціалізації для районів.
> 5 - базовий екран наказів для мера на райони
>
> А решта складних механік то вже потім підключиться. Треба буде на цьому зробити плейтест.

## Amendments (append-only)

```clojure
[{:received-at "2026-09-04, clarification 1"
  :raw-request "мені потрібен UI для цього і тільки"
  :normalized "UI only; no economy implementation"
  :confirmed true}
 {:received-at "2026-09-04, clarification 2"
  :raw-request "макет в html форматі і не намагайся створити ігровий екран посередині. Там нехай буде просто біле поле, або знайди якусь картинку, головна увага менюшкам."
  :normalized "standalone HTML mockup; white world area; focus on menus"
  :confirmed true}]
```

```clojure
[{:received-at "2026-09-04, review 1"
  :raw-request "Це дуже переобтяжений UI. Тобто мені потрібна головна панель, що саме буде бачити гравець. І які під панелі далі будуть відриватися та як"
  :normalized "Default view is a compact player HUD; detail panels are closed. Demonstrate the opening, back and closing paths."
  :confirmed true}]
```

## Review 2 — resource hierarchy (verbatim)

> 1 - от реально хз, можна в цілому щось. Або базові категорії, в Anno 1800 зверху виділено певні товари, для будівництва, а решта все в складі. Тобто нам якщо й виносити то щось таке ж базове
> 2 - так, можна.
> 3 - теж ок
> 4 - можливо окреме вікно, як в anno 1800 чи farest frontier
>
> Може бути справа, може зліва.

```clojure
{:received-at "2026-09-04, review 2"
 :normalized "Basic goods on the HUD; categories and compact good rows accepted; a separate warehouse window is a candidate; either side is possible."
 :prototype-choice "wood and stone in a small right-side rail; warehouse button opens a separate categorized window; resource details have a back path"
 :still-open "final basic set, left/right placement and acceptance of the warehouse window"
 :scope "iterate the already requested HTML; no new game content or economic rules"}
```

## Review 3 — resource details in place (verbatim)

> Склад міста в цілому норм, але є коментаз, зараз клік по ресурсу відкриває ще одне вікно. Це не правильно, ці делаті мають бути або на самому екрані, або показуватися в ньому на товарі що обрано, без переходу.

```clojure
{:received-at "2026-09-04, review 3"
 :normalized "Keep the warehouse list visible while selecting a good; show its details in place without a new window or navigation step."
 :implementation "selection detail area beside the list; stacked within the same window at narrow widths; no warehouse Back button"
 :scope "warehouse interaction in the existing HTML prototype"}
```

## Review 5 — durable main-screen specification (verbatim)

> Запиши це в окремий файл main\_UI\_specification як саме зараз виглядає головний екран. Так щоб ти потім не парсив HTML а мав уявлення про прийняті рішення зі специфікації

```clojure
{:received-at "2026-09-04, review 5"
 :normalized "Create main_UI_specification.md as the source for the current main-screen composition, menu behavior and accepted decisions; future agents read it before HTML."
 :scope "documentation only; consolidate the basic-city spec and link it from the index and existing UI documents"
 :confirmed true}
```

## Artifact scope and earlier trend amendment

```clojure
{:received-at "2026-09-04, review 6"
 :raw-request "бачу багато інфи що не зафіксована в clojure форматі."
 :normalized "Convert all substantive content in main_UI_specification.md to structured Clojure instruction blocks, preserving decisions and current examples. Keep only navigation metadata, headings and the index description outside the blocks."
 :scope "specification format only; no HTML or game-model changes"
 :confirmed true}
```

```clojure
{:received-at "2026-09-04, review 4"
 :raw-request "І ще б тренд по ресурсах показувати, вгору чи вниз"
 :normalized "Show resource stock direction in warehouse rows and the basic HUD; compare current stock with the previous mayor turn."
 :scope "up/down/unchanged indicators using the existing demonstration stock history"}
```

```clojure
{:task :basic-city-ui
 :kind :mutation
 :goal "inspect the five requested menu concerns in an interactive HTML mockup"
 :where ["design-mockups/FantasyMayor-BasicCity.html"
         "UISpecs/UISPEC_BASIC_CITY_MENUS.md"
         "Flows/FLOW_BASIC_CITY_UI.md" "INDEX.md"]
 :off-limits ["Unity code and assets" "other mockups" "game-model decisions"]
 :decided "HTML; menus only; blank white world; five requested concerns"
 :skip ["economy simulation" "elites" "deals" "politics" "map rendering"]
 :result "self-contained HTML with usable menu interactions and reviewed layout"}
```

```clojure
[{:received-at "2026-09-05"
  :raw-request "та можеш видалити і макети і спеку"   ;; owner, answering D10 of FLOW_VISION_UNIFICATION
  :normalized "main_UI_specification.md, UISPEC_BASIC_CITY_MENUS.md і FantasyMayor-BasicCity.html видалені; їхній зміст поглинув UISpecs/UISPEC_MAIN_SCREEN.md як фаза :basic; ця задача superseded FLOW_VISION_UNIFICATION"
  :confirmed true}

 {:received-at "2026-09-05, пізніше"
  :raw-request "Я видалив файли які вважав непотрібними"   ;; owner, verbatim у FLOW_VISION_UNIFICATION
  :normalized "UISPEC_MAIN_SCREEN.md, усі макети design-mockups/ і UI_LANGUAGE.md видалені власником як такі, що не відповідають баченню; жоден живий артефакт цієї задачі більше не існує; лишається лише /flow-close"
  :confirmed true}

 {:received-at "2026-09-05, закриття"
  :raw-request "закривай, бо за нашим flow тільки користувач має право закривати flow-и, я бачу що їх робота вже виконана тому закривай"   ;; owner, у FLOW_ECONOMY_POC
  :normalized "flow-close: довговічного контракту немає (артефакти видалені); історія лишається; → Flows/Archive/; метр «owner: visual direction reviewed» — :superseded"
  :confirmed true}]
```

# 2 · Contract

## Findings

```clojure
[{:finding :material-language
  :at "2026-09-04"
  :fact "UI_LANGUAGE confirms sprite frames, textured interiors, serif names, sans data and SVG icons"
  :verified-by "read UI_LANGUAGE and the accepted construction in FantasyMayor-Material2.html"}
 {:finding :screen-boundary
  :at "2026-09-04"
  :fact "The owner explicitly requests a blank world and only five basic menu concerns"
  :verified-by "current user request; overrides the full-world and elite-ring parts of UISPEC_MAIN_SCREEN for this mockup"}]
```

## Decisions

```clojure
[{:decision :artifact
  :status :confirmed :at "2026-09-04"
  :value "standalone HTML mockup, no backend or gameplay simulation"
  :verified-by "user clarification 2"}
 {:decision :world
  :status :confirmed :at "2026-09-04"
  :value "white background with no terrain, map, central illustration or game controls"
  :verified-by "user explicitly allowed a white field"}]
```

Names, amounts, recipes and action durations are demonstration fixtures for UI review, not confirmed game content.
The menu source was main_UI_specification.md until 2026-09-05; it was absorbed into UISPEC_MAIN_SCREEN.md as
phase `:basic` and deleted together with UISPEC_BASIC_CITY_MENUS.md and FantasyMayor-BasicCity.html. Later the
same day the owner deleted UISPEC_MAIN_SCREEN.md, every mockup and UI_LANGUAGE.md as not matching the vision —
see [FLOW_VISION_UNIFICATION](FLOW_VISION_UNIFICATION.md).

## Disproven (append-only)

```clojure
[{:hypothesis "the user requests an implemented economy slice"
  :refuted-by "the user explicitly said UI only, then HTML mockup"
  :at "2026-09-04" :details "Request amendments"}]
```

## Attempted (append-only)

```clojure
[{:approach "population, needs, district specialization and capacity visible simultaneously on the home screen"
  :dropped-because "owner: this is a very overloaded UI; asked for the main player panel and how subpanels open"
  :problems "technical layout and interaction checks passed, but the information hierarchy failed the owner review"
  :at "2026-09-04"}]
```

```clojure
[{:approach "selecting a warehouse good replaces the list with accounting and requires Back"
  :dropped-because "owner review 3: details must stay on the warehouse screen without a transition"
  :at "2026-09-04"}]
```

## Review 1 — hierarchy correction

```clojure
{:at "2026-09-04"
 :source "owner review 1"
 :fixed "home is compact; subpanels are reached through explicit player actions"
 :composition "top stock bar, bottom navigation with AP; one detail panel at a time"
 :proposed-by "agent, rendered for review; not a confirmed final visual composition"
 :routes "UISPEC_BASIC_CITY_MENUS owns the opening/back/closing paths"}
```

## Review 2 — basic HUD and warehouse

```clojure
{:at "2026-09-04"
 :source "owner review 2"
 :accepted "categories and compact icon/name/stock rows"
 :prototype "right-side wood/stone rail with Warehouse button; separate centered warehouse; categories, search, detail/back"
 :pending "owner review of the rendered warehouse, final basic resource set and side"
 :verification "browser checked search including zero stocks; detail/back restores query and selection; category expansion survives detail/back; Escape closes; at 390x844 no horizontal overflow and no rail/population overlap; opening warehouse closes the drawer; console error/warning log empty"
 :limitation "nine existing demo goods; no 40–50-good playtest yet"}
```


# 3 · Plan

```clojure
{:status :complete   ;; closed 2026-09-05 by the owner's word — «закривай»
 :completed [:flow-close :scope-clarification :source-review :html-menus :browser-review :progressive-disclosure :navigation-review :resource-hierarchy :resource-navigation-review :warehouse-inline-details :warehouse-selection-review :resource-trends :main-ui-specification]
 :current :closed
 :remaining []
 :resume-context "CLOSED 2026-09-05: довговічного контракту немає — макет і спека видалені власником; історія лишається тут. SUPERSEDED 2026-09-05 by FLOW_VISION_UNIFICATION: артефакти видалені; зміст був поглинутий UISPEC_MAIN_SCREEN.md як фаза :basic, а пізніше того ж дня власник видалив і цю спеку, всі макети та UI_LANGUAGE; лишається лише /flow-close. Історичний контекст: Read main_UI_specification.md first: it owns the current HUD, warehouse selection in place, trend arrows, population/district/order routes, fixtures and open decisions. HTML is the render. The former basic-city spec is a pointer; the broader screen spec is explicitly scoped separately. UI owner review and provisional basic resource set/side remain open."}
```

## Acceptance

```clojure
[{:meter "browser interaction and screenshot review"
  :target "home contains no open detail panels; population, districts and orders open through navigation; back, close and Escape work; white world; no overlap"
  :actual "2026-09-04 browser: 0 open drawers on load; 1280x720 home fits; districts -> overview -> specialization -> confirmation -> overview -> order; issue/cancel AP 3/5 -> 2/5 -> 3/5; quarry shortage blocks issue; back to overview/list; navigation switches one drawer and toggles it closed; population need detail; resource popup excludes drawer; Escape closes; 390x844 no horizontal overflow; console errors/warnings empty" :status :passed}
 {:meter "python3 Tools/doc_lint.py --quiet"
  :target "0 ghosts and 0 Clojure syntax errors" :actual "0 ghosts; 0 syntax errors; 49 md scanned (2026-09-04)" :status :passed}
 {:meter "owner"
  :target "visual direction and menu flow reviewed" :actual "Review 3: warehouse direction accepted; resource detail navigation rejected. In-place detail area rendered for review. 2026-09-05: the mockup and the spec were deleted by the owner as not matching the vision — no further review possible" :status :superseded}

 ;; ── flow-close, 2026-09-05 ──
 {:meter "python3 Tools/gen_index.py після mv в Archive" :target "LINT: clean" :actual "LINT: clean; 39 docs, 14 archive (2026-09-05)" :status :passed}
 {:meter "python3 Tools/doc_lint.py --quiet" :target "0 ghosts, 0 syntax" :actual "0 ghosts; 0 syntax errors; 43 md scanned (2026-09-05)" :status :passed}
 {:meter "owner" :target "permission to close" :actual "«закривай, бо за нашим flow тільки користувач має право закривати flow-и, я бачу що їх робота вже виконана тому закривай» — 2026-09-05" :status :passed}]
```

## Review 3 verification

```clojure
{:at "2026-09-04"
 :meter "browser interaction, DOM state and screenshots"
 :actual "Selecting fish keeps the warehouse title, search, all nine catalog entries and focus on the selected row; one dialog remains open and no warehouse Back button exists. Searching for pottery and selecting its zero stock keeps the query and matching category visible. Desktop side-by-side and 390x844 stacked detail views visually checked; no console errors or warnings."
 :status :passed}
```

## Review 4 verification

```clojure
{:at "2026-09-04"
 :meter "browser DOM state, screenshot and narrow-layout check"
 :actual "Warehouse shows grain rising by 4, fish falling by 2 and bread unchanged; all nine indicators agree with the existing stock-history fixtures. Tooltips and accessible names include the period and exact change. Wood/stone HUD indicators fit at 390px without text overlap or overflow. No console errors/warnings; doc-lint has zero ghosts and Clojure syntax errors."
 :status :passed}
```

## Review 5 verification

```clojure
{:at "2026-09-04"
 :result "main_UI_specification.md is the current UI source; the basic-city spec redirects to it and the broader screen spec points to the narrower scope"
 :meter "Tools/gen_index.py and Tools/doc_lint.py --quiet"
 :actual "INDEX: 40 docs, lint clean. Documentation: 50 files scanned, 0 ghosts, 0 Clojure syntax errors."
 :status :passed}
```

## Review 6 verification

```clojure
{:at "2026-09-04"
 :result "All substantive main_UI_specification.md content is expressed as structured Clojure instruction blocks; tables and the text layout are converted to maps and vectors."
 :meter "Tools/doc_lint.py --quiet plus document-format audit"
 :actual "0 ghosts; 0 Clojure syntax errors. Only YAML navigation metadata, headings and the INDEX description remain outside Clojure. Rule-set names are unique; map nesting is at most two."
 :status :passed}
```
