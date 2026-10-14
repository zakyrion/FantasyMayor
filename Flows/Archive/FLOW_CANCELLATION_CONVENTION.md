---
category: A
read: archive
status: implemented
tags: [flow, cancellation, conventions, addressables]
related:
  - "[ECS_CONVENTIONS](../../ECS_CONVENTIONS.md)"
  - "[ADDRESSABLE_PATTERNS](../../Patterns/ADDRESSABLE_PATTERNS.md)"
---

# FLOW_CANCELLATION_CONVENTION

Скасування — виняток (`ThrowIfCancellationRequested`), а не тихий `return` і не `Status.Cancelled`.

# 1 · Request

## User request — 2026-09-13 (verbatim)

> Тоді замінимо в документі на загально прийняту конвенцію

Контекст — попереднє питання того ж дня: «що зараз більш розповсюджено як практика
`cancellationToken.ThrowIfCancellationRequested();` чи `if (cancellationToken.IsCancellationRequested) return;`».

## Agent restatement — confirmed by the user

```clojure
{:task :cancellation-convention
 :goal "ThrowIfCancellationRequested як конвенція; скасування — виняток, а не статус"
 :where #{ECS_CONVENTIONS.md PATTERN_PIPELINE_STAGE PATTERN_CONFIG_LOADER ADDRESSABLE_PATTERNS
          feedback_fail_loud_no_silent_skip MEMORY.md
          Status.cs Result.cs Addressable.cs IAddressable.cs ConfigLoaderSystemN.cs}
 :off-limits "решта *.cs (52 місця IsCancellationRequested); Flows/Archive"
 :decided {:default          "cancellationToken.ThrowIfCancellationRequested()"
           :fail-loud        "для помилок без змін"
           :quiet-exit-owner :none
           :addressable      "кидає OperationCanceledException; Status.Cancelled і Result.Cancelled() видалені"
           :cleanup          "try/finally"}
 :skip "міграція решти коду — власник"
 :config-loader-n :throw
 :result "доки, пам'ять і Addressable API без жодного Cancelled-статусу та :quiet-return на скасування"}
```

## Amendments (append-only)

```clojure
[{:received-at "2026-09-13"
  :raw-request "1 - сам перепишу\n2 - ні\n3 - б\n4 - try/finally"
  :normalized "{:skip \"міграція коду\" :quiet-exit-owner :none :addressable :api-throws :cleanup \"try/finally\"}"
  :confirmed true}
 {:received-at "2026-09-13"
  :raw-request "Можеш не тільки прибрати їх з документації але й API та реалізації"
  :normalized "{:where (+ Status.cs Result.cs Addressable.cs IAddressable.cs ConfigLoaderSystemN.cs)}"
  :confirmed true}
 {:received-at "2026-09-13"
  :raw-request "б"
  :normalized "{:config-loader-n :throw}  ;; ConfigLoaderSystemN.cs:57 переписати на throw, не мінімальна правка"
  :confirmed true}
 {:received-at "2026-09-13"
  :raw-request "ресурс має бути звільненим. По суті Box це зараз контейнер який лише говорить чи є щось всередині, чи ні і якщо є - то цей ресурс можна звільнити."
  :normalized "{:addressable-cleanup-shape \"try/finally; звільнення вирішує Box (Exist), а не Status\"}"
  :confirmed true}]
```

# 2 · Contract

## Findings

```clojure
[{:finding :cancelled-consumers
  :at "2026-09-13"
  :fact "Status.Cancelled / Result<T>.Cancelled() живуть лише в Status.cs, Result.cs, Addressable.cs:39,75 і ConfigLoaderSystemN.cs:57 (клас ConfigLoaderSystem); Result<T> не має інших споживачів"
  :verified-by "grep по Assets/**/*.cs"
  :consequence "видалення члена enum ламає компіляцію лише в ConfigLoaderSystemN.cs:57"}
 {:finding :owner-catches-oce
  :at "2026-09-13"
  :fact "GameModeMachine.EnterAsync ловить OperationCanceledException; catch (Exception) в UniTaskSequentialSystem стоїть лише в Dispose, не на шляху виконання"
  :verified-by "grep catch по Modules/Boot і Scripts/EcsExtensions + читання обох місць"
  :consequence "throw зі скасування доходить до власника токена без логування як помилки"}
 {:finding :user-wip-already-throws
  :at "2026-09-13"
  :fact "Addressable.LoadAsync<T> (рядки 101-106) і ConfigLoaderSystemN.LoadConfigAsync вже кидають на скасування — ручні правки власника"
  :verified-by "читання файлів на диску"
  :consequence "дві інші гілки Addressable переписуються тим самим явним візерунком, що вже є у файлі"}
 {:finding :box-dispose-is-safe
  :at "2026-09-13"
  :fact "Box<T>.Dispose() на default Box — no-op (_handle?.Dispose()), на Empty — no-op (handle створений уже disposed); Exist = є що звільняти"
  :verified-by "читання Assets/Scripts/Core/Box.cs"
  :consequence "finally може звільняти безумовно; гілки по Status для звільнення зайві — ADDRESSABLE_PATTERNS інваріанти 1-2 і рядок антипатерну «Dispose on Failed» переписані"}
 {:finding :private-load-no-release
  :at "2026-09-13"
  :fact "приватний LoadAsync<T>(string) повертав Box без release-дії, публічний перезагортав Value з Release"
  :verified-by "читання Addressable.cs"
  :consequence "Release перенесено в приватний завантажувач (+ where T : class), публічний повертає Result як є — один handle замість двох"}]
```

