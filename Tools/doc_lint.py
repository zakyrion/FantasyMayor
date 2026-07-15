#!/usr/bin/env python3
"""doc-lint — deterministic ghost detector for the repo's .md docs.

Verifies every symbol name a doc claims against the actual C# declarations:
  1. frontmatter `code_refs:` entries — checked STRICTLY (every name must be declared);
  2. body mentions — any PascalCase token carrying a project role suffix
     (System/Component/Tag/Event/Config/Installer/View) that is not declared in Assets/**.cs.

A name present in a doc but absent from the code is a GHOST — either doc rot or a rename
the doc missed. Exit code 1 if any ghost is found.

Usage:
  python3 Tools/doc_lint.py                # whole repo
  python3 Tools/doc_lint.py --scope Flows  # only .md whose path contains the substring
  python3 Tools/doc_lint.py --quiet        # one summary line only
"""

import argparse
import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent

# Directories never scanned (generated, external, hidden).
SKIP_DIRS = {".git", "Library", "Temp", "Logs", "obj", "Packages", "node_modules",
             ".ecs-graph", ".di-graph", "design-mockups"}

# Role suffixes per the project naming policy — a Pascal token with one of these is a code claim.
ROLE_SUFFIXES = ("SubSystem", "System", "Component", "Tag", "Event", "Config", "Installer", "View")

# Teaching-doc placeholder prefixes — deliberately fake names, never ghosts.
PLACEHOLDER_PREFIXES = ("My", "Foo", "Some", "The", "Bar", "Baz", "Example", "Fake")

# Role vocabulary — tokens built purely from role words name a KIND of thing, not a concrete type
# (pattern recipes and convention docs speak in these). Curated; extend when a new one shows up.
GENERIC_VOCAB = {
    "ConfigComponent", "ViewComponent", "EventComponent", "KeyComponent",
    "IdComponent", "FKComponent", "IdFKComponent", "StateComponent", "KindComponent",
    "SpawnSystem", "DespawnSystem", "ReactiveSystem", "WorldInitSystem", "MapGenerationSystem",
    "TemplateTag", "TargetTag", "OutputTag", "ActionTag", "DraftTag", "FactTag",
    "ItemConfig", "CostConfig", "OutcomeConfig",
}

# A body line mentioning a symbol as HISTORY (explicitly removed/retired/renamed) is not a claim
# that it exists — suppress the ghost there.
HISTORY_MARKERS = ("remov", "retir", "renam", "привид", "ghost", "видален", "перейменован", "неіснуюч")

# External (engine/package) types that legitimately carry a role suffix but live outside Assets/**.cs.
ALLOWLIST = {
    "InputSystem", "EventSystem", "UnityEvent",                    # Unity
    "ChangeEvent", "ScrollView", "ListView", "TreeView", "GridView",  # UI Toolkit / App UI
    "AEntitySetSystem", "ISystem",                                 # DefaultEcs
    "IInstaller",                                                  # VContainer
}

DECL_RE = re.compile(r"\b(?:class|struct|interface|enum)\s+([A-Za-z_]\w*)")
TOKEN_RE = re.compile(r"[A-Za-z][A-Za-z0-9]*")
BLOCK_COMMENT_RE = re.compile(r"/\*.*?\*/", re.S)
LINE_COMMENT_RE = re.compile(r"//[^\n]*")
STRING_RE = re.compile(r'"(?:\\.|[^"\\])*"')


def collect_code_facts(root: Path):
    """Two ground-truth sets from Assets/**.cs:
    declarations (class/struct/interface/enum names) — the strict set for code_refs;
    identifiers — every identifier in real code (comments and string literals stripped,
    so a name living only in a comment stays a ghost) — the body-mention set.
    """
    decls, idents = set(), set()
    for cs in root.rglob("*.cs"):
        if SKIP_DIRS.intersection(cs.parts):
            continue
        try:
            text = cs.read_text(encoding="utf-8", errors="replace")
        except OSError:
            continue
        decls.update(DECL_RE.findall(text))
        code = BLOCK_COMMENT_RE.sub(" ", text)
        code = LINE_COMMENT_RE.sub(" ", code)
        code = STRING_RE.sub(" ", code)
        idents.update(TOKEN_RE.findall(code))
    return decls, idents


