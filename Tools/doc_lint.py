#!/usr/bin/env python3
"""doc-lint — deterministic structural lint for the repo's current .md docs.

Verifies every symbol name a doc claims against the actual C# declarations:
  1. frontmatter `code_refs:` entries — checked STRICTLY (every name must be declared);
  2. body mentions — any PascalCase token carrying a project role suffix
     (System/Component/Tag/Event/Config/Installer/View) that is not declared in Assets/**.cs.

A name present in a doc but absent from the code is a GHOST — either doc rot or a rename
the doc missed. Every ```clojure fence is also read as Clojure data: delimiters, strings,
map cardinality, and the notation's supported reader macros are checked. Exit code 1 if
any ghost or Clojure syntax error is found.

Completed task history under Flows/Archive is deliberately excluded: its old symbol names
are dated historical facts, not claims about the current codebase.

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
             ".ecs-graph", ".di-graph", ".sdd-flow", "design-mockups"}

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
    "IInstaller",                                                  # VContainer
}

DECL_RE = re.compile(r"\b(?:class|struct|interface|enum)\s+([A-Za-z_]\w*)")
TOKEN_RE = re.compile(r"[A-Za-z][A-Za-z0-9]*")
BLOCK_COMMENT_RE = re.compile(r"/\*.*?\*/", re.S)
LINE_COMMENT_RE = re.compile(r"//[^\n]*")
STRING_RE = re.compile(r'"(?:\\.|[^"\\])*"')
CLOJURE_FENCE_RE = re.compile(r"^\s*```clojure\s*$")
FENCE_END_RE = re.compile(r"^\s*```\s*$")


class ClojureReadError(Exception):
    def __init__(self, message: str, line: int, column: int):
        super().__init__(message)
        self.message = message
        self.line = line
        self.column = column


class ClojureReader:
    """Small dependency-free reader for the instruction notation's data forms."""

    DISCARDED = object()
    OPEN_TO_CLOSE = {"(": ")", "[": "]", "{": "}"}
    CLOSERS = frozenset(OPEN_TO_CLOSE.values())
    STRING_ESCAPES = frozenset('btnrf\\"')

    def __init__(self, source: str, base_line: int):
        self.source = source
        self.base_line = base_line
        self.pos = 0
        self.line = base_line
        self.column = 1

    def error(self, message: str, line=None, column=None):
        raise ClojureReadError(message, line or self.line, column or self.column)

    def peek(self, offset=0):
        pos = self.pos + offset
        return self.source[pos] if pos < len(self.source) else ""

    def advance(self):
        char = self.peek()
        if not char:
            return ""
        self.pos += 1
        if char == "\n":
            self.line += 1
            self.column = 1
        else:
            self.column += 1
        return char

    def skip_layout(self):
        while True:
            while self.peek() and (self.peek().isspace() or self.peek() == ","):
                self.advance()
            if self.peek() != ";":
                return
            while self.peek() and self.advance() != "\n":
                pass

    def read_all(self):
        while True:
            self.skip_layout()
            if not self.peek():
                return
            if self.peek() in self.CLOSERS:
                self.error(f"unexpected closing delimiter {self.peek()!r}")
            self.read_form()

    def read_form(self):
        self.skip_layout()
        char = self.peek()
        if not char:
            self.error("expected a form, reached end of fence")
        if char in self.OPEN_TO_CLOSE:
            return self.read_collection()
        if char in self.CLOSERS:
            self.error(f"unexpected closing delimiter {char!r}")
        if char == '"':
            self.read_string()
            return object()
        if char == "#":
            return self.read_dispatch()
        if char == "^":
            return self.read_metadata()
        if char == "'":
            self.advance()
            self.read_form()
            return object()
        return self.read_atom()

    def read_collection(self, set_literal=False):
        start_line, start_column = self.line, self.column
        opener = self.advance()
        closer = self.OPEN_TO_CLOSE[opener]
        count = 0
        while True:
            self.skip_layout()
            char = self.peek()
            if not char:
                kind = "set" if set_literal else "collection"
                self.error(f"unclosed {kind}: expected {closer!r}", start_line, start_column)
            if char == closer:
                self.advance()
                break
            if char in self.CLOSERS:
                self.error(f"mismatched delimiter: expected {closer!r}, found {char!r}")
            if self.read_form() is not self.DISCARDED:
                count += 1
        if opener == "{" and not set_literal and count % 2:
            self.error("map literal must contain an even number of forms", start_line, start_column)
        return object()

    def read_string(self):
        start_line, start_column = self.line, self.column
        self.advance()
        while True:
            char = self.peek()
            if not char:
                self.error("unterminated string", start_line, start_column)
            self.advance()
            if char == '"':
                return
            if char != "\\":
                continue
            escaped = self.peek()
            if not escaped:
                self.error("unterminated string escape")
            if escaped == "u":
                self.advance()
                digits = "".join(self.advance() for _ in range(4))
                if len(digits) != 4 or not all(c in "0123456789abcdefABCDEF" for c in digits):
                    self.error("invalid unicode escape; expected four hex digits")
            elif escaped in self.STRING_ESCAPES:
                self.advance()
            else:
                self.error(f"unsupported string escape \\{escaped}")

    def read_dispatch(self):
        start_line, start_column = self.line, self.column
        self.advance()
        dispatch = self.peek()
        if dispatch == "{":
            return self.read_collection(set_literal=True)
        if dispatch == "_":
            self.advance()
            self.read_form()
            return self.DISCARDED
        self.error("unsupported reader macro; only #{} and #_ are allowed", start_line, start_column)

    def read_metadata(self):
        self.advance()
        self.read_form()
        target = self.read_form()
        if target is self.DISCARDED:
            self.error("metadata target cannot be discarded")
        return object()

    def read_atom(self):
        start = self.pos
        while self.peek():
            char = self.peek()
            if char.isspace() or char == "," or char in "()[]{}\";":
                break
            if char in "^'" and self.pos == start:
                break
            self.advance()
        if self.pos == start:
            self.error(f"unexpected character {self.peek()!r}")
        return object()


