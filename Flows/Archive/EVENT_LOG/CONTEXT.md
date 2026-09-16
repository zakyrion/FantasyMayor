---
category: A
read: archive
status: closed-by-owner
tags:
  - ecs
  - events
  - cascade
related:
  - "[FLOW](FLOW.md)"
---

# Task

```clojure
{:task :event-log
 :flow "Flows/EVENT_LOG/FLOW.md"
 :stage-reads "лише цей файл і файли з # Reads; FLOW.md і розмови етап s1 не читає"

 :goal "журнал подій як у Kafka / RabbitMQ Streams: кожна система отримує свої непрочитані події без ручного обліку прочитаного; журнал очищується детерміновано — ліміт на тип події, нова подія понад ліміт витісняє найстарішу"

 :result "події живуть у власному журналі (окреме сховище Events у EntityStorages) з лімітом на тип; система бачить кожну подію свого типу рівно раз, поки та не витіснена; споживач після виробника бачить подію в тому ж тіку, перед ним — у наступному; кожен нинішній ланцюжок подій працює як раніше (перевірка власника в Unity)"

 :decided
 [{:id :store                :value "окремий EntityStore для подій, з обмеженою кількістю сутностей на кожен тип події"}
  {:id :inactive-consumer    :value "система неактивного стану, повернувшись, обробляє все, що лишилось у журналі"}
  {:id :retention-capacity   :value "ліміт на тип — ЄДИНЕ правило утримання: нова подія понад ліміт витісняє найстарішу; споживач, що не встиг прочитати, її втрачає; переповнення не кидає виняток"
   :supersedes :retention-min-offset :note "рішення :retention-min-offset (видалення нижче найменшого зміщення, виняток на переповненні) скасоване — не застосовувати"}
  {:id :global-sequence      :value "глобальний монотонний номер події між усіма типами"}
  {:id :entity-links         :value "подія посилається на сутності через PK; PK ніколи не перевикористовуються; Entity-хендлів у подіях немає"}
  {:id :no-conflation        :value "події ніколи не зливаються"}
  {:id :cursor-home          :value "спільний журнал на тип; зміщення кожного споживача веде сховище подій і просуває механізм читання — система зміщень не торкається (модель Kafka auto-commit / RabbitMQ Streams server-side offset)"}
  {:id :commit-timing        :value "подія вважається прочитаною в момент видачі (auto-ack); виняток в обробці її не повертає"}
  {:id :first-read-position  :value "споживач без зміщення читає з найстарішої події в журналі; витиснене зміщення продовжує з найстарішої, що лишилась"}
  {:id :same-tick-delivery   :value "споживач після виробника бачить подію в тому ж тіку, перед ним — у наступному; правило event/no-same-frame скасовується"}
  {:id :capacity-declaration :value "ліміт оголошується атрибутом на структурі події, напр. [EventCapacity(64)]"}
  {:id :app-state-request-as-event :value "запит на зміну стану застосунку стає подією журналу в цьому каскаді; підписка Boot на OnComponentChanged прибирається"}
  {:id :consumer-shape       :value "одна форма споживача: система тримає readonly EventReader<TEvent> і сама читає while (reader.TryRead(out var evt)); окремої бази-споживача немає; роль reactive = «тримає EventReader»"}
  {:id :buffer-shape         :value "дані події — сутність в окремому сховищі Events (EntityStorages); порядок — кільце id сутностей на кожен тип, яке веде журнал; TryRead O(1); витіснення = видалення сутності в голові кільця"}
  {:id :reader-source        :value "EventReader<TEvent> інжектиться DI як Transient: кожна ін'єкція — новий читач із власним курсором, id якого видає журнал; курсор живе в сховищі, читач тримає лише хендл"}
  {:id :skip                 :value "злиття подій (conflation) не робиться"}
  {:id :rule-change-procedure :value "CLAUDE.md § 6: правило спершу змінюється в RULES_SPECIFICATION.md (id не змінюється, нове правило — новий id, зняте лишає id назавжди), потім у кожному carrier, потім у перевірці, що його міряє (повідомлення цитує id), потім fantasymayor-rules-conformance"}]

 :names-allowed "EventReader<TEvent> і сховище Events — єдині імена майбутнього рішення, вже затверджені; інших імен класів і методів s1 не вводить"

 :out-of-scope #{"паузована задача AppStateRunner: порядок стартових станів ConfigLoading → InstanceObjects → MainMenu → MapCreation → Gameplay, шлюз входу GameModeMachine, осідання MapCreation, сід Gameplay — поза цим каскадом; з Boot у цьому каскаді змінюється лише запит на зміну стану (рішення :app-state-request-as-event)"
                 "Unity-side: prefab, .asset, сцени, addressable-записи, .meta"
                 "злиття подій"
                 "збереження подій чи курсорів між сесіями (f13)"
                 "виправлення graph-попереджень, не пов'язаних із подіями (key/data, link/persistent-key, archetype/birth, transaction/one-archetype тощо)"
                 "зміна механік ходу, будівництва району, UI — ланцюжки мають працювати як раніше"
                 "розбиття на методи й класи — робота s2"
                 "тести й тестова інфраструктура"}}
```

# Reads

