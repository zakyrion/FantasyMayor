---
category: C
read: trigger
trigger: "ONLY when the user explicitly asks to open this file — never on session-start, never by topic/keyword"
tags: [gameplay, design]
related:
  - "[ARCHITECTURE](ARCHITECTURE.md)"
---

# FantasyMayor - Gameplay Foundation

## High Concept

`FantasyMayor` is a turn-based game about governing a city through a scarcity of `Action Points`, limited resources, population as a productive and political force, and an unstable balance of power between the mayor and the local elites.

The player takes the role of the `Mayor`, but does not control the whole city directly. For the city to live and grow, the mayor must decide what to do personally, what to fund from his own purse, what to hand over to the city, and what to delegate to `Important Citizens`, who are necessary for the system to function but at the same time become separate centers of power.

## Tone

The current baseline tone of the game is `cozy`.

The world should feel local, alive, and human, with an emphasis on coexistence, everyday survival, negotiation, and gradual development. Conflict is an important part of the game, but the baseline impression should stay warm and tense rather than openly cruel.

In the future the game may gain a `cozy / cruel` switch. At this stage this is only a direction for further development, not an already-designed mechanical system.

## Design Approach

At the start the game should not try to be an "honest simulation" of all city processes.

The initial implementation deliberately prioritizes simple, understandable, and manageable systems that create strong decisions for the player without unnecessary simulation complexity. If a given mechanic works as an abstraction, a simplification, or a controlled convention, that is acceptable as long as it supports the core gameplay.

Key consequences of this approach:

- do not simulate what does not give the player better decisions
- build systems so they can be extended later
- keep a limited number of `entity` types to retain control over design and implementation complexity

Working principle:

`Make it run, make it right, make it fast`

## Core Pillars

- `Hex-based territory`
- `District specialization`
- `Action economy`
- `Population economy`
- `Political economy`

These pillars form the main loop of the game:

- the world is built out of `Hex`
- each `Hex` develops through a `District`
- a `District` is useful only when someone spends `Action Points` and has access to the required workforce
- resources, population, and control over districts turn into political influence

## World Structure

The spatial chain of the game:

`Hex -> District -> Buildings/Quarters`

Rules:

- each `Hex` can have only one `District`
- each `District` has its own placement conditions
- placement conditions depend on the nature of the specific `Hex`
- a `District` has its own development and evolution tree
- a `District` can be rebuilt into a different specialization
- if a `District` is rebuilt into a different specialization from scratch, all existing `Buildings/Quarters` on that `Hex` are demolished
- `Buildings/Quarters` also have their own development, but their maximum potential is capped by the `District` level

## Actors and Power

The current model has three key actor types:

- `City`
- `Mayor`
- `Important Citizens`

### City

`City` has its own resource pool. It is primarily from this pool that ordinary citizens cover their basic needs, food in particular. `City` can also be the `Owner` of a district.

### Mayor

`Mayor` is the player's main actor.

The mayor has:

- a personal pool of `Action Points`
- a personal resource pool
- unique actions tied to leadership, construction, negotiation, pressure, and direct intervention

The mayor can act using his own resources or through the city's resources. This creates an important distinction between personal power and public administration.

### Important Citizens

`Important Citizens` are semi-autonomous actors, close in significance to the mayor.

The player does not control them directly. Interaction with them happens through negotiation, influence, and political pressure, not through direct orders.

Each `Important Citizen` has:

- a personal pool of `Action Points`
- a personal resource pool
- their own actions
- their own goals
- their own preferences

They can conflict with the mayor and with each other over influence, political decisions, resources, and control over people. A single `Important Citizen` can own several `District`s and build their own political base through them.

### Emergence of Important Citizens

The first `Important Citizens` must appear early and reliably through simple triggers. Their appearance must not depend on complex simulation or randomness, otherwise the game's main political mechanic would start working too late.

Examples of early triggers:

- a built `District` unlocks the appearance of a related `Important Citizen`
- repeated use of a certain key mayor action, for example crafting, unlocks the appearance of a related `Important Citizen`

After the early phase, new `Important Citizens` can appear through more complex systemic conditions:

- prolonged operation of a certain `District`
- accumulation of resources in a specific sector
- growth or stabilization of the corresponding population
- patronage, conflicts, or other political consequences

The economic role and the character of an `Important Citizen` are different dimensions:

- `Role` defines which part of the economy or space the actor is tied to
- `Personality` defines how the actor behaves in politics, negotiation, and conflict

So the city can have several `Important Citizens` with a similar role, for example several farmers or craftsmen, but with different characters and different political consequences for the game.

## Resources

The current model has two main resource categories.

### `Hex Resources`

`Hex Resources` are natural properties of a `Hex`.

- all `Hex Resources` are currently considered persistent
- they do not work as exhaustible deposits
- they define which types of `District` or actions are possible on that `Hex`

Examples: forest, stone, clay, ore.

### `Inventory Resources`

`Inventory Resources` are materials and goods stored in the pools of the city or individual actors.

- they are used in economy, construction, and survival
- the `consumable / persistent` distinction does not apply to them

