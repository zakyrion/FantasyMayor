---
category: A
read: always
status: partial
tags:
  - diagrams
  - boot
  - di
related:
  - "[ARCHITECTURE](../../ARCHITECTURE.md)"
---

PlantUML flow and structure charts of the game's boot, app states and installers, generated from fmgraph.

# Request

```clojure
{:source :user
 :received-at "2026-09-26"
 :raw-request ["(def build-uml{
               :where [\"Boot.cs\" \"AppState.cs\" \"IInstaller implementation\"]
               :goal \"мені потрібна plant uml flow та structure charts для гри\"
               :result \"1 глобальна але менш деталізована діаграма для flow та structure charts і більш деталізовані для станів\"
})"]}
```

# Confirmed contract

```clojure
{:task :build-uml
 :goal "PlantUML flow та structure charts для гри"
 :kind :mutation
 :path :direct
 :where [Boot.cs AppState.cs "реалізації IInstaller"]
 :off-limits #{"код Assets/" BOOT_DEPENDENCIES.canvas}
 :decided {:home "Assets/Diagrams/*.puml"
           :structure "компоненти й DI: Boot → інсталери → що реєструють → системи по станах"
           :source "генератор з fmgraph, скрипт у Tools/"
           :render-check "власник дивиться в Rider"}
 :read [ARCHITECTURE.md]
 :tools [tool-di-wiring tool-recipe-instances roslyn]
 :accept [{:meter "власник у Rider" :target "усі діаграми рендеряться і відповідають грі"}]
 :result "1 загальна менш деталізована flow + 1 structure; детальні — по кожному з 8 станів"}
```

# Plan

```clojure
{:scope ".claude/skills/fantasymayor-graph/ (scripts + SKILL.md + references) and ^:new Assets/Diagrams/ — Assets/ untouched"
 :do (-> (:step-1 "source_reading + ecs_facts: an AppState.X inside an Events.Raise argument → edge requests_state (owner → the game state whose Mode is X); Switch(AppState.X) in Boot → edge starts_in; an AppState.Y assigned inside a game state outside Raise → edge requests_unread (the dead RequestedMode mechanism, drawn dashed)")
         (:step-2 "^:new uml.py — renders .puml from the loaded graph alone: flow and structure, global and per state")
         (:step-3 "build_graph writes Assets/Diagrams/ (only files whose text changed) as its last step (the mandatory step, decision 7); ^:new command fmgraph.py uml renders on demand")
         (:step-4 "SKILL.md, references/graph-facts.md: the three new edges and the uml command")
         (:step-5 "verify: fmgraph.py build time, fmgraph.py check no new warnings, every Diagrams/*.puml written; the owner opens them in Rider"))
 :diagrams {"Assets/Diagrams/flow.puml" "state machine: [*] → Initialization → ConfigLoading → MainMenu; MainMenu ..> MapCreation ..> Gameplay dashed; MapLoading, InstanceObjects, GameOver grey as unreachable; system count per state"
            "Assets/Diagrams/structure.puml" "Boot → GameModeMachine → 8 states; installers grouped by layer, one arrow per (installer, state) labelled with the number of systems"
            "Assets/Diagrams/states/<State>.flow.puml" "activity: Enter — entry systems by Priority; loop Tick / LateTick — per-frame and reactive systems by Priority; exits — the transitions out of this state"
            "Assets/Diagrams/states/<State>.structure.puml" "component: installer → system, each system with its role and Priority; the events each emits and consumes"}
 :risks #{"how AppStateSystems splits entry from tick systems is read from AppStateSystems.cs at step 2 — if the graph cannot tell them apart, step 1 gains that fact too"
          "PlantUML syntax is checked only in Rider (decision 4)"}
 :acceptance [{:meter "time python3 fmgraph.py build" :target "still well under a second"}
              {:meter "python3 fmgraph.py check" :target "no new warnings"}
              {:meter "ls Diagrams/ Diagrams/states/" :target "2 global + 16 per-state .puml"}
              {:meter "the owner in Rider" :target "every diagram renders and matches the game"}]}
```

# Findings