```clojure
[;; --- ядро нинішньої моделі подій (замінюється) ---
 {:ref "Assets/Scripts/EcsExtensions/EcsEventExtensions.cs" :why "CreateEvent і IsRipe — нинішній контракт підняття й дозрілості" :read :whole :is :number-source}
 {:ref "Assets/Scripts/EcsExtensions/EventArchetypes.cs" :why "архетип події [EventFrameComponent, TEvent] + EventTag" :read :whole :is :type-source}
 {:ref "Assets/Scripts/EcsExtensions/EventCleanupSystem.cs" :why "нинішнє прибирання: запит AllTags EventTag, знімок id, видалення дозрілих" :read :whole :is :number-source}
 {:ref "Assets/Scripts/EcsExtensions/EventFrameComponent.cs" :why "штамп кадру" :read :whole :is :type-source}
 {:ref "Assets/Scripts/EcsExtensions/EventTag.cs" :why "головний тег усіх подій" :read :whole :is :type-source}
 {:ref "Assets/Scripts/EcsExtensions/EntityStorages.cs" :why "реєстр сховищ: World, Singletons, конфіги; сюди лягає Events (f11)" :read :whole :is :integration-point}
 {:ref "Assets/Scripts/EcsExtensions/SingletonComponents.cs" :why "приклад класу, що створює власний EntityStore поруч із World" :read :whole :is :example}
 {:ref "Assets/Scripts/EcsExtensions/UpdatedSystem.cs" :why "база з обов'язковим якорем (архетип або запит) і знімком id; 16 споживачів передають у неї архетип події" :read :whole :is :integration-point}
 {:ref "Assets/Scripts/EcsExtensions/LateUpdatedSystem.cs" :why "та сама форма для LateUpdate; споживачів подій на ній немає" :read :whole :is :type-source}
 {:ref "Assets/Scripts/EcsExtensions/IUpdatedSystem.cs" :why "контракт циклу: Priority + Update(GameState)" :read :whole :is :type-source}
 {:ref "Assets/Scripts/EcsExtensions/AppStateSystemRegistration.cs" :why "RegisterAppStateSystem: Register<T>(lifetime).As<IAppStateSystem>().WithParameter(appState)" :read :whole :is :integration-point}
 {:ref "Assets/Scripts/EcsExtensions/SystemPriorities.cs" :why "RuntimeTick — порядок виробників і споживачів у тіку; EventCleanup = int.MaxValue; коментарі про видимість подій" :read :whole :is :number-source}
 {:ref "Assets/Scripts/EcsExtensions/SystemRoleAttribute.cs" :why "маркер ролі, який нинішня модель вимагає для утримуваного архетипу події" :read :whole :is :type-source}
 {:ref "Assets/Scripts/EcsExtensions/OrchestratorSubSystems.cs" :why "як оркестратор запускає підсистеми (TurnProcessorSystem, DistrictBuildUISystem)" :read :whole :is :integration-point}

 ;; --- цикл застосунку ---
 {:ref "Assets/Modules/Boot/Implementation/Boot.cs" :why "Update: Tick → відкладене перемикання; підписка OnComponentChanged, що прибирається (f6); робоче дерево містить незакомічені зміни паузованої задачі" :read :whole :is :integration-point}
 {:ref "Assets/Modules/Boot/Implementation/GameModeMachine.cs" :why "Switch / Tick / LateTick; як стани отримують тік" :read :whole :is :integration-point}
 {:ref "Assets/Modules/Boot/Implementation/IAppState.cs" :why "контракт стану (RequestedMode вже прибраний з інтерфейсу в робочому дереві)" :read :whole :is :type-source}
 {:ref "Assets/Modules/Boot/Implementation/States/AppStateSystems.cs" :why "Filter / RunEntryAsync / Tick — порядок і склад систем стану" :read :whole :is :integration-point}
 {:ref "Assets/Modules/Boot/Implementation/States/MainMenuState.cs" :why "стан-споживач TerrainGenerationGenerateEventComponent (тримає архетип, опитує «чи є хоч одна дозріла»)" :read :whole :is :integration-point}
 {:ref "Assets/Modules/Boot/Implementation/States/GameplayState.cs" :why "стан-виробник HexIconsVisibilityChangedEvent після входу" :read :whole :is :integration-point}
 {:ref "Assets/Modules/Boot/Implementation/States/MapCreationState.cs" :why "SettleFrames = 3 обґрунтовані прибиранням подій" :read :whole :is :number-source}
 {:ref "Assets/Modules/Boot/Implementation/States/InitializationState.cs" :why "стан, де зараз працюють лише EventCleanupSystem і InitializationSystem" :read :whole :is :example}
 {:ref "Assets/Modules/Boot/Implementation/States/ConfigLoadingState.cs" :why "стан без власних споживачів подій" :read :whole :is :example}
 {:ref "Assets/Modules/Boot/Implementation/States/InstanceObjectsState.cs" :why "те саме" :read :whole :is :example}
 {:ref "Assets/Modules/Boot/Implementation/States/MapLoadingState.cs" :why "стан, де зараз працює лише EventCleanupSystem" :read :whole :is :example}
 {:ref "Assets/Modules/Boot/Implementation/States/GameOverState.cs" :why "те саме" :read :whole :is :example}
 {:ref "Assets/Modules/Boot/Implementation/Systems/InitializationSystem.cs" :why "єдиний нинішній автор запиту на зміну стану (пише AppStateComponent лише на різниці)" :read :whole :is :integration-point}
 {:ref "Assets/Modules/Boot/Core/Components/AppStateComponent.cs" :why "нинішній носій запиту: поле Requested" :read :whole :is :type-source}
 {:ref "Assets/Modules/Boot/Core/Archetypes/CoreArchetypes.cs" :why "архетип сутності запиту AppStateComponent + AppStateTag" :read :whole :is :type-source}
 {:ref "Assets/Modules/Boot/Core/AppState.cs" :why "прапорці станів" :read :whole :is :type-source}
 {:ref "Assets/Modules/Boot/Core/IAppStateSystem.cs" :why "AppState системи" :read :whole :is :type-source}

 ;; --- виробники ---
 {:ref "Assets/Modules/UserInput/Systems/HexSelectionSystem.cs" :why "виробник SelectedHexChangedEvent (Priority 1)" :read :whole :is :number-source}
 {:ref "Assets/Modules/Turn/Systems/TurnProcessorSystem.cs" :why "споживач NextTurnEvent (лише в Idle) і виробник TurnCompletedEvent; [SystemRole(PerFrame)] з утримуваним архетипом" :read :whole :is :number-source}
 {:ref "Assets/Domains/Actions/BuildDistrictAction/Systems/BuildDistrictTurnTickSystem.cs" :why "фаза ходу — виробник BuildDistrictCompleteEvent «за рівнем» (event/lossy-producer)" :read :whole :is :number-source}
 {:ref "Assets/Domains/Actions/BuildDistrictAction/Systems/BuildDistrictActionSystem.cs" :why "споживач DistrictBuildConfirmedEvent; виробник DistrictTableChangedEvent{Planned} і BuildDistrictCompleteEvent при TurnsToBuild 0" :read :whole :is :number-source}
 {:ref "Assets/Domains/Actions/BuildDistrictAction/Systems/BuildDistrictActionCancelSystem.cs" :why "споживач BuildDistrictCancelEvent (кидає, якщо району нема); виробник DistrictTableChangedEvent{Removed}" :read :whole :is :number-source}
 {:ref "Assets/Domains/Actions/BuildDistrictAction/Systems/BuildDistrictCompletionSystem.cs" :why "споживач BuildDistrictCompleteEvent («є хоч одна дозріла» → reconcile); виробник DistrictTableChangedEvent{Built}; [SystemRole(Reactive)]" :read :whole :is :number-source}
 {:ref "Assets/Presentation/UI/DistrictBuild/Systems/DistrictBuildUISystem.cs" :why "споживач DistrictBuildUIRequestedEvent (опитує, табличний якір на view-singleton, [SystemRole(PerFrame)]); виробник DistrictBuildConfirmedEvent з колбеку view" :read :whole :is :number-source}
 {:ref "Assets/Presentation/UI/MainHud/HexInfoPanel/Systems/HexInfoPanelDistrictSystem.cs" :why "споживач трьох типів через AnyComponents у base(...); виробник BuildDistrictCancelEvent з колбеку view" :read :whole :is :number-source}
 {:ref "Assets/Presentation/UI/GeneratorMenu/Views/HexesUI.cs" :why "view-виробник TerrainGenerationGenerateEventComponent (поза тіком)" :read :whole :is :integration-point}
 {:ref "Assets/Presentation/UI/MainHud/ContextTabs/Views/ContextTabsView.cs" :why "view-виробник ContextTabChangedEvent після запису ActiveContextTabComponent" :read :whole :is :integration-point}
 {:ref "Assets/Presentation/UI/MainHud/TurnPanel/Views/TurnPanelView.cs" :why "view-виробник NextTurnEvent; самосторож _processing" :read :whole :is :integration-point}
 {:ref "Assets/Presentation/UI/MainHud/HexInfoPanel/Views/HexInfoPanelView.cs" :why "view-виробник DistrictBuildUIRequestedEvent (рядки 250-257) і C#-подія Cancelled" :read :section :is :integration-point}
 {:ref "Assets/Presentation/UI/MainHud/TurnPanel/Systems/TurnPanelViewSystem.cs" :why "звідки view бере _processing (щокадру, після TurnProcessorSystem)" :read :whole :is :number-source}
 {:ref "Assets/Presentation/UI/GeneratorMenu/Systems/ShowHexesUISystem.cs" :why "UI генератора показується на MainMenu і ніким не ховається" :read :whole :is :number-source}

 ;; --- споживачі ---
 {:ref "Assets/Modules/Turn/Systems/TurnCountSystem.cs" :why "якір TurnCompletedEvent у base(...)" :read :whole :is :example}
 {:ref "Assets/Presentation/HexIcons/Systems/HexIconsVisibilitySystem.cs" :why "якір HexIconsVisibilityChangedEvent" :read :whole :is :example}
 {:ref "Assets/Presentation/Terrain/Systems/HexSelectionViewSystem.cs" :why "якір SelectedHexChangedEvent" :read :whole :is :example}
 {:ref "Assets/Presentation/UI/MainHud/HexInfoPanel/Systems/HexInfoPanelSystem.cs" :why "якір SelectedHexChangedEvent" :read :whole :is :example}
 {:ref "Assets/Presentation/UI/MainHud/HexInfoPanel/Systems/HexInfoPanelHeaderSystem.cs" :why "якір SelectedHexChangedEvent" :read :whole :is :example}
 {:ref "Assets/Presentation/UI/MainHud/HexInfoPanel/Systems/HexInfoPanelResourcesSystem.cs" :why "якір SelectedHexChangedEvent" :read :whole :is :example}
 {:ref "Assets/Presentation/UI/MainHud/ContextTabs/Systems/ContextTabsAvailabilitySystem.cs" :why "якір SelectedHexChangedEvent" :read :whole :is :example}
 {:ref "Assets/Presentation/UI/MainHud/ContextTabs/Systems/ContextTabSelectionSystem.cs" :why "якір ContextTabChangedEvent" :read :whole :is :example}
 {:ref "Assets/Presentation/UI/DistrictBuild/Systems/DistrictBuildListUISubSystem.cs" :why "підсистема, що сама опитує DistrictBuildUIRequestedEvent під час populate" :read :whole :is :number-source}
 {:ref "Assets/Presentation/Districts/Systems/DistrictBuildProgressViewSpawnSystem.cs" :why "якір DistrictTableChangedEvent, reconcile" :read :whole :is :example}
 {:ref "Assets/Presentation/Districts/Systems/DistrictBuildProgressViewDespawnSystem.cs" :why "якір DistrictTableChangedEvent, reconcile" :read :whole :is :example}
 {:ref "Assets/Presentation/Districts/Systems/DistrictViewSpawnSystem.cs" :why "якір DistrictTableChangedEvent, reconcile" :read :whole :is :example}
 {:ref "Assets/Domains/Economy/DistrictOpenCondition/Systems/DistrictOpenConditionEvaluatorTableChangedSystem.cs" :why "якір DistrictTableChangedEvent; зареєстрована Scoped" :read :whole :is :example}
 {:ref "Assets/Presentation/HexResources/Systems/ForestSpawnSystem.cs" :why "сплячий споживач ForestHexAppearedEvent (f4)" :read :whole :is :example}
 {:ref "Assets/Presentation/HexResources/Systems/ForestDespawnSystem.cs" :why "сплячий споживач ForestHexRemovedEvent (f4)" :read :whole :is :example}

 ;; --- типи подій ---
 {:ref "Assets/Flows/DistrictBuild/Events/DistrictBuildUIRequestedEvent.cs" :why "тип без полів" :read :whole :is :type-source}
 {:ref "Assets/Presentation/HexIcons/Events/HexIconsVisibilityChangedEvent.cs" :why "тип без полів" :read :whole :is :type-source}
 {:ref "Assets/Modules/Turn/Events/TurnCompletedEvent.cs" :why "тип без полів" :read :whole :is :type-source}
 {:ref "Assets/Modules/Turn/Events/NextTurnEvent.cs" :why "тип без полів" :read :whole :is :type-source}
 {:ref "Assets/Domains/Actions/BuildDistrictAction/Events/DistrictBuildConfirmedEvent.cs" :why "поля Coords, Type, Payer" :read :whole :is :type-source}
 {:ref "Assets/Domains/Actions/BuildDistrictAction/Events/BuildDistrictCompleteEvent.cs" :why "internal, без полів" :read :whole :is :type-source}
 {:ref "Assets/Domains/Actions/BuildDistrictAction/Events/BuildDistrictCancelEvent.cs" :why "поле Coords (HexCoord, не Entity)" :read :whole :is :type-source}
 {:ref "Assets/Presentation/Terrain/Events/SelectedHexChangedEvent.cs" :why "тип без полів" :read :whole :is :type-source}
 {:ref "Assets/Presentation/HexResources/Events/ForestHexAppearedEvent.cs" :why "тип без полів, без виробника" :read :whole :is :type-source}
 {:ref "Assets/Presentation/HexResources/Events/ForestHexRemovedEvent.cs" :why "тип без полів, без виробника" :read :whole :is :type-source}
 {:ref "Assets/Domains/Economy/District/Events/DistrictTableChangedEvent.cs" :why "поле Change (Planned/Built/Removed), IEquatable" :read :whole :is :type-source}
 {:ref "Assets/Domains/Map/Generation/Components/TerrainGenerationGenerateEventComponent.cs" :why "легасі-суфікс, не в Events/" :read :whole :is :type-source}
 {:ref "Assets/Presentation/UI/MainHud/ContextTabs/Events/ContextTabChangedEvent.cs" :why "тип без полів" :read :whole :is :type-source}

 ;; --- DI ---
 {:ref "Assets/Scripts/Installers/World/WorldInstaller.cs" :why "EntityStorages як RegisterInstance; EventCleanupSystem з усіма 8 прапорцями; InitializationSystem; стани; порядок InstallModules" :read :whole :is :integration-point}
 {:ref "Assets/Modules/Turn/Installer/TurnInstaller.cs" :why "реєстрація TurnProcessorSystem, TurnCountSystem" :read :whole :is :integration-point}
 {:ref "Assets/Domains/Actions/Installer/ActionsInstaller.cs" :why "реєстрація систем будівництва й фаз ходу" :read :whole :is :integration-point}
 {:ref "Assets/Presentation/UI/Installer/UIInstaller.cs" :why "реєстрація UI-систем і підсистем DistrictBuild" :read :whole :is :integration-point}
 {:ref "Assets/Presentation/Districts/Installer/DistrictsInstaller.cs" :why "реєстрація spawn/despawn районів" :read :whole :is :integration-point}
 {:ref "Assets/Presentation/HexIcons/Installer/HexIconsInstaller.cs" :why "реєстрація HexIconsVisibilitySystem" :read :whole :is :integration-point}
 {:ref "Assets/Presentation/HexResources/Installer/HexResourcesViewInstaller.cs" :why "реєстрація Forest-систем" :read :whole :is :integration-point}
 {:ref "Assets/Presentation/Terrain/Installer/TerrainViewInstaller.cs" :why "реєстрація HexSelectionViewSystem" :read :whole :is :integration-point}
 {:ref "Assets/Domains/Economy/Installer/EconomyInstaller.cs" :why "реєстрація DistrictOpenConditionEvaluatorTableChangedSystem як Scoped" :read :whole :is :integration-point}
 {:ref "Library/PackageCache/jp.hadashikick.vcontainer@7ec84530fae8/Runtime/ContainerBuilderExtensions.cs" :why "Register(Type, Lifetime): відкритий generic → OpenGenericRegistrationBuilder (рядки 1-40)" :read :section :is :number-source}
 {:ref "Library/PackageCache/jp.hadashikick.vcontainer@7ec84530fae8/Runtime/Internal/OpenGenericRegistrationBuilder.cs" :why "побудова відкритої реєстрації" :read :whole :is :number-source}
 {:ref "Library/PackageCache/jp.hadashikick.vcontainer@7ec84530fae8/Runtime/Internal/InstanceProviders/OpenGenericInstanceProvider.cs" :why "закрита реєстрація кешується на набір аргументів типу, MakeGenericType, та сама Lifetime" :read :whole :is :number-source}
 {:ref "Library/PackageCache/jp.hadashikick.vcontainer@7ec84530fae8/Runtime/Registry.cs" :why "TryGet → TryGetClosedGenericRegistration (рядки 1-160)" :read :section :is :number-source}
 {:ref "Library/PackageCache/jp.hadashikick.vcontainer@7ec84530fae8/Runtime/Container.cs" :why "ResolveCore: Transient = SpawnInstance без кешу й без обліку IDisposable; Singleton/Scoped — облік (рядки 140-190, 280-310)" :read :section :is :number-source}
 {:ref "Library/PackageCache/jp.hadashikick.vcontainer@7ec84530fae8/Runtime/Internal/ReflectionInjector.cs" :why "кожен параметр конструктора і [Inject]-методу резолвиться окремо" :read :whole :is :number-source}
 {:ref "Library/PackageCache/jp.hadashikick.vcontainer@7ec84530fae8/Runtime/IObjectResolverExtensions.cs" :why "ResolveOrParameter: WithParameter перехоплює лише збіг за типом або ім'ям" :read :section :is :number-source}

 ;; --- Friflo ---
 {:ref "Assets/Packages/Friflo.Engine.ECS.3.6.0/lib/netstandard2.1/Friflo.Engine.ECS.xml" :why "члени: P:EntityStore.RecycleIds, M:Entity.DeleteEntity, M:EntityStore.TryGetEntityById, M:EntityStore.GetEntityById, M:EntityStore.EnsureCapacity, P:EntityStore.Capacity, M:Archetype.EnsureCapacity, M:Archetype.CreateEntity, P:Archetype.Entities, P:ArchetypeQuery.ThrowOnStructuralChange, T:StructuralChangeException, M:EntityStore.CreateEntity(System.Int32), E:EntityStore.OnEntityDelete" :read :section :is :number-source}

 ;; --- правила, carriers, перевірки ---
 {:ref "RULES_SPECIFICATION.md" :why "джерело правил; секції «Конструкції» (:event :event-archetype :cleanup :anchor :system-role :cadence :priority :storages), «Події», «Системи, ролі, маркери, порядок», «Reactive, per-frame, оркестратор», «View і межа view», «Вибір форми»" :read :section :is :number-source}
 {:ref "ARCHITECTURE.md" :why "carrier: секції Systems (:system/definition, system-role, :system/priority-is-order-only), Markers, System state, Collections and memory, Runtime forms (reactive, per-frame, orchestrator), Entities (:table/cross-archetype, :tag/main-tag-unique, :tag/event-tag), Components writes links, Events, View boundary, Threading and structural change" :read :section :is :integration-point}
 {:ref "Patterns/PATTERN_EVENT.md" :why "carrier: рецепт події (підняття, видимість кадр-після, lossy-producer)" :read :whole :is :integration-point}
 {:ref "Patterns/PATTERN_REACTIVE_SYSTEM.md" :why "carrier: якір у base(...), ripe-gate, маркер для утримуваного архетипу" :read :whole :is :integration-point}
 {:ref "Patterns/PATTERN_CLEANUP_SYSTEM.md" :why "carrier: глобальне прибирання" :read :whole :is :integration-point}
 {:ref "Patterns/PATTERN_VIEW_SYSTEM.md" :why "carrier: view не піднімає ECS-подій" :read :whole :is :integration-point}
 {:ref "Patterns/PATTERN_REACTIVE_ORCHESTRATOR_SYSTEM.md" :why "carrier: якір і ripe-gate оркестратора" :read :whole :is :integration-point}
 {:ref "Patterns/PATTERN_PERFRAME_SYSTEM.md" :why "carrier: per-frame з утримуваним архетипом події і [SystemRole(PerFrame)]" :read :whole :is :integration-point}
 {:ref "Patterns/PATTERN_TRANSACTION_ENTITY.md" :why "carrier: скелет із IsRipe (рядки 60-100)" :read :section :is :integration-point}
 {:ref ".claude/skills/fantasymayor-pattern-choice/SKILL.md" :why "carrier: (def role-marker), (def event-kernel) з EventArchetypes / IsRipe" :read :section :is :integration-point}
 {:ref ".claude/skills/fantasymayor-graph/SKILL.md" :why "carrier: опис якоря EventArchetypes.Of і ролей" :read :section :is :integration-point}
 {:ref ".claude/skills/fantasymayor-graph/references/graph-facts.md" :why "carrier: вид event, ребро emits, перевірка event/raise" :read :section :is :integration-point}
 {:ref ".claude/skills/fantasymayor-graph/scripts/ecs_facts.py" :why "перевірка: EVENT_FRAME/EVENT_TAG, anchor_events/held_events, emits за CreateEvent, event/raise" :read :whole :is :integration-point}
 {:ref ".claude/skills/fantasymayor-graph/scripts/roles.py" :why "перевірка: cleanup/reactive/per_frame/маркер, ребра reacts_to/polls, audit_cleanup (event/cleanup)" :read :whole :is :integration-point}
 {:ref ".claude/skills/fantasymayor-graph/scripts/source_reading.py" :why "перевірка: читання сайтів EventArchetypes.Of, AnyComponents, CreateEvent, DeleteEntity (рядки 340-410)" :read :section :is :integration-point}
 {:ref ".claude/skills/fantasymayor-graph/scripts/tag_law.py" :why "перевірка: EventTag без EventFrameComponent, event без архетипу" :read :whole :is :integration-point}
 {:ref ".claude/skills/fantasymayor-graph/scripts/di_facts.py" :why "ребро injects вже несе аргументи типу (args) — основа ролі «тримає EventReader»" :read :whole :is :integration-point}
 {:ref ".claude/skills/fantasymayor-graph/scripts/recipes.py" :why "сигнатури PATTERN_REACTIVE_SYSTEM / PERFRAME / CLEANUP / EVENT" :read :section :is :integration-point}
 {:ref "Tools/MarkerShapeAnalyzer/MarkedTypeCheck.cs" :why "перевірка: подієвий якір і утримуваний архетип за викликом EventArchetypes.Of / AnyComponents; Cleanup-форма" :read :whole :is :integration-point}
 {:ref "Tools/MarkerShapeAnalyzer/MarkerShapeAnalyzer.cs" :why "FM1001, FM1002, FM1005, FM1006 міряють подієвий якір" :read :whole :is :integration-point}
 {:ref "Tools/MarkerShapeAnalyzer/MarkerVocabulary.cs" :why "EventArchetypes, EventCleanupSystem у словнику аналізатора" :read :whole :is :integration-point}
 {:ref "Tools/MarkerShapeAnalyzer/TypeShape.cs" :why "EventAnchored, HeldEventArchetypes, Cleanup" :read :whole :is :integration-point}
 {:ref "~/.claude/skills/arch-check/SKILL.md" :why "перевірка: визначення системи з EventCleanupSystem, каденція, що є станом (readonly-хендли)" :read :section :is :integration-point}
 {:ref "CLAUDE.md" :why "§ 2 code-verification, § 3 заборони, § 6 процедура зміни правила" :read :section :is :integration-point}]
```