Examples: logs, planks, food, and other produced goods.

The main difference is that a `Hex Resource` is not a ready stock you simply take off the map. It is a permanent source of potential that must be converted into value through actions, `District`, and development.

## Population

Population is the people who work. In the current model, `1 unit of population = 1 worker` (an `Anno`-style approach): population is not an abstract aggregated mass but concrete people, each of whom is workforce.

Population is needed for:

- performing `District` actions (each action costs a specific number of people)
- construction (raising a `District` also spends people)
- supporting the city's economy
- forming the political base of different actors

### Population Model (v1)

`v1` deliberately keeps population simple:

- a single type of people - conditional `peasants`; everyone can perform the available jobs (medieval setting - everyone works)
- no simulation of social classes; loyalty and patronage at the population level are `out of v1` (`Population and Patronage` remains a future direction)
- a single basic need - food

This is enough to close the economic loop (`people -> actions -> resources -> food -> people`) without unnecessary simulation complexity.

### Population Types (future direction)

Later, population becomes a `tree of types` rather than a linear tier progression:

- profession types: `peasants` -> `craftsmen` -> `masters`; higher types unlock more complex `District` actions
- races as separate branches of the tree: `elves`, `orcs`, etc.
- specifically a `tree` (not a line), because races and professions branch out rather than line up in a single row

Action gating by type (for example, pottery is shaped only by `craftsmen`) is enabled together with this tree. In `v1` it does not exist.

### Population Rules

- people have a basic need - food; if it is not covered, this leads to a drop in efficiency, mortality, or a combination of consequences
- people are a limited workforce pool for both construction and `District` actions
- if a district has free `Action Points` but no available people, the corresponding actions cannot be performed
- `Important Citizens` can grow their influence over population through patronage and resource control - this is a political layer `out of v1`

## Action Economy

The game is turn-based, and one of its main constraints is the number of `Action Points`.

Rules:

- only `Mayor` and `Important Citizens` have `Action Points`
- `Action Points` do not carry over between turns
- the number of `Action Points` can be increased through development
- an action can cost `Action Points`, people (workforce), and resources - in any combination

This means resources alone are not enough. For the system to work, you need an actor with free `Action Points`, and for `District` actions also available people (and sometimes input resources).

So `Action Points` mean not just effort but the practical ability to control the economy and impose your order of actions on the city.

## Interaction Model

`Mayor` actions are presented through panels and submenus.

An action is a button on a panel that opens a submenu, a sub-panel, or a modal window. Presentation through panels matches the mayor's role as a separate actor with a limited pool of `Action Points`, his own resources, and political will. Actions represent not the state of the city but the concrete moves the mayor can try to impose on the system in the current turn. There are no cards in the game.

Panels and submenus are especially well suited for:

- negotiations
- the mayor's personal investments
- direct intervention in a `District`
- political initiatives
- pressure, concessions, and exceptional decisions

The state of the city, district structure, ownership, resource flow, population, and other systemic data are read through the map and information panels. The same panel can simultaneously show state and carry action buttons alongside it.

Working principle of interaction:

`Map and panels for state and agency; actions open as submenus`

## Turn Structure

Each turn must be structured into clear phases. This is needed so the player can read the state of the system, make decisions at the right moment, and understand exactly when the consequences of their actions apply.

### 1. Start of Turn Preview

At the start of the turn the player sees the current state of the settlement:

- current resources in the pools
- active `District`s
- ongoing constructions
- population state
- basic needs for the next calculation
- available `Mayor` actions
- active requests, problems, or political signals

The goal of this phase is to give the player enough information for a conscious decision rather than forcing them to act blindly.

### 2. Mayor Phase

In this phase the player chooses `Mayor` actions through panels and submenus and defines their actions for the turn.

Typical decisions in this phase:

- activate manual economic actions
- start construction or `District` development
- invest resources from the personal or city pool
- conduct negotiations
- intervene in the work of districts or in the political situation

If a given action requires a long-term commitment, it may reserve part of the `Action Points` for several turns or otherwise limit the available action pool in following turns.

### 3. Citizen Phase

After the mayor, the `Important Citizens` perform their actions.

In this phase they:

- spend their own `Action Points`
- activate the `District`s they control
- advance their interests according to their role, resources, goals, and character

At this level it is important that they act not as an extension of the player's hand but as separate actors with their own will.

### 4. Resolution Phase

After actions are assigned, they are resolved.

In this phase:

- the effects of the played actions are applied
- activated `District`s produce `Yield`
- `Yield` is split across `City Share`, `Owner Share`, `Operator Share`
- construction and development advance
- the state of short-term economic actions is updated

This is the main phase where the turn's decisions turn into a result.

### 5. Upkeep Phase

After resolution, the systems go through upkeep.

In this phase:

- resources are added to the pools according to the turn's results
- costs and basic consumption are deducted from the pools
- population count is updated
- population loyalty and patronage are `out of v1` (a future layer)
- deficits, famine, shortage consequences, and other basic systemic effects are checked

