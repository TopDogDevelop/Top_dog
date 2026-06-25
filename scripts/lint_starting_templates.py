#!/usr/bin/env python3
"""Lint starting_templates CSV: identity uniqueness, tonnage aliases."""
from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TEMPLATES = ROOT / "content" / "starting_templates"
TONNAGE = ROOT / "content" / "ships" / "tonnage_classes.json"


def load_aliases() -> set[str]:
    data = json.loads(TONNAGE.read_text(encoding="utf-8"))
    aliases = set(data.get("aliases", {}).keys())
    for v in data.get("aliases", {}).values():
        aliases.add(v)
    return aliases


def parse_tonnage_tokens(raw: str) -> list[str]:
    names: list[str] = []
    for token in raw.strip().split():
        i = len(token) - 1
        while i >= 0 and token[i].isdigit():
            i -= 1
        names.append(token[: i + 1] if i >= 0 else token)
    return names


def lint_template(template_id: str, aliases: set[str]) -> list[str]:
    errors: list[str] = []
    members = TEMPLATES / f"{template_id}.members.csv"
    if not members.exists():
        return errors
    lines = members.read_text(encoding="utf-8-sig").splitlines()
    key_row = next((i for i, ln in enumerate(lines) if "identityCode" in ln and "accountSuffix" in ln), -1)
    if key_row < 0:
        return [f"{template_id}: missing header row"]
    cols = [c.strip() for c in lines[key_row].split(",")]
    idx = {name: i for i, name in enumerate(cols)}
    seen_identity: set[str] = set()
    for r, line in enumerate(lines[key_row + 1 :], start=key_row + 2):
        if not line.strip():
            continue
        row = [c.strip() for c in line.split(",")]
        code = row[idx["identityCode"]] if idx.get("identityCode", -1) < len(row) else ""
        suffix = row[idx["accountSuffix"]] if idx.get("accountSuffix", -1) < len(row) else ""
        if code:
            pair = f"{code}:{suffix}"
            if pair in seen_identity:
                errors.append(f"{template_id}:{r} duplicate identityCode+accountSuffix {pair}")
            seen_identity.add(pair)
        if "tonnageSpecialties" in idx and idx["tonnageSpecialties"] < len(row):
            raw = row[idx["tonnageSpecialties"]]
            for name in parse_tonnage_tokens(raw):
                if name and name not in aliases:
                    errors.append(f"{template_id}:{r} unknown tonnage alias '{name}'")
    return errors


def main() -> int:
    aliases = load_aliases()
    errors: list[str] = []
    for meta in sorted(TEMPLATES.glob("*.meta.csv")):
        tid = meta.name.replace(".meta.csv", "")
        errors.extend(lint_template(tid, aliases))
    if errors:
        print("lint_starting_templates FAILED:")
        for e in errors:
            print(" ", e)
        return 1
    print("lint_starting_templates OK")
    return 0


if __name__ == "__main__":
    sys.exit(main())