def lint_clojure_fences(md: Path):
    """Return (line, column, message) for every invalid current Clojure fence."""
    try:
        lines = md.read_text(encoding="utf-8", errors="replace").splitlines()
    except OSError:
        return []

    issues = []
    opener_line = None
    content = []
    for line_no, line in enumerate(lines, start=1):
        if opener_line is None:
            if CLOJURE_FENCE_RE.match(line):
                opener_line = line_no
                content = []
            continue
        if FENCE_END_RE.match(line):
            try:
                ClojureReader("\n".join(content), opener_line + 1).read_all()
            except ClojureReadError as error:
                issues.append((error.line, error.column, error.message))
            opener_line = None
            content = []
        else:
            content.append(line)

    if opener_line is not None:
        issues.append((opener_line, 1, "unclosed ```clojure fence"))
    return issues


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
    ap = argparse.ArgumentParser(description="MD structural lint: code ghosts and Clojure fences")
    ap.add_argument("--scope", default="", help="only lint .md files whose path contains this substring")
    ap.add_argument("--quiet", action="store_true", help="print the summary line only")
    args = ap.parse_args()

    decls, idents = collect_code_facts(REPO / "Assets")
    mds = [p for p in REPO.rglob("*.md")
           if not SKIP_DIRS.intersection(p.parts)
           and not str(p.relative_to(REPO)).startswith("Flows/Archive/")
           and args.scope in str(p.relative_to(REPO))]

    total = []
    clojure_issues = []
    for md in sorted(mds):
        rel = md.relative_to(REPO)
        for line_no, name, where in lint_file(md, decls, idents):
            total.append((rel, line_no, name, where))
        for line_no, column, message in lint_clojure_fences(md):
            clojure_issues.append((rel, line_no, column, message))

    files_hit = len({t[0] for t in total})
    print(f"doc-lint: {len(total)} ghost(s) in {files_hit} file(s) · "
          f"{len(clojure_issues)} Clojure syntax error(s) · "
          f"{len(mds)} md scanned · {len(decls)} symbols declared")
    if not args.quiet:
        for rel, line_no, name, where in total:
            print(f"  {rel}:{line_no}  {name}  ({where})")
        for rel, line_no, column, message in clojure_issues:
            print(f"  {rel}:{line_no}:{column}  {message}  (clojure)")
    sys.exit(1 if total or clojure_issues else 0)


if __name__ == "__main__":
    main()
