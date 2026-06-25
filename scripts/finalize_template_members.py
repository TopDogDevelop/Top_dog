#!/usr/bin/env python3
"""Assign identityCode/accountSuffix, drop placeholder rows, write UTF-8 BOM CSV."""
import csv
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "content" / "starting_templates"
BACKDROPS = ["战士", "射手", "法师", "刺客"]

HEADER_ZH = [
    "现实身份码八位", "账号序号两位", "现实名栏", "游戏内名", "稀有等级", "简介", "标签",
    "归属感", "资金", "精力", "智慧", "综合账号建设值", "吨位专精", "词条", "底图", "形象",
    "多开编组ID", "名下资产", "备注",
]
HEADER_EN = [
    "identityCode", "accountSuffix", "accountName", "name", "rarity", "bio", "labels",
    "legionBelonging", "funds", "energy", "wisdom", "accountBuildScore", "tonnageSpecialties",
    "traitIds", "cardBackdrop", "portraitRef", "multiboxGroupId", "personalAssets", "notes",
]


def read_members(path: Path) -> list[dict[str, str]]:
    with path.open(encoding="utf-8-sig", newline="") as f:
        rows = list(csv.reader(f))
    if len(rows) < 3:
        return []
    keys = rows[1]
    out = []
    for row in rows[2:]:
        if not any(cell.strip() for cell in row):
            continue
        d = {keys[i]: (row[i] if i < len(row) else "") for i in range(len(keys))}
        if not d.get("name", "").strip():
            continue
        out.append(d)
    return out


def finalize(rows: list[dict[str, str]], start_code: int = 10000001) -> list[dict[str, str]]:
    name_to_code: dict[str, str] = {}
    suffix_ctr: dict[str, int] = {}
    next_code = start_code
    finalized = []

    def reserve_code(ic: str) -> None:
        nonlocal next_code
        n = int(ic)
        if n >= next_code:
            next_code = n + 1

    for i, r in enumerate(rows):
        account = (r.get("accountName") or r.get("name") or "").strip()
        if not account:
            account = r["name"].strip()

        ic = (r.get("identityCode") or "").strip()
        if ic:
            name_to_code.setdefault(account, ic)
            reserve_code(ic)
        elif account in name_to_code:
            ic = name_to_code[account]
        else:
            ic = str(next_code)
            name_to_code[account] = ic
            next_code += 1

        suffix_ctr[ic] = suffix_ctr.get(ic, 0) + 1
        suf = (r.get("accountSuffix") or "").strip()
        if not suf:
            suf = f"{suffix_ctr[ic]:02d}"
        elif suf.isdigit():
            suf = f"{int(suf):02d}"

        r["identityCode"] = ic
        r["accountSuffix"] = suf
        if not r.get("portraitRef", "").strip():
            r["portraitRef"] = ic
        if not r.get("cardBackdrop", "").strip():
            r["cardBackdrop"] = BACKDROPS[i % len(BACKDROPS)]
        finalized.append(r)

    multibox_ids = {ic for ic, n in suffix_ctr.items() if n > 1}
    for r in finalized:
        traits = r.get("traitIds", "")
        if "多开" in traits or r["identityCode"] in multibox_ids:
            if not r.get("multiboxGroupId", "").strip():
                r["multiboxGroupId"] = f"mb_{r['identityCode']}"
    return finalized


def write_members(path: Path, rows: list[dict[str, str]]) -> None:
    with path.open("w", encoding="utf-8-sig", newline="") as f:
        w = csv.writer(f)
        w.writerow(HEADER_ZH)
        w.writerow(HEADER_EN)
        for r in rows:
            w.writerow([r.get(k, "") for k in HEADER_EN])


def write_identities(path: Path, rows: list[dict[str, str]], existing: dict[str, dict]) -> None:
    seen: dict[str, str] = {}
    for r in rows:
        ic = r["identityCode"]
        if ic not in seen:
            seen[ic] = r.get("accountName", "").strip()
    with path.open("w", encoding="utf-8-sig", newline="") as f:
        w = csv.writer(f)
        w.writerow(["现实身份码八位", "现实名栏", "真实世界标签", "备注"])
        w.writerow(["identityCode", "accountName", "rwTags", "notes"])
        for ic in sorted(seen):
            ex = existing.get(ic, {})
            w.writerow([ic, seen[ic], ex.get("rwTags", ""), ex.get("notes", "")])


def load_identities(path: Path) -> dict[str, dict]:
    if not path.exists():
        return {}
    with path.open(encoding="utf-8-sig", newline="") as f:
        rows = list(csv.reader(f))
    if len(rows) < 3:
        return {}
    keys = rows[1]
    out = {}
    for row in rows[2:]:
        d = {keys[i]: (row[i] if i < len(row) else "") for i in range(len(keys))}
        ic = d.get("identityCode", "").strip()
        if ic:
            out[ic] = d
    return out


def main() -> None:
    src_name = sys.argv[1] if len(sys.argv) > 1 else "template_ash_coalition"
    dst_name = sys.argv[2] if len(sys.argv) > 2 else "template_1"
    src = ROOT / f"{src_name}.members.csv"
    dst_members = ROOT / f"{dst_name}.members.csv"
    dst_identities = ROOT / f"{dst_name}.identities.csv"
    src_identities = ROOT / f"{src_name}.identities.csv"

    rows = finalize(read_members(src))
    write_members(dst_members, rows)
    write_identities(dst_identities, rows, load_identities(src_identities))
    print(f"{len(rows)} members -> {dst_members.name}")
    print(f"identities -> {dst_identities.name}")


if __name__ == "__main__":
    main()
