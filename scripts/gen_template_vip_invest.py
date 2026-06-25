#!/usr/bin/env python3
"""Generate template_vip_invest.members.csv from 词条设计.xlsx Sheet3 rules."""
import csv
from pathlib import Path

members = [
    ("绵羊伸腿", "绵羊伸腿", "s"),
    ("绵羊控股", "绵羊伸腿", "s"),
    ("绵羊制造", "绵羊伸腿", "s"),
    ("羊村酱鸭", "绵羊伸腿", "s"),
    ("盖奶", "绵羊伸腿", "a"),
    ("绵羊没腿", "绵羊伸腿", "s"),
    ("迪亚多啦", "迪亚多啦", "s"),
    ("多啦a梦", "迪亚多啦", "s"),
    ("小困包", "困包", "s"),
    ("大困包", "困包", "s"),
    ("不困包", "困包", "s"),
    ("刀疤哥", "困包", "a"),
    ("铁菊狗蛋", "铁菊公子", "c"),
    ("榴蛋", "铁菊公子", "c"),
    ("羊村星星", "羊村众人", "a"),
    ("羊村元芳", "羊村众人", "a"),
    ("羊村醉鬼", "羊村众人", "a"),
    ("羊村教练", "羊村众人", "a"),
    ("羊村守护", "羊村众人", "a"),
    ("羊村加尔", "羊村众人", "a"),
    ("羊村星猫", "羊村众人", "a"),
    ("羊村虎虎", "羊村众人", "a"),
    ("羊村盐酸", "羊村众人", "a"),
    ("羊村先生", "羊村众人", "a"),
    ("羊村光头", "羊村众人", "a"),
    ("羊村老猫", "羊村众人", "a"),
    ("羊村林冲", "羊村众人", "a"),
    ("羊村夜火", "羊村众人", "a"),
    ("羊村枫桥", "羊村众人", "a"),
    ("羊村幽篁", "羊村众人", "a"),
    ("羊村趣味", "羊村众人", "a"),
    ("羊村晓部", "羊村众人", "a"),
    ("羊村铁菊", "羊村众人", "a"),
    ("羊村78", "羊村众人", "a"),
    ("深度追踪", "深度", "s"),
    ("左手指天右手指地", "大少", "a"),
    ("左手指地右手指天", "大少", "a"),
    ("狈头军师", "狈头", "s"),
]

bios = {
    "绵羊伸腿": "我虽无意逐鹿，但知狼烟四起，不能独善其身",
    "羊村众人": "始终易得，初心难忘",
}

traits_sheep = (
    "酱鸭源头 董事会的青睐 团长之证-羊 死忠 情报源 董事会召来 策划支援 "
    "不和之源 不和之源 不和之源 愚忠 多开"
)
traits_village = "愚忠 情报源 奉献 多开"
traits_bei = "招新官 新手教官 岿然不动"
traits_multibox_only = "多开"

identity_map = {
    "绵羊伸腿": "10001001",
    "迪亚多啦": "10001002",
    "困包": "10001003",
    "铁菊公子": "10001004",
    "羊村众人": "10001005",
    "深度": "10001006",
    "大少": "10001007",
    "狈头": "10001008",
}

header_zh = [
    "现实身份码八位", "账号序号两位", "现实名栏", "游戏内名", "稀有等级", "简介", "标签",
    "归属感", "资金", "精力", "智慧", "综合账号建设值", "吨位专精", "词条", "底图", "形象",
    "多开编组ID", "名下资产", "备注",
]
header_en = [
    "identityCode", "accountSuffix", "accountName", "name", "rarity", "bio", "labels",
    "legionBelonging", "funds", "energy", "wisdom", "accountBuildScore", "tonnageSpecialties",
    "traitIds", "cardBackdrop", "portraitRef", "multiboxGroupId", "personalAssets", "notes",
]

suffix_counter: dict[str, int] = {}
rows = []
for name, account, rarity in members:
    ic = identity_map[account]
    suffix_counter[ic] = suffix_counter.get(ic, 0) + 1
    suffix = f"{suffix_counter[ic]:02d}"
    if account == "绵羊伸腿":
        traits = traits_sheep
    elif account == "羊村众人":
        traits = traits_village
    elif account == "狈头":
        traits = traits_bei
    else:
        traits = traits_multibox_only
    rows.append(
        {
            "identityCode": ic,
            "accountSuffix": suffix,
            "accountName": account,
            "name": name,
            "rarity": rarity,
            "bio": bios.get(account, ""),
            "labels": "",
            "legionBelonging": "",
            "funds": "",
            "energy": "",
            "wisdom": "",
            "accountBuildScore": "",
            "tonnageSpecialties": "",
            "traitIds": traits,
            "cardBackdrop": ["战士", "射手", "法师", "刺客"][len(rows) % 4],
            "portraitRef": "",
            "multiboxGroupId": f"mb_{ic}",
            "personalAssets": "",
            "notes": "VIP投资集团",
        }
    )

out = Path(__file__).resolve().parents[1] / "content" / "starting_templates" / "template_vip_invest.members.csv"
with out.open("w", encoding="utf-8-sig", newline="") as f:
    w = csv.writer(f)
    w.writerow(header_zh)
    w.writerow(header_en)
    for r in rows:
        w.writerow([r[k] for k in header_en])

print(f"Wrote {len(rows)} members -> {out}")
