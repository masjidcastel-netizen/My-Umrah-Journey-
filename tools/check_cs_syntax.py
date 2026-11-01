#!/usr/bin/env python3
"""Offline sanity check for the C# sources. NOT a compiler.
Checks: balanced braces/parens/brackets (string/comment/char aware), `using System;` (ambiguity with UnityEngine.Object/Random),
file ends cleanly, namespace present. Exit code 1 on problems."""
import sys, pathlib

def strip(src):
    out, i, n = [], 0, len(src)
    while i < n:
        c = src[i]
        if src.startswith("//", i):
            while i < n and src[i] != "\n": i += 1
        elif src.startswith("/*", i):
            j = src.find("*/", i + 2); i = n if j < 0 else j + 2
        elif src.startswith('@"', i):
            i += 2
            while i < n:
                if src[i] == '"':
                    if src.startswith('""', i): i += 2; continue
                    break
                i += 1
            i += 1
        elif c == '"':
            i += 1
            while i < n and src[i] != '"':
                i += 2 if src[i] == "\\" else 1
            i += 1
        elif c == "'":
            i += 1
            while i < n and src[i] != "'":
                i += 2 if src[i] == "\\" else 1
            i += 1
        else:
            out.append(c); i += 1
    return "".join(out)

def check(path):
    problems = []
    src = path.read_text(encoding="utf-8")
    s = strip(src)
    pairs = {")": "(", "]": "[", "}": "{"}
    stack = []
    line = 1
    for ch in s:
        if ch == "\n": line += 1
        elif ch in "([{": stack.append((ch, line))
        elif ch in pairs:
            if not stack or stack[-1][0] != pairs[ch]:
                problems.append(f"unbalanced '{ch}' at line {line}"); break
            stack.pop()
    if stack: problems.append(f"unclosed '{stack[-1][0]}' from line {stack[-1][1]}")
    if "using System;" in src: problems.append("uses `using System;` (Object/Random ambiguity with UnityEngine)")
    if "namespace MyUmrah" not in src: problems.append("missing namespace MyUmrah")
    return problems

root = pathlib.Path(sys.argv[1] if len(sys.argv) > 1 else ".")
files = sorted(root.rglob("*.cs"))
bad = 0
for f in files:
    p = check(f)
    if p:
        bad += 1
        print("FAIL", f.relative_to(root), "; ".join(p))
print(f"{len(files)} C# files checked, {bad} with problems (syntax-level only, not a compile)")
sys.exit(1 if bad else 0)
