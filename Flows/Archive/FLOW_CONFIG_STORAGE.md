---
category: A
read: archive
status: implemented
tags: [flow, configs, entity-storages, boot, addressables]
related:
  - "[ARCHITECTURE](../../ARCHITECTURE.md)"
  - "[PATTERN_CONFIG](../../Patterns/PATTERN_CONFIG.md)"
  - "[PATTERN_CONFIG_LOADER](../../Patterns/PATTERN_CONFIG_LOADER.md)"
---

# FLOW_CONFIG_STORAGE

Конфіги — ScriptableObject у EntityStorages за типом; generic-завантажувач і крок InstanceObjects.

# 1 · Request

## User request — 2026-09-13 (verbatim)

> Я хочу в клас EntityStorages помістити всі конфіги, треба просте API що дозволить додавати та брати звідти конфіг по типу. Add<T> та bool TryGet<T>(out T);

## Agent restatement — confirmed by the user

```clojure
[{:task :config-storage
  :goal "EntityStorages тримає ScriptableObject-конфіги за типом"
  :where EntityStorages.cs
  :decided {:api        "void Add<T>(T config) where T : ScriptableObject; T Get<T>() — кидає, якщо немає"
            :storage    "Dictionary<Type, ScriptableObject>"
            :duplicate  :overwrite
            :release    :none
            :disposable :none}}

 {:task :generic-config-loader
  :listen :config-storage
  :goal "один generic завантажувач; новий конфіг = один рядок в installer"
  :shape "ConfigLoaderSystem<T> : IUniTaskSystem where T : ScriptableObject — завантажує T, storages.Add(config), нічого не звільняє"
  :wiring "builder.Register<ConfigLoaderSystem<X>>(Lifetime.Singleton).As<IUniTaskSystem>().WithParameter(AppState.ConfigLoading) + адреса через WithParameter"
  :address "WithParameter в installer"
  :result "VContainer створює по екземпляру на кожен тип конфіга"}

 {:task :instance-objects-step
  :listen :generic-config-loader
  :goal "окремі системи кроку AppState.InstanceObjects створюють усе похідне від конфігів"
  :contains "валідація SO + похідні об'єкти, які зараз будують завантажувачі"
  :author :agent}

 {:task :migrate-config-consumers
  :listen #{:config-storage :generic-config-loader}
  :do "…ConfigLoaderSystem → рядки в installer-ах; читання → storages.Get<XConfig>(); …ConfigComponent видалити; SingletonArchetypes очистити"
  :runner ?        ;; відповідь «так» на альтернативне питання — прочитано як :agent, перепідтвердити на findings-gate
  :legacy "видалити ConfigLoaderSystem, ConfigLoaderSystemN, ConfigLoadAppState, IUniTaskSystem<T> якщо без споживачів"
  :off-limits "*.unity, .meta, prefabs/.asset"}

 {:task :docs
  :where #{ARCHITECTURE.md PATTERN_CONFIG PATTERN_CONFIG_LOADER ADDRESSABLE_PATTERNS ECS_CONVENTIONS}
  :permission {:ARCHITECTURE.md "дано 2026-09-13"}
  :accept [{:meter "grep ConfigComponent|ConfigLoaderSystem по Assets/**/*.cs" :target "лише ConfigLoaderSystem<T>"}
           {:meter ecs-graph :target "жодного …ConfigComponent серед singleton-компонентів"}
           {:meter di-graph :target "ConfigLoaderSystem<…> зареєстровані як IUniTaskSystem"}
           {:meter "roslyn get_diagnostics" :target "0 errors"}]}]
```

## Amendments (append-only)