# Search

```clojure
[{:for "усі сайти нинішнього API подій" :where "rg -n \"CreateEvent\\(|IsRipe\\(|EventArchetypes.Of<|AnyComponents\" Assets --type cs"
  :settles "повноту переліку виробників і споживачів, які s1 переводить на журнал"}
 {:for "ролі й ребра подій у графі" :where "python3 .claude/skills/fantasymayor-graph/scripts/fmgraph.py explain <Class> | systems --role reactive | systems --role per_frame"
  :settles "які класи сьогодні reactive за якорем, які за маркером, хто polls"}
 {:for "усі згадки моделі «кадр після / дозрілість / прибирання» у carriers"
  :where "rg -n -i \"ripe|one-frame|EventCleanup|EventArchetypes|frame after|no-same-frame\" ARCHITECTURE.md Patterns .claude/skills INDEX.md"
  :settles "поверхню зміни carriers за CLAUDE.md § 6"}
 {:for "члени Friflo, яких немає в # Facts (наприклад порядок створення, CreateEntity з компонентами)"
  :where "Friflo.Engine.ECS.xml — python-пошук за <member name=…>; dll без джерел"
  :settles "операції кільця id і витіснення в окремому сховищі"}
 {:for "поведінка резолву відкритого generic у VContainer 1.18.0"
  :where "Library/PackageCache/jp.hadashikick.vcontainer@7ec84530fae8/Runtime — лише локальні джерела, без вебу"
  :settles "як EventReader<TEvent> народжується і скільки екземплярів отримує споживач"}]
```

