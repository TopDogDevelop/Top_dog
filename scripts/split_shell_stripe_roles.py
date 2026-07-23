#!/usr/bin/env python3
"""Split shell_stripe.png along TR–BL diagonal into role textures.

- Gold/yellow-rich half → armor_stripe.png (mirrored fill)
- Blue/purple-rich half → shield_stripe.png (mirrored fill)

Writes content/vfx/field_aura and StreamingAssets mirror.
"""
from __future__ import annotations

from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "content" / "vfx" / "field_aura" / "shell_stripe.png"
OUT_DIRS = [
    ROOT / "content" / "vfx" / "field_aura",
    ROOT / "TopDog.Unity" / "Assets" / "StreamingAssets" / "content" / "vfx" / "field_aura",
]


def main() -> None:
    src = np.asarray(Image.open(SRC).convert("RGBA")).astype(np.float32)
    h, w = src.shape[:2]
    if w != h:
        raise SystemExit(f"expected square, got {w}x{h}")
    n = w
    yy, xx = np.mgrid[0:h, 0:w]
    # TR–BL: gold-rich below (yy/h > 1 - xx/w)
    t = (yy + 0.5) / h - (1.0 - (xx + 0.5) / w)
    feather = 0.04
    soft_gold = np.clip(0.5 + t / (2 * feather), 0, 1)[..., None]
    mx = n - 1 - yy
    my = n - 1 - xx
    mirrored = src[my, mx]
    armor = src * soft_gold + mirrored * (1.0 - soft_gold)
    shield = src * (1.0 - soft_gold) + mirrored * soft_gold

    r, g, b = armor[:, :, 0], armor[:, :, 1], armor[:, :, 2]
    goldness = np.clip((r * 0.45 + g * 0.55 - b) / 80.0, 0, 1)
    armor[:, :, :3] = np.clip(armor[:, :, :3] * (0.85 + 0.25 * goldness[..., None]), 0, 255)

    r, g, b = shield[:, :, 0], shield[:, :, 1], shield[:, :, 2]
    blueness = np.clip((b * 0.65 + r * 0.25 - g * 0.35) / 80.0, 0, 1)
    shield[:, :, :3] = np.clip(shield[:, :, :3] * (0.85 + 0.25 * blueness[..., None]), 0, 255)

    armor_u8 = np.clip(armor, 0, 255).astype(np.uint8)
    shield_u8 = np.clip(shield, 0, 255).astype(np.uint8)
    for d in OUT_DIRS:
        d.mkdir(parents=True, exist_ok=True)
        Image.fromarray(armor_u8, "RGBA").save(d / "armor_stripe.png")
        Image.fromarray(shield_u8, "RGBA").save(d / "shield_stripe.png")
        print("wrote", d)


if __name__ == "__main__":
    main()