```clojure
[{:received-at "2026-09-13"
  :raw-request "1 - сам ScriptableObject\n2 - перенеси ти і виправи ARCHITECTURE.md \n3 - перезапис конфіга\n4 - зроби 1 метод що може звільнити всі конфіги\n5 - всі конфіги будуть ScriptableObject, тому boxing/unboxing не проблема\n6 - треба буде видалити всі ConfigsComponents бо ми тепер переходимо на прямий доступ до самих конфігів."
  :normalized "{:stored :scriptable-object :migration :agent :architecture-permission true :duplicate :overwrite :release \"один метод\" :storage \"boxing ok\" :config-components :delete-all}"
  :confirmed true}
 {:received-at "2026-09-13"
  :raw-request "1 - але box це структура. це не ок. Конфіги використовуються всю ігрову сесію і незмінні від старту до кінця, тому скоріше за все не потрібно навіть робити їм dispose. \n2 - перезапис конфіга малоймовірний\n3 - а\n3 - давай тоді лише T Get<T> який кидає exception якщо такого конфігу немає"
  :normalized "{:add \"Add<T>(T) без Box\" :release :none :duplicate :overwrite-no-special-logic :read-api \"T Get<T>() throws\"}"
  :confirmed true}
 {:received-at "2026-09-13"
  :raw-request "тоді не роби його disposable взагалі.\nА ті 12 завантажувачів що одразу ж звільняють ресурс ми перепишемо. Взагалі зараз з таким підходом як є, ми можемо зробити наступне. ConfigLoader розділити на сутності.\n1 - це буде generic loader який ми будемо просто задавати через installer VContainer. Умовно якщо мені треба завантажити CameraMovementConfig то я маю лише додати це в installer з параметром AppState.ConfigLoading і VContainer сам створить по екземпляру на тип конфіга\n2 - instanceSystem як окремий крок, AppState.InstanceObjects і там вже може бути код який ми напишемо ручками"
  :normalized "{:disposable :none :generic-config-loader {...} :instance-objects-step {...}}"
  :confirmed true}
 {:received-at "2026-09-13"
  :raw-request "1 - б\n2 - так\n3 -  так в цьому й сенс створити окремі системи що будуть займатися створеннями обʼєктів\n4 - так"
  :normalized "{:address :with-parameter :runner ? :instance-objects {:author :agent} :legacy :delete}"
  :confirmed true}
 {:received-at "2026-09-13"
  :raw-request "Для .WithParameter(\"address\", \"MayorConfig\"); ось цих біндів треба зробити окремий статичний клас з константами для всіх конфігів і назначанти з нього, а не зі строчок"
  :normalized "{:task :config-address-constants :shape \"static class, const string per config\" :home ? :const-naming ? :docs ?}"
  :confirmed true}
 {:received-at "2026-09-13"
  :raw-request "1 - поклади там де й решта таких класів\n2 - а\n3 - так"
  :normalized "{:home \"поруч із SystemPriorities (Ecs.Extensions)\" :const-naming :screaming-snake-by-type :docs #{PATTERN_CONFIG_LOADER ADDRESSABLE_PATTERNS}}"
  :confirmed true}]
```

# 2 · Contract

## Findings

