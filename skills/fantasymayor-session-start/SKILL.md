---
name: fantasymayor-session-start
description: Load and resume working context for the FantasyMayor repository by reading `CLAUDE.md`, executing its startup instructions, reading the remaining root Markdown files, identifying where work appears to have stopped, surfacing any conflicting status notes between documents, and ending by asking the user what to do next. Use when starting a new session in this repository, when the user asks to resume context, continue prior work, or find the current roadmap before making changes.
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

Read `CLAUDE.md` before anything else.

Execute the instructions inside `CLAUDE.md`, not just summarize them. At minimum this means reading:
- `ARCHITECTURE.md`

If `CLAUDE.md` points to additional mandatory startup material, read that too.

Respect the repository constraints you find there, especially:
- startup-order requirements
- Unity build restrictions
- module-folder rules
- "read on demand" references such as Addressables patterns or terrain/isoline pre-read files

Do not preload optional deep-dive files unless the current startup flow or the user's request requires them.

### 2. Read the remaining root Markdown files

After completing the startup documents, read the other Markdown files in the repository root.

Ignore nested package/plugin READMEs during this startup pass unless a root file explicitly sends you there or the user asks for work in that area.

Treat root Markdown as potentially inconsistent. Compare documents rather than assuming the first one is canonical.

### 3. Reconstruct the current state

Extract only the facts that help resume work:
- what baseline or milestone is described as complete
- what files or systems are called out as current source of truth
- what the next roadmap item appears to be
- whether multiple documents disagree about "what is next"

When documents conflict, do not silently merge them. State the conflict explicitly with file names and the differing claims.

Use simple priority rules:
- treat process and safety rules in `CLAUDE.md` and `SESSION_START.md` as mandatory
- treat dated status notes as useful evidence for recency
- when two roadmap documents disagree, surface both and ask the user which one is current

### 4. Hand control back to the user

End the startup pass by asking the user what they want to do now.

Do this even if the next step seems obvious from the docs. The point of this skill is to re-establish context first, then let the user choose the task.

## Output shape

Respond in the user's language. Keep the summary short and operational.

Use this structure:

1. `Read`
   List the startup and root files you loaded.

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