# Facts

```clojure
[;; ===== нинішня модель =====
 {:fact "CreateEvent<T>(store, payload) створює сутність у переданому сховищі одним викликом store.CreateEntity(EventFrameComponent{Frame = Time.frameCount}, payload, Tags.Get<EventTag>()); IsRipe(entity) = Frame < Time.frameCount"
  :at "2026-09-16" :verified-by "прочитано Assets/Scripts/EcsExtensions/EcsEventExtensions.cs цілком"
  :consequence "нинішня подія невидима в кадрі народження; журнал замінює штамп кадру глобальним номером і курсорами"}
 {:fact "EventArchetypes.Of<T>(store) = store.GetArchetype(ComponentTypes.Get<EventFrameComponent, T>(), Tags.Get<EventTag>()); усі події мають один головний тег EventTag і різняться компонентом"
  :at "2026-09-16" :verified-by "прочитано EventArchetypes.cs і EventTag.cs; ARCHITECTURE.md :tag/main-tag-unique називає EventTag єдиним винятком"
  :consequence "«ліміт на кожен event tag» у рішенні :store фактично означає ліміт на тип події (компонент), бо тег у всіх один — див. питання власнику"}
 {:fact "EventCleanupSystem: IUpdatedSystem, Priority RuntimeTick.EventCleanup = int.MaxValue, запит World.Query().AllTags(EventTag), знімок id дозрілих у NativeList<int> Allocator.Temp, потім DeleteEntity; зареєстрована Singleton з усіма 8 прапорцями AppState"
  :at "2026-09-16" :verified-by "прочитано EventCleanupSystem.cs, SystemPriorities.cs, WorldInstaller.cs рядки 69-71"
  :consequence "за :retention-capacity глобальне прибирання за часом зникає; витіснення відбувається при записі"}
 {:fact "API подій має 14 сайтів CreateEvent над 11 типами, 23 виклики IsRipe (21 клас-споживач + MainMenuState + EventCleanupSystem), 21 виклик EventArchetypes.Of<…> (16 у base(...), 5 утримуваних: TurnProcessorSystem, BuildDistrictCompletionSystem, DistrictBuildUISystem, DistrictBuildListUISubSystem, MainMenuState) і 1 AnyComponents у base(...) (HexInfoPanelDistrictSystem)"
  :at "2026-09-16" :verified-by "rg -n з підрахунком по Assets --type cs; кожен файл-сайт прочитано цілком"
  :consequence "f2 у FLOW.md недолічує (12 типів, 19 споживачів) — фактичні числа тут; оголошено 13 типів подій, 2 з них сплячі"}
 {:fact "UpdatedSystem має лише два конструктори: (appState, store, archetype) і (appState, query); Update(GameState) робить знімок id якоря в NativeList<int> Temp і викликає Update(state, entity) на кожну сутність; окремої форми без якоря немає"
  :at "2026-09-16" :verified-by "прочитано UpdatedSystem.cs і LateUpdatedSystem.cs цілком"
  :consequence "16 споживачів із якорем у base(...) не можуть зберегти цю базу без якоря; :consumer-shape каже «окремої бази-споживача немає» — на чому стоять ці системи, вирішує s1/s2"}
 {:fact "AppStateSystems.Filter сортує IUpdatedSystem за Priority і Tick викликає Update кожної з них щокадру незалежно від наявності подій; порожні списки не кидають (виняток лише для системи без жодного виду)"
  :at "2026-09-16" :verified-by "прочитано AppStateSystems.cs цілком"
  :consequence "система-читач тікає щокадру і сама вирішує, чи є що читати; прибрання EventCleanupSystem залишить GameOverState і MapLoadingState без систем — це допустимо"}
 {:fact "Boot.Update: _machine.Tick(state), потім, якщо є _pendingMode і він ≠ CurrentMode, _machine.Switch; LateUpdate: LateTick. MainMenuState.Tick перевіряє запит ДО _systems.Tick"
  :at "2026-09-16" :verified-by "прочитано Boot.cs і MainMenuState.cs цілком"
  :consequence "перемикання стану завжди після тіку; стан-споживач читає до систем свого тіку"}
 {:fact "робоче дерево містить незакомічені зміни паузованої задачі: IAppState більше не має RequestedMode, Boot його не читає, GameModeMachine тікає стан без шлюзу завершення входу; MainMenuState і MapCreationState досі обчислюють RequestedMode, але його ніхто не читає; єдиний автор запиту — InitializationSystem (Requested = ConfigLoading)"
  :at "2026-09-16" :verified-by "git diff на Boot.cs, GameModeMachine.cs, IAppState.cs, WorldInstaller.cs; rg AppStateComponent|RequestedMode по Assets"
  :consequence "переходи після ConfigLoading зараз не проводяться — це паузована задача, не цей каскад; цей каскад переводить на подію лише наявний запит і прибирає OnComponentChanged"}
 {:fact "Boot у Construct створює сутність CoreArchetypes.AppStateArchetype (AppStateComponent + AppStateTag) і підписується на OnComponentChanged; обробник пише _pendingMode; OnDestroy відписується і кидає, якщо сутності нема. InitializationSystem (UpdatedSystem, якір — цей архетип, Priority 2, лише Initialization) пише AppStateComponent{ConfigLoading} тільки коли значення інше"
  :at "2026-09-16" :verified-by "прочитано Boot.cs, InitializationSystem.cs, CoreArchetypes.cs, AppStateComponent.cs; WorldInstaller.cs рядок 76"
  :consequence "запит як подія: автор піднімає її один раз (інакше щотіку); Boot — MonoBehaviour, не система, і він стає читачем"}
 {:fact "EntityStorages у конструкторі створює World = new EntityStore() і Singletons = new SingletonComponents(...), а SingletonComponents створює ще один власний new EntityStore(); EntityStorages реєструється в DI як RegisterInstance у WorldInstaller"
  :at "2026-09-16" :verified-by "прочитано EntityStorages.cs, SingletonComponents.cs, WorldInstaller.cs рядки 51-54"
  :consequence "кілька EntityStore в одному процесі вже працюють, ComponentTypes/Tags спільні між сховищами; сховище Events лягає в той самий конструктор без нового DI-типу"}
 {:fact "усі 14 сайтів CreateEvent виконуються на головному потоці: у тілах Update, у синхронних фазах ходу, у C#-колбеках UI, у GameplayState після RunEntryAsync; RunOnThreadPool є лише в TerrainViewGenerationSubSystem і TerrainViewTextureSubSystem, які подій не піднімають"
  :at "2026-09-16" :verified-by "rg UniTask.Run|RunOnThreadPool|SwitchToThreadPool по Assets/Domains, Presentation, Modules, Scripts, Flows; прочитано кожен сайт CreateEvent"
  :consequence "журнал не потребує потокобезпечності; :thread/main-only лишається"}
 {:fact "фази ходу синхронні: MayorAPRestoreSubSystem і BuildDistrictTurnTickSystem повертають UniTask.CompletedTask, DistrictOpenConditionEvaluatorSystem — async без жодного await; отже TurnProcessorSystem.RunTurnAsync завершується всередині того самого Update і ставить Status = Completed до повернення"
  :at "2026-09-16" :verified-by "прочитано TurnProcessorSystem.cs, MayorAPRestoreSubSystem.cs, BuildDistrictTurnTickSystem.cs, DistrictOpenConditionEvaluatorSystem.cs рядки 25-45"
  :consequence "Running ніколи не спостерігається TurnPanelViewSystem (1020), тож самосторож TurnPanelView._processing фактично не спрацьовує; вікно «зайнятості» процесора — кадр із Status Completed"}
 {:fact "UI генератора (HexesUI) показується ShowHexesUISystem на MainMenu і ніде не ховається: Hide() не має жодного виклику"
  :at "2026-09-16" :verified-by "прочитано ShowHexesUISystem.cs; rg .Hide()/.Show() по Assets для HexesUI/_uiBox — нуль викликів"
  :consequence "кнопка Generate може піднімати TerrainGenerationGenerateEventComponent у MapCreation і Gameplay, де споживач MainMenuState не тікає (гіпотеза H6: чи кнопка справді доступна — Unity-side)"}
 {:fact "жоден код не просить MainMenu, MapCreation, MapLoading чи GameOver після Gameplay; GameplayState.RequestedMode = null"
  :at "2026-09-16" :verified-by "rg AppState.(MainMenu|MapCreation|GameOver|MapLoading) поза інсталерами й States; прочитано GameplayState.cs"
  :consequence "повторний вхід у Gameplay з тим самим контейнером сьогодні недосяжний; ризик застарілих подій між сесіями латентний (H5)"}

 ;; ===== інвентар за типом події (пріоритети — SystemPriorities.RuntimeTick) =====
 {:fact "SelectedHexChangedEvent (без полів): виробник HexSelectionSystem (система, Priority 1, Gameplay, кожна зміна виділення); споживачі з якорем у base(...): HexSelectionViewSystem 501, HexInfoPanelSystem 550, HexInfoPanelHeaderSystem 560, ContextTabsAvailabilitySystem 562, HexInfoPanelResourcesSystem 563; через AnyComponents: HexInfoPanelDistrictSystem 1030; усі reconcile зі станом HexSelectedComponent"
  :at "2026-09-16" :verified-by "прочитано всі сім файлів і SystemPriorities.cs"
  :consequence "усі споживачі ПІСЛЯ виробника → за :same-tick-delivery реагують у тому ж тіку (сьогодні — у наступному кадрі); reconcile робить це безпечним"}
 {:fact "DistrictTableChangedEvent (поле Change): виробники BuildDistrictActionSystem 600 {Planned}, BuildDistrictActionCancelSystem 602 {Removed}, BuildDistrictCompletionSystem 603 {Built} — усі системи Gameplay; споживачі з якорем: DistrictBuildProgressViewSpawnSystem 601, DistrictBuildProgressViewDespawnSystem 604, DistrictViewSpawnSystem 605, DistrictOpenConditionEvaluatorTableChangedSystem 610 (Scoped); через AnyComponents: HexInfoPanelDistrictSystem 1030; усі ігнорують Change і роблять reconcile"
  :at "2026-09-16" :verified-by "прочитано всі вісім файлів і EconomyInstaller.cs рядок 59"
  :consequence "ProgressViewSpawn (601) стоїть між Planned (600) і Built (603) — див. H3; значення Change сьогодні ніхто не читає"}
 {:fact "BuildDistrictCompleteEvent (internal, без полів): виробники BuildDistrictTurnTickSystem (фаза ходу всередині TurnProcessorSystem 1000, «за рівнем» — щоходу, поки є TurnsLeft ≤ 0) і BuildDistrictActionSystem 600 (коли TurnsToBuild == 0); споживач BuildDistrictCompletionSystem 603 — утримує архетип, [SystemRole(Reactive)], перевіряє «є хоч одна дозріла» і reconcile над усіма in-progress"
  :at "2026-09-16" :verified-by "прочитано BuildDistrictTurnTickSystem.cs, BuildDistrictActionSystem.cs, BuildDistrictCompletionSystem.cs"
  :consequence "з фази ходу подія дійде в наступному тіку (603 < 1000), з 600 — у тому ж; кілька подій в одному тіку сьогодні зливаються в один reconcile (H2)"}
 {:fact "DistrictBuildConfirmedEvent (Coords, Type, Payer): виробник DistrictBuildUISystem.OnConfirmed — C#-колбек view, поза тіком; споживач BuildDistrictActionSystem 600 з якорем — діє на значеннях кожної події окремо (списує ціну й створює рядки)"
  :at "2026-09-16" :verified-by "прочитано DistrictBuildUISystem.cs, BuildDistrictActionSystem.cs, DistrictBuildConfirmedEvent.cs"
  :consequence "кожна подія = окрема дія; кількість реакцій дорівнює кількості подій і сьогодні, і в журналі"}
 {:fact "BuildDistrictCancelEvent (Coords): виробник HexInfoPanelDistrictSystem.OnCancelled — C#-колбек view, сторож перевіряє Planned; споживач BuildDistrictActionCancelSystem 602 з якорем кидає, якщо району на Coords немає"
  :at "2026-09-16" :verified-by "прочитано HexInfoPanelDistrictSystem.cs рядки 175-185, BuildDistrictActionCancelSystem.cs рядки 75-102"
  :consequence "подвійне скасування до реакції кидає і сьогодні, і в журналі (еквівалентно); застаріла подія з іншого світу кинула б — H5"}
 {:fact "NextTurnEvent (без полів): виробник TurnPanelView.OnClicked — view, поза тіком, самосторож _processing; споживач TurnProcessorSystem 1000 — [SystemRole(PerFrame)], утримує архетип і читає «є хоч одна дозріла» ЛИШЕ в гілці Status == Idle"
  :at "2026-09-16" :verified-by "прочитано TurnPanelView.cs рядки 162-169, TurnProcessorSystem.cs цілком"
  :consequence "дві дивергенції: H1 (подія під час не-Idle) і H2 (кілька подій → один хід)"}
 {:fact "TurnCompletedEvent (без полів): виробник TurnProcessorSystem 1000 у кадрі Completed → Idle; споживачі TurnCountSystem 1010 (якір, інкремент лічильника на КОЖНУ подію) і HexInfoPanelDistrictSystem 1030 (AnyComponents, reconcile)"
  :at "2026-09-16" :verified-by "прочитано TurnProcessorSystem.cs, TurnCountSystem.cs, HexInfoPanelDistrictSystem.cs"
  :consequence "обидва споживачі після виробника → той самий тік; коментар TurnCountSystem «читає в тому ж кадрі» стає правдою лише тепер"}
 {:fact "DistrictBuildUIRequestedEvent (без полів): виробник HexInfoPanelView.OnBuildClicked — view, поза тіком; споживачі DistrictBuildUISystem 566 (табличний якір на view-singleton + утримуваний архетип, [SystemRole(PerFrame)], «є хоч одна дозріла» → Open, який виходить мовчки без виділеного гексу) і DistrictBuildListUISubSystem (підсистема, утримує архетип, «є хоч одна дозріла» → дефолтний вибір; викликається всередині Open → PopulateSections і повторно з кліку рядка через Repopulate)"
  :at "2026-09-16" :verified-by "прочитано DistrictBuildUISystem.cs, DistrictBuildListUISubSystem.cs, HexInfoPanelView.cs рядки 250-257"
  :consequence "підсистема читає ту саму подію в межах того ж виклику, що й оркестратор — із власним курсором (H4); :orchestrator/ripe-once забороняє підсистемі перевіряти дозрілість повторно, а вона це робить"}
 {:fact "TerrainGenerationGenerateEventComponent (без полів, легасі-суфікс, лежить у Components/): виробник HexesUI.GenerateHexes — view, поза тіком; споживач MainMenuState — IAppState, утримує архетип, «є хоч одна дозріла» → _requestedMode = MapCreation"
  :at "2026-09-16" :verified-by "прочитано HexesUI.cs, MainMenuState.cs, TerrainGenerationGenerateEventComponent.cs; fmgraph check — попередження event/declare на цьому типі"
  :consequence "споживач — не система; за :consumer-shape читач тримає система — див. питання власнику"}
 {:fact "ContextTabChangedEvent (без полів): виробник ContextTabsView.Emit — view, поза тіком, після Singletons.Set(ActiveContextTabComponent); споживач ContextTabSelectionSystem 561 з якорем, reconcile"
  :at "2026-09-16" :verified-by "прочитано ContextTabsView.cs, ContextTabSelectionSystem.cs"
  :consequence "без дивергенції; ідемпотентно"}
 {:fact "HexIconsVisibilityChangedEvent (без полів): виробник GameplayState.SeedStartOfPlay після RunEntryAsync (стан, не система); споживач HexIconsVisibilitySystem 800 з якорем, clear-and-rebuild"
  :at "2026-09-16" :verified-by "прочитано GameplayState.cs, HexIconsVisibilitySystem.cs"
  :consequence "подія народжується поза тіком і читається в першому тіку Gameplay"}
 {:fact "ForestHexAppearedEvent і ForestHexRemovedEvent: без виробника; споживачі ForestSpawnSystem 599 і ForestDespawnSystem 606 з якорем, reconcile; ForestSpawnSystem мовчки виходить без TerrainTextureComponent"
  :at "2026-09-16" :verified-by "rg CreateEvent — сайтів нема; прочитано обидва файли"
  :consequence "читач без виробника мусить існувати (event/dormant-consumer)"}
 {:fact "view-виробники HexesUI, ContextTabsView, TurnPanelView, HexInfoPanelView отримують EntityStorages через [Inject] Construct і піднімають ECS-подію; fmgraph check показує на кожному попередження view/boundary"
  :at "2026-09-16" :verified-by "прочитано чотири view; python3 fmgraph.py check (базова лінія до змін)"
  :consequence "журнал мусить приймати запис ззовні циклу систем; чи виправляти view/boundary в цьому каскаді — питання власнику"}
 {:fact "мовчазні виходи після прочитання події вже є: ContextTabsAvailabilitySystem і ContextTabSelectionSystem без ContextTabsViewComponent, HexInfoPanel*-системи без view, ForestSpawnSystem без текстури — подія в них губиться і сьогодні (прибирання наступного кадру)"
  :at "2026-09-16" :verified-by "прочитано відповідні файли"
  :consequence "за :commit-timing (auto-ack) поведінка еквівалентна; не дивергенція"}

 ;; ===== DI (VContainer 1.18.0) =====
 {:fact "Register(typeof(Open<>), lifetime) створює OpenGenericRegistrationBuilder; при резолві EventReader<X> Registry.TryGet не знаходить закритий тип, бере відкритий, і OpenGenericInstanceProvider.GetClosedRegistration повертає закриту Registration (MakeGenericType, кеш на набір аргументів типу) з ТІЄЮ Ж Lifetime"
  :at "2026-09-16" :verified-by "прочитано ContainerBuilderExtensions.cs рядки 1-40, OpenGenericRegistrationBuilder.cs, OpenGenericInstanceProvider.cs, Registry.cs рядки 1-160 з PackageCache jp.hadashikick.vcontainer@7ec84530fae8 (package.json version 1.18.0)"
  :consequence "одна відкрита реєстрація Transient покриває всі типи подій"}
 {:fact "Container.ResolveCore: Lifetime.Transient → registration.SpawnInstance(this) на кожен резолв, без кешу і без додавання IDisposable у disposables; Singleton і Scoped кешуються й відстежуються для Dispose"
  :at "2026-09-16" :verified-by "прочитано Container.cs рядки 150-185 і 285-305"
  :consequence "кожна ін'єкція — новий читач (підтверджує :reader-source); контейнер ніколи не звільнить Transient-читача — курсор живе до кінця процесу, узгоджено з f13"}
 {:fact "ReflectionInjector.CreateInstance резолвить кожен параметр конструктора окремо через ResolveOrParameter; InjectMethods робить те саме для [Inject]-методів; WithParameter(appState) з RegisterAppStateSystem — TypedParameter, що збігається лише з параметром типу AppState"
  :at "2026-09-16" :verified-by "прочитано ReflectionInjector.cs, IObjectResolverExtensions.cs рядки 41-66, Internal/InjectParameter.cs Match"
  :consequence "Singleton-система, Scoped-система, підсистема, IAppState і MonoBehaviour з [Inject] отримують по читачу на параметр; два параметри одного типу події — два незалежні курсори"}
 {:fact "у VContainer 1.18.0 runtime немає перевірки захоплених залежностей (Transient у Singleton дозволено без попередження)"
  :at "2026-09-16" :verified-by "grep -rn «aptive|lifetime» по Runtime/*.cs — лише параметри Register"
  :consequence "Transient-читач у Singleton-системі — легальна конфігурація"}
 {:fact "у проєкті сьогодні немає жодної відкритої generic-реєстрації і жодної Lifetime.Transient"
  :at "2026-09-16" :verified-by "rg Transient і typeof(…<>) по Assets поза Plugins"
  :consequence "EventReader<TEvent> — перша така реєстрація; fmgraph di_facts читає реєстрації за Register<T> — чи побачить Register(typeof(X<>)), треба перевірити при зміні графа"}
 {:fact "fmgraph di_facts.connect_injections пише ребро injects з args = аргументи типу параметра для конструкторів зареєстрованих типів і для [Inject]-методів"
  :at "2026-09-16" :verified-by "прочитано .claude/skills/fantasymayor-graph/scripts/di_facts.py рядки 61-80"
  :consequence "роль «тримає EventReader<T>» і ребро споживач → подія виводяться з injects без нової ознаки (підтверджує :reader-source)"}
 {:fact "увесь граф систем будується в момент ін'єкції Boot: Boot.Construct ← GameModeMachine ← IReadOnlyList<IAppState> ← кожен стан бере IReadOnlyList<IAppStateSystem> у конструкторі; Boot.Start робить перший Switch"
  :at "2026-09-16" :verified-by "прочитано Boot.cs, GameModeMachine.cs, конструктори всіх станів, WorldInstaller.cs рядки 80-91"
  :consequence "читачі систем і станів народжуються до першої події — :first-read-position на старті не має чого пропускати; точний момент ін'єкції Boot відносно RegisterBuildCallback не перевірено (сцена заборонена)"}

 ;; ===== Friflo 3.6.0 =====
 {:fact "EntityStore.RecycleIds = true за замовчуванням: id видаленої сутності видається новій"
  :at "2026-09-16" :verified-by "Friflo.Engine.ECS.xml, P:Friflo.Engine.ECS.EntityStore.RecycleIds"
  :consequence "кільце може тримати лише id живих сутностей; зміщення курсора — глобальний номер, не id"}
 {:fact "Entity.DeleteEntity переводить екземпляр у detached (подальші виклики — NullReferenceException) і виконується за O(1), якщо в сутності немає дітей"
  :at "2026-09-16" :verified-by "Friflo.Engine.ECS.xml, M:Friflo.Engine.ECS.Entity.DeleteEntity"
  :consequence "витіснення голови кільця — O(1); видана читачу подія не може переживати витіснення як Entity"}
 {:fact "EntityStore.TryGetEntityById(id, out entity) повертає true для id < Capacity, але сутність може бути null (IsNull == true); GetEntityById так само; Entity.IsNull — true для видаленої"
  :at "2026-09-16" :verified-by "Friflo.Engine.ECS.xml: M:EntityStore.TryGetEntityById, M:EntityStore.GetEntityById, P:Entity.IsNull"
  :consequence "перевірка живучості за id вимагає IsNull, а не лише результату TryGet"}
 {:fact "є EntityStore.EnsureCapacity(int), P:EntityStore.Capacity, Archetype.EnsureCapacity(int), Archetype.CreateEntity(), EntityStore.CreateEntity(int id), подія EntityStore.OnEntityDelete"
  :at "2026-09-16" :verified-by "Friflo.Engine.ECS.xml, відповідні <member name>"
  :consequence "ємність кілець відома з атрибута — сховище можна заздалегідь розмітити; OnEntityDelete — спостерігач, який :event/no-change-observers забороняє"}
 {:fact "Archetype.Entities і EntityStore.Entities задокументовані як «mainly used for debugging»; порядок вставки не гарантовано"
  :at "2026-09-16" :verified-by "Friflo.Engine.ECS.xml: P:Archetype.Entities, P:EntityStore.Entities (f10)"
  :consequence "порядок подій веде кільце, не перебір архетипу"}
 {:fact "StructuralChangeException кидає ArchetypeQuery свого сховища (ThrowOnStructuralChange, типово true); структурна зміна — Add/Remove компонента чи тегу; народження архетипом і видалення нею не є"
  :at "2026-09-16" :verified-by "Friflo.Engine.ECS.xml: T:StructuralChangeException, P:ArchetypeQuery.ThrowOnStructuralChange; ARCHITECTURE.md :structural/delete (f12)"
  :consequence "запис і витіснення в Events не заважають системам, що перебирають World"}
 {:fact "конструктор EntityStore і generic-перевантаження CreateEntity з компонентами в XML не задокументовані, але вживаються в коді проєкту (new EntityStore() двічі, store.CreateEntity(c1, c2, tags) у CreateEvent)"
  :at "2026-09-16" :verified-by "python-пошук <member name> з EntityStore.#ctor / CreateEntity`` — нуль; прочитано EntityStorages.cs, SingletonComponents.cs, EcsEventExtensions.cs"
  :consequence "ці виклики доведені робочим кодом, не документацією"}

 ;; ===== поверхня зміни правил =====
 {:fact "правила в RULES_SPECIFICATION.md, що кодують нинішню модель і стають предметом зміни: :event/is (однокадрова), :event/raise (штамп кадру), :event/ripe, :event/anchor, :event/no-same-frame, :event/cleanup, :event/lossy-producer, :event/startup-bulk (однокадрові не переживають конвеєр), :tag/event-tag, :tag/main-tag-unique (виняток EventTag), :table/cross-archetype (обхід EventTag), :system/definition (EventCleanupSystem), :system/role-order, :system/marker-required, :system/marker-forbidden, :system/marker-value, :reactive/shape, :orchestrator/ripe-once, :choose/behavior-recipe (прибирає події → cleanup), :system/cadence (reactive на кожну подію); конструкції :event, :event-archetype, :cleanup, :anchor, :system-role, :cadence, :priority, :storages"
  :at "2026-09-16" :verified-by "grep :id і секції «Конструкції», «Події», «Системи, ролі…», «Reactive…», «Вибір форми» у RULES_SPECIFICATION.md"
  :consequence "за :decided і CLAUDE.md § 6 ці id змінюються або знімаються (зняті лишають id назавжди), нові правила отримують нові id"}
 {:fact "правила, які нова модель мусить зберегти: :event/declare, :event/suffix, :event/reaction, :event/dormant-consumer, :event/no-change-observers, :event/no-tag-on-persistent-row, :event/one-way, :thread/main-only, :system/priority-is-order-only, :view/boundary, :view/ecs-only-across-a-boundary, :state/no-instance-state, :state/is-not-state, :alloc/repeated-zero"
  :at "2026-09-16" :verified-by "прочитано ARCHITECTURE.md секції Events, Systems, System state, Collections and memory, View boundary; f1 у FLOW.md"
  :consequence "курсор у сховищі, а не в полі системи — щоб readonly EventReader лишався «store handle» за :state/is-not-state"}
 {:fact "carriers нинішньої моделі: ARCHITECTURE.md (Systems, Markers, Runtime forms, Entities, Events), Patterns/PATTERN_EVENT.md, PATTERN_REACTIVE_SYSTEM.md, PATTERN_CLEANUP_SYSTEM.md, PATTERN_REACTIVE_ORCHESTRATOR_SYSTEM.md, PATTERN_PERFRAME_SYSTEM.md, PATTERN_TRANSACTION_ENTITY.md, PATTERN_VIEW_SYSTEM.md, PATTERN_COMPONENT.md (рядок 70), .claude/skills/fantasymayor-pattern-choice/SKILL.md (role-marker, event-kernel), .claude/skills/fantasymayor-graph/SKILL.md і references/graph-facts.md, .claude/skills/fantasymayor-placement/SKILL.md (Events/ «one-frame»), INDEX.md (описи трьох рецептів — генерована зона)"
  :at "2026-09-16" :verified-by "rg -il ripe|EventArchetypes|EventCleanup|one-frame|CreateEvent по Patterns, .claude/skills, INDEX.md, DOC_STANDARD.md"
  :consequence "кожен carrier мусить сказати нове правило повністю; INDEX.md оновлюється через gen_index, не руками між маркерами"}
 {:fact "перевірки, що міряють нинішню модель: fmgraph ecs_facts.py (EVENT_FRAME/EVENT_TAG, is_event за EventFrameComponent, anchor_events/held_events, emits за CreateEvent, event/raise), roles.py (cleanup за обходом EventTag + DeleteEntity, reactive за якорем, маркер для утримуваного, reacts_to/polls, audit_cleanup з event/cleanup і int.MaxValue), source_reading.py (сайти EventArchetypes.Of, AnyComponents, CreateEvent), tag_law.py (EventTag без EventFrameComponent), recipes.py (сигнатури reactive/per_frame/cleanup/event); MarkerShapeAnalyzer (MarkedTypeCheck за EventArchetypes.Of і AnyComponents, TypeShape.EventAnchored/HeldEventArchetypes/Cleanup, FM1001, FM1002, FM1005, FM1006; словник EventArchetypes і EventCleanupSystem); arch-check SKILL.md (EventCleanupSystem у визначенні системи й каденції)"
  :at "2026-09-16" :verified-by "grep по .claude/skills/fantasymayor-graph/scripts/*.py, Tools/MarkerShapeAnalyzer/*.cs, ~/.claude/skills/arch-check/SKILL.md; прочитано roles.py цілком і ecs_facts.py рядки 75-225"
  :consequence "за :consumer-shape маркер [SystemRole] для подій втрачає предмет — аналізатор і граф змінюються разом зі специфікацією; збірка Tools/MarkerShapeAnalyzer поза забороною, dll лежить у Assets/Scripts/EcsExtensions/MarkerShapeAnalyzer.dll"}
 {:fact "базова лінія fmgraph check до змін: integrity clean; попередження на подіях — лише event/declare (TerrainGenerationGenerateEventComponent) і view/boundary на чотирьох view; жодного event/cleanup чи system/marker-*"
  :at "2026-09-16" :verified-by "python3 .claude/skills/fantasymayor-graph/scripts/fmgraph.py check, хвіст виводу"
  :consequence "мірило «без попереджень на змінених файлах» порівнюється з цією лінією"}
 {:fact "Unity 6000.5.1f1; scriptingBackend Standalone: 1 і Android: 1 (IL2CPP); il2cppCodeGeneration не заданий"
  :at "2026-09-16" :verified-by "прочитано ProjectSettings/ProjectVersion.txt і ProjectSettings.asset рядки 854-864"
  :consequence "відкритий generic резолвиться через MakeGenericType у рантаймі — гіпотеза H9 для плеєр-збірки"}]
```

