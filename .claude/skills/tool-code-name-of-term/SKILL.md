---
name: tool-code-name-of-term
description: "What a game-design term the owner used (Ukrainian or English — «хід», «район», «податок») is called in code — read GLOSSARY.md first, then confirm every anchor with roslyn search_symbols; only then search the code. Never grep a prose word, and never invent a class name when the term is not found — ask."
user-invocable: false
---

# The code name of a term

```clojure
{:tool-skill "tool-code-name-of-term"
 :question "which classes stand behind this word the owner used"
 :order (-> (:step-1 "find the term in GLOSSARY.md — its aliases list Ukrainian and English words; the value names :domain and 2-4 :anchors")
            (:step-2 "confirm each anchor with mcp__roslyn__search_symbols — the glossary is hand-kept and may lag the code")
            (:step-3 (cond (term-in-glossary?) "go on with the confirmed anchors into tool-who-calls / tool-who-writes-component / tool-di-wiring"
                           (english-variant-finds-symbols?) "search_symbols by English variants of the word; state the match as a hypothesis"
                           :else "a question for the owner — never a guessed class name")))
 :because "the owner speaks in game words; the code speaks in type names — a grep of a prose word misses, and a guessed name sends the whole survey the wrong way"
 :invoke "grep -n -i '<term>' GLOSSARY.md, then mcp__roslyn__search_symbols {:solutionPath \"<repo>/FantasyMayor.sln\" :query \"<Anchor>\"}"
 :example {:call "GLOSSARY.md «turn | хід»"
           :returned "{:domain Modules/Turn :anchors [NextTurnEvent TurnCompletedEvent TurnPhaseStep]}"}
 :blind-spots #{"a model-only concept carries :anchors ? — it has no code yet; say so, do not search"
                "the glossary grows by concepts, not classes — a helper class is never in it"}
 :fallback "search_symbols by the English word; the finding says the glossary had no entry"}
```