```clojure
[{:finding :loader-inventory
  :at "2026-09-13"
  :fact "18 завантажувачів публікують 26 SO (HexIcons — 2, TerrainView — 8) у 30 …ConfigComponent; адреси — константи, 25/26 = назва типу, виняток \"BuildDistrictOutcomesConfig\" для DistrictBuildOutcomesConfig"
  :verified-by "di-graph resolve IUniTaskSystem<ConfigLoadAppState> + читання всіх 18 файлів"
  :consequence "26 рядків у installer-ах з адресою через WithParameter"}
 {:finding :isoline-same-type
  :at "2026-09-13"
  :fact "TerrainViewConfigLoaderSystem завантажує IsolineConfig ДВІЧІ — адреси InnerIsolineConfig і OuterIsolineConfig, один тип SO"
  :verified-by "читання TerrainViewConfigLoaderSystem.cs + grep class IsolineConfig"
  :consequence "сховище з ключем за типом не вміщає два IsolineConfig — потрібне рішення"}
 {:finding :loader-extra-work
  :at "2026-09-13"
  :fact "окрім завантаження: 12 завантажувачів валідують SO (throw на помилках авторингу); TerrainView будує VertexGridComponent з TerrainViewConfig; TerrainGeneration розкладає SO на Mountain/River/Lake/Sea-компоненти і кидає, якщо вкладений конфіг обраного WaterType не призначений"
  :verified-by "читання всіх 18 завантажувачів"
  :consequence "VertexGrid — єдиний похідний об'єкт для InstanceObjects; валідації потрібен дім"}
 {:finding :water-presence-semantics
  :at "2026-09-13"
  :fact "River/Lake/SeaGenerationSubSystem тихо виходять, коли їхнього …ConfigComponent немає — присутність компонента кодує обраний WaterType"
  :verified-by "читання guard-ів у трьох підсистемах"
  :consequence "після міграції вибір = terrainConfig.WaterType; зникнення компонента не можна замінити на Get<T> 1:1"}
 {:finding :read-sites
  :at "2026-09-13"
  :fact "52 Get + 49 Has по 30 компонентах у ~40 файлах; Has-guard: 16 кидають, 21 тихо повертають (return / return false / LogError+false), серед них per-frame CameraMovementSystem і HexSelectionSystem"
  :verified-by "grep Singletons.(Get|Has)<…ConfigComponent> + класифікація наступного рядка"
  :consequence "пари Has+Get зливаються в Get<T>(); тихі пропуски стають винятками — безпечно, бо Boot тікає машину лише після _configsLoaded"}
 {:finding :flatten-transforms
  :at "2026-09-13"
  :fact "FromConfig — чисте копіювання полів, крім: City/Mayor клонують Resources, ClayView Vector2→float2; частина полів перейменована (Mountain.SizeFraction = config.HillSizeFraction)"
  :verified-by "фільтр рядків тіл FromConfig"
  :consequence "читачі переходять на імена полів SO; клон не потрібен — ResourceLoadoutSpawner бере ReadOnlySpan<ResourceAmount>"}
 {:finding :no-burst-consumers
  :at "2026-09-13"
  :fact "жоден …ConfigComponent не потрапляє в IJob/Burst"
  :verified-by "grep IJob|BurstCompile ∩ ConfigComponent"
  :consequence "прямий доступ до SO з main-thread систем не впирається в blittable-обмеження"}
 {:finding :asmdef-reach
  :at "2026-09-13"
  :fact "Ecs.Extensions.asmdef: noEngineReferences=false, посилається на Addressables.Core, Boot.Core, Core"
  :verified-by "читання Ecs.Extensions.asmdef"
  :consequence "ConfigLoaderSystem<T> where T : ScriptableObject і EntityStorages.Add/Get компілюються в Ecs.Extensions без нових посилань"}
 {:finding :boot-wip
  :at "2026-09-13"
  :fact "AppState [Flags] з ConfigLoading/InstanceObjects, IUniTaskSystem : IAppStateSystem з Execute — вже в робочому дереві власника; Boot досі виконує IUniTaskSystem<ConfigLoadAppState>; не-generic IUniTaskSystem не має жодного запускача; WorldInstaller реєструє CameraMovementConfigLoaderSystem (ConfigLoaderSystemN) як IUniTaskSystem<ConfigLoadAppState>, якого той не реалізує"
  :verified-by "читання AppState.cs, IUniTaskSystem.cs, Boot.cs, WorldInstaller.cs + di-graph resolve IUniTaskSystem = 0"
  :consequence "запускач за AppState треба написати; редагування йде поверх незавершених правок власника в Boot"}
 {:finding :validation-same-assembly
  :at "2026-09-13"
  :fact "кожен завантажувач лежить в тій самій збірці, що і його SO (Domains.Economy/Map/Actors, UserInput, Presentation, Presentation.UI); Presentation.UI вже бачить Unity.Collections (завантажувач там використовує NativeHashSet)"
  :verified-by "asmdef_reach assembly-of для 25 типів SO + refs; GUID Unity.Collections у Presentation.UI.asmdef"
  :consequence "перенесення ValidateConfig у сам SO не потребує змін asmdef"}
 {:finding :consumer-reach-unchanged
  :at "2026-09-13"
  :fact "кожен …ConfigComponent лежить у збірці свого SO, тож читачі вже посилаються на збірку SO"
  :verified-by "шляхи файлів компонентів vs SO + asmdef_reach refs"
  :consequence "перехід читачів на SO не потребує нових посилань asmdef"}
 {:finding :installer-wip
  :at "2026-09-13"
  :fact "незавершені правки власника в installer-ах: перейменування ConfigLoadStep → ConfigLoadAppState і Economy Singleton → Scoped"
  :verified-by "git diff installer-ів"
  :consequence "рядки реєстрації завантажувачів замінюються поверх цих правок; решту рядків не чіпаю"}
 {:finding :off-thread-config-reads
  :at "2026-09-13"
  :fact "TerrainViewGenerationSubSystem читає поля IsolineConfig / WindErosionConfig / HeightSmoothingConfig всередині UniTask.RunOnThreadPool (раніше — копії-struct-и)"
  :verified-by "grep RunOnThreadPool + використань полів конфігів у файлі"
  :consequence "дозволено Law 1 (це не доступ до store, дані керовані й незмінні) — записано в PATTERN_CONFIG :off-thread"}
 {:finding :ecs-graph-role-gap
  :at "2026-09-13"
  :fact "ecs-graph класифікує VertexGridSpawnSystem як sub_system — build_graph.py виводить роль із IUniTaskSystem і не знає кроків AppState"
  :verified-by "ecsg.py explain VertexGridSpawnSystem після перебудови"
  :consequence "зв'язок writes → VertexGridComponent правильний; окрема роль startup-кроку — не в scope цієї задачі"}]
```

## Decisions

