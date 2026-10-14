---
name: tool-where-asset-used
description: "Where a prefab, UXML, config or other asset is used and why it ships — code → address const via roslyn, address → asset via the live Unity Editor (official unity MCP / unity command), asset → who references it via the unity-asset-graph (uag refs / why). Never read .unity scenes or .prefab YAML by hand."
user-invocable: false
---

# Where this asset is used

```clojure
{:tool-skill "tool-where-asset-used"
 :question "where is this asset used, who loads it from code, why does it ship"
 :order (-> (:code-to-key "the address is a const in ConfigAddresses — mcp__roslyn__find_references on the const gives the installer that loads it")
            (:key-to-asset "the live Editor answers from its own asset database: mcp__unity__editor_status first (status ready), then mcp__unity__find_assets {:name <address>} or mcp__unity__search; without MCP, unity command --project-path <repo> --caller plugin --skill tool-where-asset-used")
            (:asset-to-users "python3 ~/.claude/skills/unity-asset-graph/scripts/uag.py refs <asset> (direct) / why <asset> (the chain from a ship root)"))
 :because "asset links are GUIDs in serialized YAML and addressable entries — grep sees neither; the const is the one code-side anchor, and the Editor and the asset graph resolve the rest"
 :invoke "mcp__roslyn__find_references on ConfigAddresses.<CONST>; mcp__unity__find_assets {:name \"<address>\"}; uag.py why <asset path>"
 :example {:call "find_references ConfigAddresses.CITY_CONFIG; find_assets {:name \"CityConfig\"}; uag.py why Assets/Presentation/HexIcons/Prefabs/HexIconsView.prefab"
           :returned "ActorsInstaller.cs:17 WithParameter(\"address\", ConfigAddresses.CITY_CONFIG); Assets/Addressables/Configs/Economy/CityConfig.asset; root addressables -> HexIconsConfig.asset -> HexIconsView.prefab"}
 :blind-spots #{"the live Editor leg answers only while an Editor with com.unity.pipeline is open — editor_status shows it"
                "uag's build scans scene files for GUIDs — its answers are derived; the agent still never reads a .unity file itself (AGENTS.md § 3)"
                "an address built at run time (a computed Resources.Load path) — uag warns about dynamic loads"}
 :fallback "uag alone when no Editor is connected, and the finding says the live leg was not available"}
```
