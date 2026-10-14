---
category: A
read: archive
status: implemented
tags:
  - boot
  - algorithm
---

The app walks its modes; states only run systems, and systems ask for the next mode.

# s1

```clojure
(def AppFlow
  {:makes     "застосунок проходить свої режими від запуску до гри, і кожен перехід просить система, а не стан"
   :criterion "стан лише виконує системи свого режиму; знання гри й рішення «куди далі» — у системах шарів, яким вони належать"

   :data {:mode      {:type AppState :holds "поточний режим — один із восьми прапорців" :from "запускач, після кожного перемикання"}
          :states    {:type IAppState :holds "по одному стану на режим, без знань гри" :from "DI — усі зареєстровані стани"}
          :systems   {:type IAppStateSystem :holds "усі системи першого порядку з їхніми прапорцями" :from "DI — реєстрація в інсталерах шарів"}
          :kept      {:type AppStateSystems :holds "системи одного режиму: разові кроки, етапи пайплайна, тікові, пізні тікові" :from "відбір за прапорцем режиму"}
          :entry     {:type UniTask :holds "вхід поточного стану, що ще триває або завершився" :from "стан, при перемиканні"}
          :request   {:type AppStateRequestedEvent :holds "режим, якого хтось просить" :from "системи переходів"}
          :generate  {:type TerrainGenerationGenerateEventComponent :holds "натискання кнопки меню генератора" :from "вʼюха меню генератора — будь-яка з трьох кнопок"}
          :settle    {:type int :holds "тіки MapCreation після завершення входу" :from "система переходу MapCreation"}
          :icons     {:type HexIconsVisibilityComponent :holds "іконки гексів видимі на старті гри" :from "разовий крок Gameplay у шарі іконок"}
          :turn      {:type TurnCountComponent :holds "хід 1 на старті гри" :from "разовий крок Gameplay у шарі ходів"}}

   :flow (-> (:step-1 "запускач перемикає застосунок у Initialization")
             (:step-2 "стан нового режиму відбирає свої системи"
                      {:reads #{:systems :mode} :writes #{:kept}})
             (:step-3 "стан виконує вхід: разові кроки, потім етапи пайплайна за пріоритетом; у ConfigLoading — усі завантажувачі одночасно"
                      {:reads #{:kept} :writes #{:entry :icons :turn} :state "режим побудований"})
             (:step-4 "поки вхід не завершився, стан не тікає"
                      {:reads #{:entry}})
             (:step-5 "стан тікає свої системи в порядку пріоритету, і система переходу режиму вирішує, чи просити далі"
                      {:reads #{:kept :generate :settle} :writes #{:request :settle}})
             (:step-6 (cond "Initialization, перший тік"            "просить ConfigLoading"
                            "ConfigLoading, перший тік після входу"   "просить InstanceObjects"
                            "InstanceObjects, перший тік після входу" "просить MainMenu"
                            "MainMenu, прийшла подія генерації"       "просить MapCreation"
                            "MapCreation, третій тік після входу"     "просить Gameplay"
                            :else                                     "нічого не просить — Gameplay, MapLoading, GameOver"))
             (:step-7 "запускач після тіку читає останній запит і перемикає режим — і назад до кроку 2"
                      {:reads #{:request :mode} :writes #{:mode}}))

   :exits #{"запит того самого режиму — ігнорується: система переходу може просити повторно, поки запускач не перемкнув"
            "режим без стану — throw: стан не зареєстрований у DI"
            "вхід скасовано — OperationCanceledException: новий запит або зупинка запускача під час входу"
            "система, позначена режимом, не належить жодному виду систем — throw: помилка реєстрації"}

   :numbers {:settle-ticks "3 — тіки MapCreation після входу, щоб реактивні системи встигли відповісти на все, що підняв пайплайн"}})
```

# Contra

```clojure
[{:id :c-1
  :kills "переходи «після входу» і відлік settle-тіків"
  :case "пайплайн MapCreation чекає на завантаження asset-ів довше трьох кадрів"
  :fails "GameModeMachine тікає стан з першого кадру, навіть поки вхід ще триває: система переходу MapCreation нарахує три тіки і попросить Gameplay посеред пайплайна, а Boot скасує вхід; так само ConfigLoading попросить InstanceObjects, поки конфіги ще вантажаться"
  :fix [{:id :fix-a :confidence 75 :is "машина не тікає стан, доки не завершився його вхід — те, що вже обіцяє коментар IAppState" :cost "одна перевірка в машині станів; стани й системи не змінюються"}
        {:id :fix-b :confidence 25 :is "кожна система переходу сама перевіряє, чи завершився вхід, через singleton «вхід завершено»" :cost "новий компонент, запис у кожному стані, перевірка в кожній системі переходу"}]
  :outcome "fix-a — власник, «A (75) Машина чекає кінця входу»"}
 {:id :c-2
  :kills "чистоту межі view"
  :case "HexesUI сам надсилає ECS-подію генерації"
  :fails "це відхилення view-boundary (fmgraph); виправити його чисто можна лише через систему, яка тримає HexesUI, а ShowHexesUISystem змінювати заборонено"
  :fix [{:id :fix-a :confidence 70 :is "прийняти відхилення в цій задачі й записати його як відоме: HexesUI лише шле ту саму подію з трьох кнопок" :cost "відхилення лишається у звіті fmgraph"}
        {:id :fix-b :confidence 30 :is "окрема наступна задача: HexesUI — C#-події, підписка в системі, що його тримає" :cost "відкрити ShowHexesUISystem пізніше"}]
  :outcome "fix-b — власник, «B (30) Окрема задача»"}]
```