```clojure
[{:finding :systems-per-state-in-graph
  :at "2026-09-26"
  :fact "fmgraph state <X> lists every system of a state with its installer line: Initialization 1, ConfigLoading 26, MapLoading 0, MapCreation 14, InstanceObjects 1, MainMenu 1, Gameplay 25, GameOver 0"
  :verified-by "python3 fmgraph.py state for each of the 8 states, after fmgraph.py build"
  :consequence "the per-state structure charts come straight from the graph"}
 {:finding :graph-has-no-machine-output
  :at "2026-09-26"
  :fact "fmgraph commands print text only, no --json; the graph-gate hook denies reading .fantasymayor-graph/ from Bash"
  :verified-by "fmgraph.py -h, state -h, installer -h, systems -h; the hook's deny on ls .fantasymayor-graph/"
  :consequence "the generator either imports fmgraph's Python modules, or fmgraph gains a machine-readable output"}
 {:finding :transition-target-not-in-graph
  :at "2026-09-26"
  :fact "the graph knows WHO raises AppStateRequestedEvent (InitializationSystem, ConfigLoadingState) but not WHICH mode it requests — the target is a field value Requested = AppState.X"
  :verified-by "fmgraph.py explain AppStateRequestedEvent; grep 'Requested = ' over Assets/*.cs — to read the value fmgraph does not carry"
  :consequence "the flow chart's transition arrows need a source the graph lacks"}
 {:finding :two-transitions-dead
  :at "2026-09-26"
  :fact "only two transitions are live: Initialization → ConfigLoading (InitializationSystem raises the event) and ConfigLoading → MainMenu (ConfigLoadingState raises it after its loaders). MainMenuState (→ MapCreation on a generate request) and MapCreationState (→ Gameplay after 3 settle frames) still set a RequestedMode property, but IAppState no longer declares it and nothing reads it — Boot switches only on AppStateRequestedEvent. MapLoading, InstanceObjects and GameOver are entered by nobody"
  :verified-by "read Boot.cs, GameModeMachine.cs, IAppState.cs, the 8 state files; grep RequestedMode over Assets/*.cs — 8 declarations, 0 readers; IAppState.cs:12 doc comment still describes RequestedMode"
  :consequence "as the code stands the game stops in MainMenu — the generate button changes no state; a faithful flow chart shows the gap"}]
```

# Decisions

