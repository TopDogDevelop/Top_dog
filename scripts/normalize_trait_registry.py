#!/usr/bin/env python3
"""Normalize sheep-line traits: global registry, not template-exclusive."""
import json
from pathlib import Path

TRAITS = {
    "trait_duck_source": ("duck_source", "ui_neutral", False),
    "trait_duck_sauce": ("duck_sauce", "ui_neutral", False),
    "trait_board_favor": ("board_favor", "ui_positive", False),
    "trait_commander_cert_sheep": ("commander_cert_sheep", "ui_negative", False),
    "trait_commander_cert": ("commander_cert", "ui_negative", False),
    "trait_discord_source": ("discord_source", "ui_negative", False),
    "trait_board_summon": ("board_summon", "ui_positive", False),
    "trait_planning_support": ("planning_support", "ui_positive", False),
    "trait_fool_loyal": ("fool_loyal", "ui_positive", False),
    "trait_devotion": ("devotion", "ui_positive", False),
    "trait_recruit_officer": ("recruit_officer", "ui_positive", False),
    "trait_rookie_instructor": ("rookie_instructor", "ui_positive", False),
    "trait_immovable": ("immovable", "ui_neutral", False),
}

ROOT = Path(__file__).resolve().parents[1] / "content" / "traits"
ALIASES = json.loads((ROOT / "trait_aliases.json").read_text(encoding="utf-8"))

for path in ROOT.glob("trait_*.json"):
    if path.name == "trait_aliases.json":
        continue
    data = json.loads(path.read_text(encoding="utf-8"))
    tid = data.get("traitId")
    if tid in TRAITS:
        mech, ui_tag, pool = TRAITS[tid]
        data["mechanismId"] = mech
        data["recruitPool"] = pool
        data["presentationTags"] = [ui_tag]
    path.write_text(json.dumps(data, ensure_ascii=False) + "\n", encoding="utf-8")

print("Updated", len(TRAITS), "trait JSON files")
