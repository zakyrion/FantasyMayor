---
name: fantasymayor-session-start
description: Load and resume working context for the FantasyMayor repository by reading `CLAUDE.md` through the Obsidian MCP, following it to `INDEX.md` (the generated doc map), reading only the docs `INDEX.md` marks `read: always`, identifying where work appears to have stopped, surfacing any conflicting status notes between documents, and ending by asking the user what to do next. Use when starting a new session in this repository, when the user asks to resume context, continue prior work, or find the current roadmap before making changes.
---

# FantasyMayor Session Start

## Overview

Read the repository startup documents in the correct order, reconstruct the current project state from root-level notes, then hand control back to the user with a concise status summary and a direct question about the next task.

Follow the repository process exactly. Do not skip startup documents, do not invent a different onboarding flow, and do not start implementing code changes before asking the user what to do next unless they explicitly asked for implementation in the same request.

## Workflow

### 0. Ask the task, what we will to do

Bofore start any other actions you need to ask the developer what task we will do today
Only after provided asnwer you must to continue this skill.

### 1. Load startup instructions first

**Read docs through the Obsidian MCP** (`mcp__obsidian__vault_read`) — this repo is an Obsidian vault and that is the primary channel. If the `obsidian` server is not connected (Obsidian closed / HTTP server off), fall back to the plain `Read` tool.

Read `CLAUDE.md` before anything else, then read `INDEX.md` (CLAUDE.md "Start Working" points there). `INDEX.md` is the generated doc map and the single key to every doc and canvas.

Respect the repository constraints in `CLAUDE.md`, especially:
- startup-order requirements
- Unity build restrictions
- module-folder rules
- "read on demand" references such as Addressables patterns or terrain/isoline pre-read files

### 2. Read only the start-reading set

Read every doc `INDEX.md` marks `read: always` (currently `ARCHITECTURE.md`, `DOC_STANDARD.md`, `GAMEPLAY_FOUNDATION.md`) — via the same Obsidian channel as step 1.

Do **not** read all root Markdown, and do **not** preload `trigger` or `reference` docs — `INDEX.md` already summarizes each one. Glance at the index and read a `trigger`/`reference` doc only when the user's task needs it. Ignore nested package/plugin READMEs.

Do **not** enumerate or preload canvases at startup. `INDEX.md` lists them in its `Canvas map`; read a `.canvas` (via Obsidian `vault_read`) only when the user's task needs it.

Treat the always-read docs as potentially inconsistent. Compare them rather than assuming the first one is canonical. For "what is the current state", the `status` column in `INDEX.md` is the fast machine-readable view; the prose `## Current State` in each module doc is the authoritative detail.

### 3. Reconstruct the current state

Extract only the facts that help resume work:
- what baseline or milestone is described as complete
- what files or systems are called out as current source of truth
- what the next roadmap item appears to be
- whether multiple documents disagree about "what is next"

When documents conflict, do not silently merge them. State the conflict explicitly with file names and the differing claims.

Use simple priority rules:
- treat process and safety rules in `CLAUDE.md` and `DOC_STANDARD.md` as mandatory
- treat dated status notes and the `status` column in `INDEX.md` as evidence of current state
- when two roadmap documents disagree, surface both and ask the user which one is current

### 4. Hand control back to the user

End the startup pass by asking the user what they want to do now.

Do this even if the next step seems obvious from the docs. The point of this skill is to re-establish context first, then let the user choose the task.

## Output shape

Respond in the user's language. Keep the summary short and operational.

Use this structure:

1. `Read`
   List the files you loaded (`CLAUDE.md`, `INDEX.md`, and the `read: always` docs).

2. `Current state`
   State where the project appears to have stopped and what is already considered done.

3. `Possible next work`
   State the next roadmap item or the competing candidates if the docs disagree.

4. `Question`
   Ask the user directly what to do next.

## Guardrails

Do not start coding, editing, or running implementation commands during this startup flow unless the user explicitly asks for that in the same request.

Do not claim certainty about project status when the documents conflict.

Do not expand into module-level Markdown files during startup unless a root file explicitly requires it for the present task.