def is_code_claim(token: str) -> bool:
    """PascalCase + role suffix, not the bare suffix / role vocabulary / a teaching placeholder."""
    if not token[0].isupper() or token in ROLE_SUFFIXES or token in GENERIC_VOCAB:
        return False
    if re.match(r"^T[A-Z]", token):  # generic type parameter (TSubSystem, TResourceTag)
        return False
    if any(token.startswith(p) and len(token) > len(p) for p in PLACEHOLDER_PREFIXES):
        return False
    return any(token.endswith(s) and len(token) > len(s) for s in ROLE_SUFFIXES)


def base_name(token: str) -> str:
    """Strip generic args and take the first dotted segment: Foo<Bar>.Baz -> Foo."""
    return token.split("<")[0].split(".")[0]


def parse_code_refs(lines):
    """Yield (line_no, name) for every entry under a frontmatter `code_refs:` key.

    Handles both inline lists `systems: [A, B]` and dash lists `- A`.
    Only called on the frontmatter slice (between the --- markers).
    """
    in_refs = False
    refs_indent = 0
    for i, line in enumerate(lines, start=1):
        stripped = line.strip()
        indent = len(line) - len(line.lstrip())
        if stripped.startswith("code_refs:"):
            in_refs = True
            refs_indent = indent
            continue
        if not in_refs:
            continue
        if stripped and indent <= refs_indent and not stripped.startswith("-"):
            in_refs = False
            continue
        for m in re.finditer(r"\[([^\]]*)\]", line):
            for name in m.group(1).split(","):
                name = name.strip().strip("\"'")
                if name:
                    yield i, name
        dash = re.match(r"^\s*-\s*([\w.<>]+)\s*$", line)
        if dash:
            yield i, dash.group(1)


def lint_file(md: Path, decls: set, idents: set):
    ghosts = []
    try:
        lines = md.read_text(encoding="utf-8", errors="replace").splitlines()
    except OSError:
        return ghosts

    # Frontmatter slice — code_refs entries are strict: the name must be DECLARED.
    fm_end = 0
    if lines and lines[0].strip() == "---":
        for i in range(1, len(lines)):
            if lines[i].strip() == "---":
                fm_end = i
                break
        for line_no, name in parse_code_refs(lines[1:fm_end]):
            base = base_name(name)
            if base and base not in decls and base not in ALLOWLIST:
                ghosts.append((line_no + 1, base, "code_refs"))

    # Body: every role-suffixed Pascal token (prose, backticks, and code fences alike) must
    # exist as an identifier somewhere in real code.
    seen_on_line = set()
    suppressed = False  # region toggle: <!-- doc-lint: off --> … <!-- doc-lint: on -->
    for i in range(fm_end, len(lines)):
        line = lines[i]
        low = line.lower()
        if "doc-lint: off" in low:
            suppressed = True
            continue
        if "doc-lint: on" in low:
            suppressed = False
            continue
        if suppressed or any(m in low for m in HISTORY_MARKERS):
            continue
        if ".canvas" in low:  # canvas-map rows carry user-intake labels, not code claims
            continue
        for raw in TOKEN_RE.findall(line):
            token = base_name(raw)
            key = (i + 1, token)
            if key in seen_on_line or not is_code_claim(token):
                continue
            seen_on_line.add(key)
            if token not in idents and token not in ALLOWLIST:
                ghosts.append((i + 1, token, "body"))
    return ghosts


def main():
    ap = argparse.ArgumentParser(description="MD ghost detector: doc symbol claims vs C# declarations")
    ap.add_argument("--scope", default="", help="only lint .md files whose path contains this substring")
    ap.add_argument("--quiet", action="store_true", help="print the summary line only")
    args = ap.parse_args()

    decls, idents = collect_code_facts(REPO / "Assets")
    mds = [p for p in REPO.rglob("*.md")
           if not SKIP_DIRS.intersection(p.parts) and args.scope in str(p.relative_to(REPO))]

    total = []
    for md in sorted(mds):
        rel = md.relative_to(REPO)
        for line_no, name, where in lint_file(md, decls, idents):
            total.append((rel, line_no, name, where))

    files_hit = len({t[0] for t in total})
    print(f"doc-lint: {len(total)} ghost(s) in {files_hit} file(s) · "
          f"{len(mds)} md scanned · {len(decls)} symbols declared")
    if not args.quiet:
        for rel, line_no, name, where in total:
            print(f"  {rel}:{line_no}  {name}  ({where})")
    sys.exit(1 if total else 0)


if __name__ == "__main__":
    main()