```clojure
(def decisions
  [{:decision :runner-author :status :confirmed :at "2026-09-13" :value :agent :verified-by "відповідь «3 - так» на перепитування" :reason "запускач за AppState у Boot пише агент"}
   {:decision :isoline-duplicate :status :confirmed :at "2026-09-13" :value "InnerIsolineConfig : IsolineConfig, OuterIsolineConfig : IsolineConfig" :verified-by "відповідь «1 - а»" :reason "ключ за типом лишається простим; перепризначення script у двох .asset — сторона власника"}
   {:decision :validation-home :status :confirmed :at "2026-09-13" :value "SO реалізує інтерфейс з Validate(); generic-завантажувач викликає після завантаження" :verified-by "відповідь «2 - а»" :reason "перевірка поруч із даними, реєстрація — один рядок"}
   {:decision :config-addresses :status :confirmed :at "2026-09-13"
    :value "EcsExtensions.ConfigAddresses (Assets/Scripts/EcsExtensions/ConfigAddresses.cs): 26 const string SCREAMING_SNAKE_CASE за типом конфіга, значення = адреса без змін"
    :verified-by "SystemPriorities — єдиний інший спільний клас констант, лежить в Ecs.Extensions; усі 6 збірок installer-ів посилаються на Ecs.Extensions напряму (asmdef_reach)"
    :reason "адреси в одному місці замість рядкових літералів у 10 installer-ах"}])
```

## Disproven (append-only)

```clojure
[]
```

## Attempted (append-only)

```clojure
[]
```

# 3 · Plan

```clojure
{:status :complete   ;; closed 2026-09-13 by the owner's word — «закривай всі flow-и і коміть всі зміни»
 :completed #{:research-pass-1 :findings-gate :research-pass-2 :plan-go :storage :validation :generic-loader :isoline-subclasses :registrations :instance-step :boot-runner :consumers :delete-components :docs :verify :config-addresses :isoline-asset-repoint :owner-unity-check}
 :current :closed
 :remaining #{}
 :resume-context "CLOSED 2026-09-13: конфіги — ScriptableObject у EntityStorages (Add<T>/Get<T>), завантажує ConfigLoaderSystem<T> за адресою з ConfigAddresses на AppState.ConfigLoading, похідні об'єкти — системи AppState.InstanceObjects (VertexGridSpawnSystem); 30 …ConfigComponent і 18 завантажувачів видалені; IsolineConfig розділений на Inner/Outer, два .asset перепризначені. Власник підтвердив Unity-компіляцію і плейтест."}
```

```clojure
;; harvested 2026-09-13 (Rule 2d): інваріанти → PATTERN_CONFIG, PATTERN_CONFIG_LOADER, ECS_CONVENTIONS (State Storage Taxonomy, storage-registry-law :configs, Error Handling), ARCHITECTURE (shared-kernel, System Taxonomy, boot-flow, Pattern Recipes), ADDRESSABLE_PATTERNS (Config handoff, Conventions); контракти в коді → заголовки EntityStorages, ConfigLoaderSystem<T>, IValidatableConfig, ConfigAddresses, IsolineConfig, VertexGridSpawnSystem, Boot.
;; Прогалина ecs-graph (крок AppState класифікується як sub_system) лишається тільки тут, як історія. Покроковий план 1-11 скинутий — виконаний повністю; record = commit log.
```

## Acceptance

```clojure
[{:meter "grep …ConfigComponent по Assets/**/*.cs" :target "0" :actual "0 (2026-09-13)" :status :passed}
 {:meter "grep ConfigLoaderSystem по Assets/**/*.cs" :target "лише ConfigLoaderSystem<T> і його реєстрації" :actual "так — клас + 26 реєстрацій (2026-09-13)" :status :passed}
 {:meter "ecsg.py — singleton-компоненти" :target "жодного …ConfigComponent" :actual "search ConfigComponent: no node; VertexGridComponent <-writes- VertexGridSpawnSystem (2026-09-13)" :status :passed}
 {:meter "dig.py resolve IUniTaskSystem" :target "26 ConfigLoaderSystem<…> + система VertexGrid" :actual "27 реалізацій: 26 ConfigLoaderSystem<…> + VertexGridSpawnSystem (2026-09-13)" :status :passed}
 {:meter "mcp__roslyn__get_diagnostics по зачеплених файлах" :target "0 errors" :actual "0 errors по всьому FantasyMayor.sln (2026-09-13)" :status :passed}
 {:meter "python3 Tools/doc_lint.py --quiet" :target "ghosts не зросли понад адреси-рядки в цьому FLOW" :actual "25: 8 старих HexIdComponent + історичні імена видалених типів у FLOW_CONFIG_STORAGE / FLOW_CANCELLATION_CONVENTION / FLOW_CASCADE_S2_LAKE; у policy-доках і рецептах нових немає (2026-09-13)" :status :passed}
 {:meter "Unity-компіляція + плейтест (власник)" :target "старт → меню → генерація карти без винятків" :actual "власник: «все працює» (2026-09-13)" :status :passed}]
```
