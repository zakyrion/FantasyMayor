# FantasyMayor Flow Close

Close the active Category A FLOW by the canon `close-ritual` block (`CLAUDE.md` → Engineering Task
Template). This command is the explicit completion act — closing is never triggered automatically.

## Workflow

1. **Locate the FLOW.** `$ARGUMENTS` may name it; otherwise take the single `Flows/FLOW_*.md` with
   `status: partial`. Several candidates → list them and ask which one to close; zero → report and
   stop.
2. **check-all-acceptance!** Take every `:accept` meter from the FLOW's contract and READ IT FOR REAL
   (run the tool, grep the doc, request the Unity-side check) — never fill `:actual` from memory.
   Any meter off target → the FLOW stays `partial`: record the blocker per `blocked-outcome`,
   report, stop.
3. **record-acceptance-audit!** Write `:meter / :target / :actual / :status` for every meter into
   the FLOW.
4. **harvest-plan!** Propose the Rule 2d split (`DOC_STANDARD.md`): hazard → code comment at
   distance zero; invariant → the FLOW's Contract; toolchain fact → policy proposal; the rest →
   drop, leaving the 3-line tombstone. The user vetoes the split before you write it.
5. **choose-fate!** Rule 2e: a durable contract still useful before a future change →
   `read: trigger` + `status: implemented`; otherwise move to `Flows/Archive/` (via `vault_move`
   so links survive) and strip `code_refs`. A linked `Flows/RESEARCH_*.md` document shares the
   FLOW's fate — move it to Archive together, never leave it behind (canon `done-contract`
   `:research-document`).
6. **regen-index!** Run `python3 Tools/gen_index.py`, then `python3 Tools/doc_lint.py --quiet` —
   report both one-liners.

## Guardrails

- Never archive with a failing meter; never mark complete without acceptance evidence in the FLOW.
- `:commit :only-when-requested` — closing a FLOW does not imply a git commit.
- An interrupt or a user question during the ritual revokes it: answer only, wait for a fresh ask.