## Decisions

```clojure
(def decisions
  [{:decision :default :status :confirmed :at "2026-09-13" :value "ThrowIfCancellationRequested" :verified-by "відповідь власника" :reason "TAP/BCL/UniTask конвенція: тихий return робить скасований таск RanToCompletion"}
   {:decision :quiet-exit-owner :status :confirmed :at "2026-09-13" :value :none :verified-by "відповідь 2 — ні" :reason "у документі не описується дозволений тихий вихід"}
   {:decision :addressable :status :confirmed :at "2026-09-13" :value "API кидає; Cancelled видалено з доків, API й реалізації" :verified-by "відповідь 3 — б + поправка" :reason "скасування — виняток, не статус"}
   {:decision :cleanup :status :confirmed :at "2026-09-13" :value "try/finally" :verified-by "відповідь 4" :reason "звільнення не залежить від гілки виходу"}
   {:decision :code-migration :status :confirmed :at "2026-09-13" :value "власник, окрім файлів у :where" :verified-by "відповідь 1 — сам перепишу" :reason "52 місця лишаються власнику"}
   {:decision :config-loader-n :status :confirmed :at "2026-09-13" :value :throw :verified-by "відповідь «б»" :reason "рядок переписується на throw одразу"}
   {:decision :addressable-cleanup-shape :status :confirmed :at "2026-09-13"
    :value "try/finally у всіх трьох публічних методах; локальний Box стає Empty при передачі викликачу, finally звільняє решту"
    :verified-by "відповідь власника «ресурс має бути звільненим…» + Box.cs"
    :reason "read-back знайшов, що гілка-перед-throw суперечила новому рецепту; власник підтвердив, що звільнення обов'язкове і вирішується Box, тож finally звільняє безумовно"}])
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
 :completed #{:flow-created :code :docs :memory :verify :addressable-try-finally :owner-unity-check}
 :current :closed
 :remaining #{}
 :resume-context "CLOSED 2026-09-13: скасування — ThrowIfCancellationRequested, ніколи не тихий return; Status.Cancelled і Result<T>.Cancelled() видалені, IAddressable кидає OperationCanceledException і звільняє завантажене в try/finally. Власник підтвердив Unity-компіляцію і роботу гри."}
```

```clojure
;; harvested 2026-09-13 (Rule 2d): правило → ECS_CONVENTIONS (Error Handling), ADDRESSABLE_PATTERNS (API, інваріанти 1-2 і 5, антипатерни), PATTERN_PIPELINE_STAGE / PATTERN_CONFIG_LOADER :cancellation, пам'ять feedback_fail_loud_no_silent_skip; контракт у коді → заголовки IAddressable і Addressable.
;; Покроковий план (код, доки, пам'ять, перевірка, try/finally-форма Addressable) скинутий — виконаний повністю; record = commit log.
```

## Acceptance

```clojure
[{:meter "grep Status.Cancelled|Result<.*>.Cancelled|Cancelled() по Assets/**/*.cs і *.md (крім Archive)" :target "0" :actual "0 (2026-09-13)" :status :passed}
 {:meter "grep :quiet-return|legitimate quiet по *.md і пам'яті" :target "0" :actual "0 (2026-09-13)" :status :passed}
 {:meter "mcp__roslyn__get_diagnostics по редагованих файлах" :target "0 нових помилок" :actual "0 errors: Addressable.cs, ConfigLoaderSystemN.cs, Result.cs (2026-09-13)" :status :passed}
 {:meter "python3 Tools/doc_lint.py --quiet" :target "0 нових ghosts, 0 syntax" :actual "8 ghosts — усі HexIdComponent у рядках, яких задача не торкалась (ECS_CONVENTIONS ×5, PATTERN_COMPONENT, PATTERN_TRANSACTION_ENTITY ×2); 0 syntax (2026-09-13)" :status :passed}
 {:meter "Unity-компіляція (власник)" :target "без помилок" :actual "власник: «все працює» (2026-09-13)" :status :passed}]
```