This is where the cost of the decisions made in the previous phase becomes visible.

### 6. End of Turn Consequences

At the end of the turn the system may generate new consequences:

- requests from the population
- new political problems
- patronage events
- conditions for the appearance of new `Important Citizens`
- other signals that change the priorities of the next turn

After that the game moves to a new `Start of Turn Preview`.

## District Economy

A `District` in the current model is not just zoning. It is at the same time:

- a specialization of a `Hex`
- a unit of production
- an object of ownership
- a consumer of labor
- a source of political power

`Hex Resources` and `District` are interdependent systems. A natural property of a `Hex` can unlock or limit the available `District` types, and the chosen `District` defines which buildings, actions, and long-term development paths become available.

For a `District` to be useful, it usually needs:

- the right to exist on a specific `Hex`
- an `Operator`
- a spend of `Action Points`
- access to the required labor

### District Construction

A `District` is built in advance and not instantly:

- to start construction, the actor immediately invests the full cost: `Action Points` (always from the `Mayor` pool), resources, and people (workers)
- construction takes several turns; the finished `District` appears after completion
- the resources for construction are paid by either the `Mayor` or the `City` - and this choice sets the initial `Owner`: whoever pays, owns (`Payer = Owner`)
- a built `District` has a periodic `upkeep` each turn

### District Actions

An activated `District` works through `fixed-step` actions rather than as a passive generator:

- one order (`step`) costs a fixed number of people + `Action Points` (`Operator`), sometimes also input resources, and gives a fixed output
- the district's `max worker capacity` limits how many such orders can be issued per tile per turn (e.g. step 100 people, capacity 200 -> up to 2 orders)
- some actions consume one resource and produce another (e.g. `clay -> pottery`)
- each action's output is split across `City / Owner / Operator` (see `Yield Split`)
- output can be fractional (`float`); the UI displays it rounded to one decimal place

## Ownership and Operation

Each `District` has two separate roles:

- `Owner`
- `Operator`

### `Owner`

`Owner` is the entity that owns the `District` as an asset.

`Owner` can be:

- `City`
- `Mayor`
- `Important Citizen`

The initial `Owner` is determined at the construction stage by whoever pays for it with resources (`Mayor` or `City`): `Payer = Owner` (see `District Construction`).

### `Operator`

`Operator` is the entity that activates the `District` during a turn by spending `Action Points`.

`Owner` and `Operator` are different roles, but the same entity can combine both.

This separation matters because ownership, activation, and receiving the benefit do not necessarily coincide.

## Yield Split

A `District` generates resources only when it is activated - that is, when the `Operator` issues a `District` action (a fixed step: people + `Action Points` [+ input resources]; see `District Actions`).

High-level model:

`District Action -> Yield Split`

The output of each action is split into three channels:

- `City Share`
- `Owner Share`
- `Operator Share`

This means:

- `City` receives a part of the result for shared survival and public life
- `Owner` receives a part of the result for control over the asset
- `Operator` receives a part of the result for the spent `Action Points` and the performed action

The exact formulas are deliberately not defined at this stage.

## Delegation and Political Conflict

The mayor cannot personally service every important `District`, because he has a limited pool of `Action Points`.

From this comes the central tension of the game:

- direct control preserves power but spends the mayor's limited actions
- delegation keeps the economy running but strengthens other actors

`Important Citizens` are needed not as bonus helpers but as a way to bypass the mayor's `Action Points` deficit. This is especially important when critical `District`s must stay active and the population depends on stable production.

At the same time, every delegated `District`, every private resource flow, and every controlled population loop strengthens a local political base. Actors who control land, production, food, and people can pressure the mayor, sabotage policy, or push their own agenda.

The game's main conflict can be reduced to the formula:

`Control vs Delegation vs Survival`

## Population and Patronage

Ordinary citizens initially consume `City` resources.

If the city cannot cover basic needs, people turn to their patrons. In practice this means `Important Citizens` can become alternative sources of survival, especially if they control productive `District`s and have their own resource stocks.

This makes private wealth and control over resources politically significant:

- resources are not only economic power
- resources are a lever of influence over people
- support in times of crisis can create dependency, loyalty, and local political influence

So patronage is a social and political layer built on top of the economic one.

## Open Questions

- Who makes long-term strategic decisions for a `City-owned` `District`?
- What exactly should the formulas or rules of `Yield Split` be?
- Should `Important Citizens` have formal obligations to the city when they support "their" people with resources?
- When and how to introduce the `population type tree` (craftsmen, masters, races) after `v1`?
- Which basic needs besides food should enter `v1`?
- How detailed and aggressive should the AI behavior of `Important Citizens` be?
- What exactly should the future `cozy / cruel` switch change: only numbers and mood, or also the available behaviors and consequences?

## Core Identity

`FantasyMayor` is a turn-based hex-based city-political builder in which the player governs a city through a mayor with limited actions, develops territory through `District`, resources, and population, and is also forced to rely on autonomous `Important Citizens`, who are at the same time necessary for the city's survival and constitute a long-term threat to centralized control.