```clojure
[{:id 1
  :asked "Де мають лежати діаграми?"
  :status :confirmed
  :at "2026-09-26"
  :options [{:id :a :option "Assets/Diagrams/*.puml — окрема папка, тільки .puml" :confidence 60}
            {:id :b :option ".puml + відрендерені SVG поруч" :confidence 25}
            {:id :c :option "один .md з plantuml-блоками" :confidence 15}]
  :chosen :a
  :value "Assets/Diagrams/*.puml"
  :verified-by "«A (60) Diagrams/*.puml»"}
 {:id 2
  :asked "Що таке «structure chart»?"
  :status :confirmed
  :at "2026-09-26"
  :options [{:id :a :option "компоненти й DI: Boot → інсталери → що реєструють → системи по станах" :confidence 60}
            {:id :b :option "діаграма класів" :confidence 25}
            {:id :c :option "класичний Yourdon — ієрархія викликів" :confidence 15}]
  :chosen :a
  :value "компоненти й DI"
  :verified-by "«A (60) Компоненти й DI»"}
 {:id 3
  :asked "Знімок чи генератор?"
  :status :confirmed
  :at "2026-09-26"
  :options [{:id :a :option "знімок вручну" :confidence 60}
            {:id :b :option "генератор з fmgraph, скрипт у Tools/" :confidence 40}]
  :chosen :b
  :value "генератор з fmgraph"
  :verified-by "«B (40) Генератор з fmgraph»"}
 {:id 4
  :asked "Як перевіряти, що діаграми рендеряться?"
  :status :confirmed
  :at "2026-09-26"
  :options [{:id :a :option "brew install plantuml, -checkonly" :confidence 55}
            {:id :b :option "без перевірки — власник дивиться у своєму редакторі" :confidence 30}
            {:id :c :option "plantuml.jar у scratchpad" :confidence 15}]
  :chosen :b
  :value "власник дивиться в Rider"
  :verified-by "«я в rider дивлюся»"}
 {:id 5
  :asked "Два переходи мертві — що робимо?"
  :status :confirmed
  :at "2026-09-26"
  :options [{:id :a :option "діаграма показує як є, мертві переходи пунктиром; полагодити — окрема задача" :confidence 70}
            {:id :b :option "спершу полагодити переходи (MainMenu/MapCreation піднімають подію), потім діаграми" :confidence 30}]
  :chosen :a
  :value "діаграма показує як є, мертві переходи пунктиром; полагодити — окрема задача"
  :verified-by "«A (70) Показати як є»"}
 {:id 6
  :asked "Звідки генератор бере дані?"
  :status :confirmed
  :at "2026-09-26"
  :options [{:id :a :option "fmgraph отримує --json (state, installer, systems) і ребро «запитує стан X»; генератор читає тільки JSON" :confidence 60}
            {:id :b :option "генератор імпортує Python-модулі fmgraph напряму; цільовий стан — окремий скан коду" :confidence 25}
            {:id :c :option "генератор розбирає текстовий вивід fmgraph" :confidence 15}
            {:id :d :option "власна відповідь власника — окреме API всередині fmgraph" :confidence 0}]
  :chosen :d
  :value "генерація — окреме API всередині fmgraph і обовʼязковий крок у ньому; джерело даних — на розсуд агента; критерії — точно і швидко"
  :verified-by "«додай це як обовʼязковий крок в fmgraph як окреме API а з чого воно буде генерувати - пофіг. Головне щоб точно і швидко»"}
 {:id 7
  :asked "«Обовʼязковий крок» — коли fmgraph пише Diagrams/?"
  :status :confirmed
  :at "2026-09-26"
  :options [{:id :a :option "кожен build переписує Diagrams/ (build і так біжить сам, коли граф застарів) + команда uml" :confidence 65}
            {:id :b :option "тільки команда uml; check попереджає, коли Diagrams/ застаріли" :confidence 35}]
  :chosen :a
  :value "кожен build переписує діаграми + команда uml"
  :verified-by "«A (65) Кожен build + uml»"}
 {:id 8
  :asked "Assets/Diagrams/ — в git чи в .gitignore?"
  :status :confirmed
  :at "2026-09-26"
  :options [{:id :a :option "в git — вивід детермінований, диф міняється лише разом з кодом" :confidence 60}
            {:id :b :option "в .gitignore, як сам граф fmgraph — похідний артефакт" :confidence 40}]
  :chosen :b
  :value "в .gitignore назавжди"
  :verified-by "«B (40) В .gitignore»; «І додай цю папку назавжди в гітігнор»"}
 {:id 1
  :round 2
  :new-fact "власник хоче, щоб Rider підхоплював діаграми як частину проєкту Unity"
  :asked "Де мають лежати діаграми?"
  :status :confirmed
  :at "2026-09-26"
  :options [{:id :a :option "Assets/Diagrams/*.puml у корені" :confidence 30}
            {:id :d :option "окрема папка в Assets/ — Assets/Diagrams/, у .gitignore разом з Assets/Diagrams.meta (Unity сам пише .meta)" :confidence 70}]
  :chosen :d
  :value "Assets/Diagrams/ (+ states/), ignored together with the .meta Unity writes"
  :verified-by "«створи окрему папку для uml діаграм в папці Assets щоб її норм підхоплював Rider. І додай цю папку назавжди в гітігнор»"
  :reason "Rider shows Assets/ content; a ~-suffixed folder would skip Unity import but may be hidden from Rider's Unity view"}
 {:id 9
  :asked "План приймаєш — починати реалізацію?"
  :status :confirmed
  :at "2026-09-26"
  :options [{:id :a :option "так, GO" :confidence 80}
            {:id :b :option "спершу зміни" :confidence 20}]
  :chosen :b
  :value "GO with the location changed to Assets/Diagrams/, ignored forever"
  :verified-by "«створи окрему папку для uml діаграм в папці Assets …»"}
 {:id 10
  :asked "Чим перевіряти рендер у мене?"
  :status :confirmed
  :at "2026-09-26"
  :options [{:id :a :option "brew install plantuml, -checkonly по всіх файлах" :confidence 60}
            {:id :b :option "plantuml.jar у scratchpad" :confidence 25}
            {:id :c :option "тільки Rider" :confidence 15}]
  :chosen :a
  :value "brew install plantuml"
  :verified-by "«A (60) brew install plantuml»"}
 {:id 11
  :asked "Що саме показує Rider?"
  :status :confirmed
  :at "2026-09-26"
  :options [{:id :a :option "помилку синтаксису" :confidence 40}
            {:id :b :option "нічого / немає превʼю" :confidence 40}
            {:id :c :option "іншу помилку" :confidence 20}]
  :chosen :b
  :value "Rider shows no preview at all"
  :verified-by "«B Нічого / немає превʼю»"}
 {:id 12
  :asked "Починати реалізацію цього доповнення?"
  :status :confirmed
  :at "2026-09-26"
  :options [{:id :a :option "так, GO" :confidence 80}
            {:id :b :option "спершу зміни" :confidence 20}]
  :chosen :a
  :value "GO on amendment a-1"
  :verified-by "«A (80) Так, GO»"}]
```

# Disproven

```clojure
[{:hypothesis "no preview in Rider means the PlantUML plugin is not installed"
  :refuted-by "Rider2026.1/plugins/plantuml4idea present, enabled (idea.log 12:32 'Loaded custom plugins … plantuml4idea (7.15.0)'), not in disabled_plugins.txt; the owner said so too"
  :at "2026-09-26"
  :details "# Progress :rider-check — the real cause: a comment above @startuml; plantuml4idea previews the diagram under the caret, which opens on line 1"}]
```

