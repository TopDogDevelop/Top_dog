#!/usr/bin/env python3
"""Import Second Galaxy field-aura VFX prefabs into TopDog.Unity (placeholder)."""
from __future__ import annotations

import argparse
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "TopDog.Unity" / "Assets" / "Art" / "SG" / "FieldAuraVfx"
SG_BUNDLE = Path(r"e:\sg\decrypted\bundles\fx_equipmentfx_abs.b")

PREFABS = (
    "FX_ArmorMaintainField_Active_LOD0",
    "FX_DisturbWeaknessField_Active_LOD0",
    "GroupDestroy_Energy_Active_LOD0",
)


def main() -> int:
    parser = argparse.ArgumentParser(description="Build field aura VFX assets")
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()
    OUT.mkdir(parents=True, exist_ok=True)
    print(f"output: {OUT}")
    print(f"source bundle: {SG_BUNDLE} exists={SG_BUNDLE.exists()}")
    for name in PREFABS:
        print(f"  prefab: {name}")
    if args.dry_run:
        print("dry-run only")
    else:
        print("manual Unity import still required for SG shaders")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