## Divergence hypotheses (глибина :replace — кожна з дешевою перевіркою)

```clojure
[{:id :h1-busy-processor
  :hypothesis "NextTurnEvent, піднятий, коли TurnProcessorSystem не в Idle (кадр Completed), сьогодні губиться у вікні дозрілості; у журналі він лишиться непрочитаним і в наступному тіку запустить ще один хід"
  :rests-on "TurnProcessorSystem читає події лише в гілці Idle; фази синхронні, тож _processing у view не встигає стати true"
  :cheap-check "у Unity: подвійний клік «Завершити хід» протягом двох кадрів — сьогодні один хід; у журналі порахувати ходи; або читанням: чи читає процесор журнал поза Idle"}
 {:id :h2-many-events-one-reaction
  :hypothesis "споживачі, що перевіряють «є хоч одна дозріла» (TurnProcessorSystem, BuildDistrictCompletionSystem, MainMenuState, DistrictBuildUISystem, DistrictBuildListUISubSystem), сьогодні зливають N подій одного тіку в одну реакцію; while (TryRead) з дією на кожну подію дасть N реакцій (для процесора — N ходів)"
  :rests-on "код цих п'яти класів: return true на першій дозрілій; решта 17 споживачів з якорем уже реагують на кожну сутність"
  :cheap-check "rg -n \"return true\" у HasRipe*-методах п'яти класів; у Unity — два швидкі кліки End Turn / Build"}
 {:id :h3-same-tick-chain
  :hypothesis "ланцюг підтвердження з TurnsToBuild == 0 тепер проходить Planned → Built за один тік: ProgressViewSpawn (601) інстанціює прогрес-view, Despawn (604) знищує його в тому ж тіку, DistrictViewSpawn (605) ставить готовий район; сьогодні обидві події народжуються в кадрі N і споживаються разом у N+1 — порядок той самий, тож очікується еквівалентність"
  :rests-on "пріоритети 600-605 і reconcile у всіх споживачів"
  :cheap-check "у Unity: район із нульовим часом будівництва — жодного залишкового прогрес-view"}
 {:id :h4-subsystem-own-cursor
  :hypothesis "DistrictBuildListUISubSystem зі своїм курсором прочитає DistrictBuildUIRequestedEvent у першому populate після відкриття; якщо Open вийшов мовчки (гекс не виділено), подія лишиться непрочитаною підсистемою до наступного відкриття — там вона однаково ставить дефолт, тож поведінка еквівалентна; Repopulate з кліку рядка можливий лише після populate відкриття, який уже дочитав"
  :rests-on "DistrictBuildUISystem.Open: ранній вихід при _selectedHexSet.Count == 0; PopulateSections синхронний"
  :cheap-check "у Unity: клік «Будувати» без виділення (якщо можливо), потім з виділенням — вибір рядка не скидається"}
 {:id :h5-stale-across-sessions
  :hypothesis "за :inactive-consumer читачі Gameplay-систем після повернення в Gameplay з новим світом прочитають події попереднього світу; BuildDistrictActionCancelSystem кине «no District row» на старих Coords"
  :rests-on "readers — Transient у Singleton-системах, живуть весь контейнер; повторний вхід у Gameplay сьогодні недосяжний"
  :cheap-check "rg AppState.MainMenu|MapCreation серед запитів поза інсталерами — нуль сьогодні; ризик латентний до появи «назад у меню»"}
 {:id :h6-generator-button-outside-menu
  :hypothesis "Generate, натиснута в MapCreation/Gameplay, сьогодні прибирається наступного кадру; у журналі подія лежить до витіснення, а при поверненні в MainMenu одразу запросить MapCreation"
  :rests-on "ShowHexesUISystem.Hide ніхто не викликає; MainMenuState — єдиний читач"
  :cheap-check "у Unity (власник): чи видно й клікабельна кнопка Generate у Gameplay"}
 {:id :h7-settle-frames-reason
  :hypothesis "MapCreationState.SettleFrames = 3 обґрунтований прибиранням подій; без EventCleanupSystem причина зникає (саме значення — предмет паузованої задачі)"
  :rests-on "коментар MapCreationState рядки 17-20"
  :cheap-check "читання: чи є інші per-frame системи з прапорцем MapCreation — у реєстраціях немає"}
 {:id :h8-perframe-holders-vs-role-rule
  :hypothesis "TurnProcessorSystem і DistrictBuildUISystem сьогодні явно PerFrame і тримають архетип події; правило «роль reactive = тримає EventReader» (:consumer-shape) перекласифікує їх у reactive, хоча вони опитують стан щокадру"
  :rests-on "[SystemRole(PerFrame)] на обох; roles.py/MarkedTypeCheck"
  :cheap-check "fmgraph.py systems --role per_frame до і після зміни графа"}
 {:id :h9-il2cpp-open-generic
  :hypothesis "у IL2CPP-збірці MakeGenericType(EventReader<>, struct-подія) може не мати AOT-коду, якщо закритий тип ніде не згадано статично; у Editor (Mono) проблема не видна"
  :rests-on "OpenGenericInstanceProvider.CreateRegistration викликає MakeGenericType; Standalone backend = IL2CPP"
  :cheap-check "поле readonly EventReader<X> у системі — статична згадка закритого типу; остаточно — плеєр-збірка власника (агентові збірки заборонені)"}]
```

