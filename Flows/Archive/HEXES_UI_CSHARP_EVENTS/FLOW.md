---
category: A
read: archive
status: implemented
tags:
  - ui
  - view
---

HexesUI talks through C# events; Generate asks for MapCreation and disposes the generator menu.

# Request

```clojure
{:source :user
 :received-at "2026-09-26"
 :raw-request ["давай переведемо HexesUI на C#-події і після натискання на кнопку generate, цей UI потрібно dispose"
               "1 — «B (40) Лише Generate»; 2 — «A (80) Видалити повністю»; 3 — «A (65) Завантажувати щоразу»; «Так»"]}
```

# Confirmed contract

```clojure
{:task :hexes-ui-csharp-events
 :goal "HexesUI на C#-подіях; після натискання Generate — перехід у MapCreation і dispose цього UI"
 :path :direct
 :where #{HexesUI ShowHexesUISystem MapCreationRequestSystem TerrainGenerationGenerateEventComponent UIInstaller SystemPriorities EventReaderAotDeclarations}
 :pattern PATTERN_VIEW_SYSTEM
 :decided {:buttons "лише GenerateButton піднімає C#-подію; SecondButton і GenerateMeshButton не підключені"
           :dead "MapCreationRequestSystem і TerrainGenerationGenerateEventComponent видаляються повністю"
           :reentry "кожен вхід у MainMenu завантажує UI; UI, що вже існує, — throw"
           :subscriber "[ViewSubscriber(typeof(HexesUI))]; [StateAllowed] на полях; fail-loud"}
 :accept [{:meter "roslyn get_diagnostics" :target "0 errors"}
          {:meter "/arch-check" :target "нових порушень немає"}
          {:meter "fmgraph check + pattern PATTERN_VIEW_SYSTEM" :target "3 відхилення HexesUI зникли, нових немає"}
          {:meter "Editor recompile + console_status" :target "compilationFailed false"}
          {:meter "Unity (власник)" :target "Generate → MapCreation, меню генератора зникає"}]
 :result "Generate переводить гру в MapCreation і прибирає меню генератора"}
```

# Findings

```clojure
[{:finding :injection-path
  :at "2026-09-26"
  :fact "HexesUI отримує EntityStorages через container.Instantiate у Addressable.LoadAndInstanceAsync; fmgraph не бачить інжекторів"
  :verified-by "Read Assets/Modules/Addressable/Implementation/Addressable.cs:42; fmgraph consumers HexesUI → 0"
  :consequence "без Construct вʼюсі інʼєкція не потрібна"}
 {:finding :dispose-in-click
  :at "2026-09-26"
  :fact "release Box інстансу — Object.Destroy (відкладений до кінця кадру), тож dispose в обробнику кліку безпечний"
  :verified-by "Read Addressable.cs:49-54; ADDRESSABLE_PATTERNS правило 3"
  :consequence "обробник може звільнити UI одразу"}
 {:finding :dead-after
  :at "2026-09-26"
  :fact "TerrainGenerationGenerateEventComponent: emits HexesUI, reacts MapCreationRequestSystem — інших немає; Show/Hide у ShowHexesUISystem — 0 посилань"
  :verified-by "fmgraph explain TerrainGenerationGenerateEventComponent; roslyn find_references Show"
  :consequence "видаляються"}
 {:finding :namespace-vanished
  :at "2026-09-26"
  :fact "TerrainGenerationGenerateEventComponent був єдиним файлом Domains/Map/Generation/Components; з ним зник простір імен — 7 файлів мали мертвий using"
  :verified-by "roslyn get_diagnostics → 7× CS0234; ls теки"
  :consequence "тека з .meta видалена, using прибрано"}]
```

# Decisions

```clojure
[{:id 1 :asked "Що роблять дві інші кнопки?" :status :confirmed :at "2026-09-26"
  :options [{:id :a :option "як Generate" :confidence 50} {:id :b :option "лише Generate" :confidence 40} {:id :c :option "кожна своя подія" :confidence 10}]
  :chosen :b :value "лише Generate підключена" :verified-by "«B (40) Лише Generate»"}
 {:id 2 :asked "MapCreationRequestSystem і TerrainGenerationGenerateEventComponent стають мертвими. Видалити?" :status :confirmed :at "2026-09-26"
  :options [{:id :a :option "видалити повністю" :confidence 80} {:id :b :option "лишити" :confidence 20}]
  :chosen :a :value "видалити повністю" :verified-by "«A (80) Видалити повністю»"}
 {:id 3 :asked "Повторний вхід у MainMenu?" :status :confirmed :at "2026-09-26"
  :options [{:id :a :option "завантажувати щоразу" :confidence 65} {:id :b :option "лише раз" :confidence 35}]
  :chosen :a :value "кожен вхід завантажує UI" :verified-by "«A (65) Завантажувати щоразу»"}]
```

# Progress

```clojure
{:status :complete
 :completed #{"task statement" "code" "verification"}
 :current "closed 2026-09-26"
 :remaining #{}
 :resume-context "implementation go 2026-09-26"}
```

# Acceptance

```clojure
[{:meter "roslyn get_diagnostics" :target "0 errors" :actual "Error — 0, solution-wide" :status :met}
 {:meter "/arch-check" :target "нових порушень немає" :actual "ShowHexesUISystem — разова система, обидва поля з [StateAllowed], без Generic-колекцій і алокацій на тік" :status :met}
 {:meter "fmgraph check + pattern PATTERN_VIEW_SYSTEM" :target "3 відхилення HexesUI зникли" :actual "32 → 28 warnings (3 view-boundary HexesUI + event/declare TerrainGenerationGenerateEventComponent), integrity clean; ShowHexesUISystem — instance [marker], ShowHexesUISystem -> HexesUI via GenerateClicked +=" :status :met}
 {:meter "Editor recompile + console_status" :target "compilationFailed false" :actual "up_to_date, compilationFailed false, consoleErrors 0; CS0234 о 14:26:37 — проміжна компіляція між видаленням події і прибиранням using" :status :met}
 {:meter "Unity (власник)" :target "Generate → MapCreation, меню зникає" :actual "власник: «перевірив, працює» (2026-09-26)" :status :met}]
```