# Attempted

```clojure
[]
```

# Progress

```clojure
{:status :complete
 :completed #{"statement confirmed" "research passes 1-2" "plan + go (decision 9)"
              "step 1 — requests_state edge (Events.Raise | Switch | assign) + entry_run facet"
              "step 2 — scripts/uml.py"
              "step 3 — build writes Assets/Diagrams/ (changed files only), command uml, missing folder = stale graph"
              "step 4 — SKILL.md Diagrams section + schema, references/graph-facts.md, .gitignore"
              "step 5 — verified: build 0.34-0.37s, check exit 0 with 32 warnings as before, 2 + 16 .puml, both paths ignored"}
 :amendment-a-1 #{"render: all 18 were already valid (plantuml 1.2026.8 renders every one, exit 0) — no preview in Rider = no PlantUML plugin there, owner-side"
                  "graph: requests_state carries member; installs edge (new X().Install(builder))"
                  "uml.py: switcher + request event, exit timing (end of entry / method), no-live-way-out, installer layers, singleton links between states (red when the writer state is entered by nobody), outside emitters, reacts_to/polls in structures, red unreachable note per state, true registration order"
                  "verified: build 0.37-0.38s, check exit 0 with 32 warnings, plantuml -tsvg -failfast2 exit 0 on all 18, PNGs read by eye"}
 :rider-check #{"plantuml4idea 7.15.0 is installed and enabled in Rider 2026.1; .puml maps to it (PlantUmlTypeFactory); Rider opened the files 12:32-12:39 with no plugin error in idea.log"
                "the plugin's own engine (plantuml-mit-1.2026.8.jar) renders all 18 with exit 0 — the files are not the cause"
                "fix applied: the generated header moved inside @startuml — the preview renders the diagram under the caret, and the caret opens on line 1"
                "outside emitters limited to events read inside the state"
                "guard (owner: «я не хочу отримувати цей баг заново»): uml.check_ide_previewable throws before any write when line 1 is not @startuml, the last line not @enduml, or a file holds more than one diagram; SKILL.md states the rule"}
 :current "done — every acceptance row met; archiving waits for the Obsidian MCP (vault_move)"
 :remaining #{"the owner opens the diagrams in Rider"}
 :follow-ups #{"wiring/state-switch (checked-by :agent) is exactly what requests_state via assign measures — the graph could enforce it, but that is a rule change through RULES_SPECIFICATION.md first"
               "the two dead transitions (MainMenu → MapCreation, MapCreation → Gameplay) and the stale RequestedMode doc comment in IAppState.cs — a separate code task"}
 :resume-context "Flows/BUILD_UML/FLOW.md — everything built; only the owner's acceptance in Rider is open"}
```

# Acceptance

```clojure
[{:meter "time python3 fmgraph.py build" :target "still well under a second" :actual "0.34-0.37s (was 0.35s)" :status :met}
 {:meter "python3 fmgraph.py check" :target "no new warnings" :actual "exit 0, 32 warnings — the same 32 as before" :status :met}
 {:meter "ls Assets/Diagrams/ Assets/Diagrams/states/" :target "2 global + 16 per-state .puml" :actual "2 + 16" :status :met}
 {:meter "git check-ignore Assets/Diagrams/flow.puml Assets/Diagrams.meta" :target "both ignored" :actual ".gitignore:13, :14" :status :met}
 {:meter "plantuml -tsvg -failfast2 Assets/Diagrams/**/*.puml" :target "0 errors" :actual "exit 0, 18 SVG" :status :met}
 {:meter "власник у Rider"
  :target "усі діаграми рендеряться і відповідають грі"
  :actual "«Тепер реально працює» — after the header moved inside @startuml"
  :status :met}]
```

# Amendments

```clojure
[{:amendment :a-1
  :received-at "2026-09-26"
  :raw-request "перенеси ці покращення в генератор і так, зараз ці діаграми не рендеряться в Rider"
  :normalized {:do ["рендер-фікс" "механізм перемикання: Boot читає запит" "вихід — у кінці входу чи на тіку"
                    "емітери подій ззовні стану" "інсталери за шарами" "singleton-залежності між станами"
                    "позначка недосяжності в діаграмі стану" "справжній порядок реєстрації"]
               :source "the comparison of three runs — generator, Opus from code, Opus over the generator's diagrams (chat report 2026-09-26)"
               :accept [{:meter "plantuml -checkonly" :target "0 errors on every .puml"}
                        {:meter "fmgraph.py check" :target "no new warnings"}
                        {:meter "the owner in Rider" :target "all 18 render"}]}
  :confirmed true}]
```