# Occasions

```clojure
[{:exit "читач типу, для якого жодного виробника немає — TryRead завжди нічого не дає"
  :occasion "факт: ForestHexAppearedEvent і ForestHexRemovedEvent мають споживачів і не мають виробника"}
 {:exit "подія записана, а її читач зараз не тікає (стан неактивний) — подія чекає або витісняється за лімітом"
  :occasion "факт: HexesUI не ховається, а читач TerrainGenerationGenerateEventComponent — лише MainMenuState; Gameplay-системи не тікають в інших станах"}
 {:exit "подія записана ззовні тіку систем — у C#-колбеку view або в EnterAsync стану"
  :occasion "факт: п'ять виробників-не-систем (чотири view і GameplayState) плюс колбеки DistrictBuildUISystem.OnConfirmed і HexInfoPanelDistrictSystem.OnCancelled"}
 {:exit "кілька подій одного типу за один тік"
  :occasion "факт: BuildDistrictActionSystem піднімає DistrictTableChangedEvent{Planned} і BuildDistrictCompleteEvent на одну підтверджену подію, BuildDistrictCompletionSystem у тому ж тіку — {Built}; UI-колбеки не обмежені одним кліком на кадр"}
 {:exit "споживач кидає виняток після видачі події"
  :occasion "факт: BuildDistrictActionSystem, BuildDistrictActionCancelSystem, BuildDistrictCompletionSystem, HexIconsVisibilitySystem та інші кидають на зламаних інваріантах; :commit-timing — подія не повертається"}
 {:exit "id витісненої сутності видається новій події"
  :occasion "факт: RecycleIds = true за замовчуванням"}
 {:exit "читач створюється раніше за будь-яку подію"
  :occasion "факт: усі системи й стани будуються при ін'єкції Boot до першого Switch"}]
```

# Verification

```clojure
{:meters [{:meter "mcp__roslyn__get_diagnostics, solutionPath FantasyMayor.sln, на змінених файлах" :target "чисто (CS0246 на символах, щойно створених у тому ж пакеті, — очікувана застарілість workspace)"}
          {:meter "/arch-check на зміненому обсязі" :target "без нових порушень; облік прочитаного не є станом систем (readonly EventReader — хендл, курсор у сховищі)"}
          {:meter "python3 .claude/skills/fantasymayor-graph/scripts/fmgraph.py build, потім check" :target "без попереджень на змінених файлах понад базову лінію з # Facts; виробник → споживач видно (emits і споживання через injects EventReader<T>)"}
          {:meter "python3 .claude/skills/fantasymayor-graph/scripts/fmgraph.py pattern PATTERN_VIEW_SYSTEM" :target "без нової девіації, якщо view змінювались"}
          {:meter "збірка Tools/MarkerShapeAnalyzer і оновлений Assets/Scripts/EcsExtensions/MarkerShapeAnalyzer.dll" :target "діагностики цитують id змінених правил; компіляція Unity без FantasyMayor.Markers помилок"}
          {:meter "fantasymayor-rules-conformance над зміненими правилами й carriers" :target "no :diverges, no :incomplete, no :uncarried"}
          {:meter "python3 Tools/doc_lint.py --quiet" :target "ghost-claim count не виріс, Clojure-блоки читаються"}
          {:meter "python3 Tools/gen_index.py" :target "INDEX skeleton перегенеровано, lint clean — якщо змінився frontmatter чи перший рядок рецептів"}]
 :owner-check "запуск у Unity власником: кожен нинішній ланцюжок подій працює як раніше — виділення гексу і панелі, вкладки контексту, кінець ходу і лічильник, будівництво району (0 і N ходів), скасування, іконки гексів, Generate з меню; окремо H1 (подвійний клік кінця ходу) і H6 (Generate поза меню); IL2CPP-збірка — H9"}
```

# Complete

```clojure
{:names-every-file-the-next-stage-may-read true
 :states-out-of-scope true
 :ends-with-verification true
 :links-to-follow-on-own-initiative 0
 :open-questions-for-owner
 [{:q :q1-tag-vs-type :ask "«ліміт на кожен event tag» (:store) — це ліміт на тип події (компонент)? Усі події сьогодні мають один тег EventTag."}
  {:q :q2-non-system-readers :ask ":consumer-shape говорить про систему; читачами також стають MainMenuState (IAppState), DistrictBuildListUISubSystem (підсистема) і Boot (MonoBehaviour, запит стану). Дозволяємо читача поза системами, і якою буде їхня роль у графі?"}
  {:q :q3-perframe-holders :ask "TurnProcessorSystem і DistrictBuildUISystem тримають подію, але опитують стан щокадру (H8): лишаються per_frame чи стають reactive за правилом «тримає EventReader»?"}
  {:q :q4-busy-and-burst :ask "H1/H2: подія, що прийшла, коли споживач «зайнятий», і кілька однакових подій за тік — обробляти кожну (буквальний журнал) чи відтворити нинішнє злиття в одну реакцію, дочитуючи решту? :no-conflation забороняє зливати в журналі, але не забороняє споживачу дочитати й відреагувати раз."}
  {:q :q5-view-producers :ask "чотири view піднімають події (view/boundary): лишаються виробниками журналу як є, чи перенесення підняття в системи входить у цей каскад?"}
  {:q :q6-lossy-producer :ask "BuildDistrictTurnTickSystem перепіднімає подію щоходу «за рівнем», бо подія могла загубитись у вікні кадру; у журналі вона не губиться (лише витіснення). Правило :event/lossy-producer знімаємо чи лишаємо як захист від витіснення?"}
  {:q :q7-capacity-default :ask "яке значення ліміту для типу без атрибута: атрибут обов'язковий (виняток при відсутності) чи є типовий?"}]}
```

# Owner answers

```clojure
;; 2026-09-16 — відповіді власника на питання вище; FLOW.md # Decisions тримає їх як :confirmed
[{:q :q1-tag-vs-type :answer "кожна подія — сутність у сховищі Events з власним головним тегом свого типу; ліміт, кільце й атрибут ліміту — на цьому тезі; спільний EventTag прибирається" :decision :capacity-per-event-tag}
 {:q :q2-non-system-readers :answer "дозволено: EventReader тримають і не-системи (стан, підсистема, Boot); роль у графі — «споживач події», без ролі системи" :decision :readers-outside-systems}
 {:q :q3-perframe-holders :answer "TurnProcessorSystem і DistrictBuildUISystem лишаються per_frame (поки що), хоч тримають EventReader" :decision :per-frame-readers}
 {:q :q4-busy-and-burst :answer "журнал не зливає; споживач сам вирішує — на кожну подію чи раз на пачку; п'ять нинішніх «є хоч одна» споживачів дочитують пачку й реагують раз" :decision :batch-reaction}
 {:q :q5-view-producers :answer "view лишаються виробниками як є; перенесення — не в цьому каскаді" :decision :views-raise-events-kept}
 {:q :q6-lossy-producer :answer "правило знімається; BuildDistrictTurnTickSystem піднімає подію один раз, коли лічильник уперше доходить до нуля; BuildDistrictCompletionSystem лишається звіркою зі станом" :decision :lossy-producer}
 {:q :q7-capacity-default :answer "без атрибута — типовий ліміт: розмір одного чанку архетипу Friflo, якщо його підтверджено, інакше 128" :decision :default-capacity}]
```
