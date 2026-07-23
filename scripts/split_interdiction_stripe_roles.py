#!/usr/bin/env python3
"""Split interdiction shell_stripe.png along TR–BL into fixed (blue) / mobile (red)."""
from __future__ import annotations

from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "content" / "vfx" / "interdiction" / "shell_stripe.png"
OUT_DIRS = [
    ROOT / "content" / "vfx" / "interdiction",
    ROOT / "TopDog.Unity" / "Assets" / "StreamingAssets" / "content" / "vfx" / "interdiction",
]


def main() -> None:
    src = np.asarray(Image.open(SRC).convert("RGBA")).astype(np.float32)
    h, w = src.shape[:2]
    if w != h:
        raise SystemExit(f"expected square, got {w}x{h}")
    yy, xx = np.mgrid[0:h, 0:w]
    t = (yy + 0.5) / h - (1.0 - (xx + 0.5) / w)
    feather = 0.04
    soft_a = np.clip(0.5 + t / (2 * feather), 0, 1)[..., None]
    mx = w - 1 - yy
    my = h - 1 - xx
    mirrored = src[my, mx]
    half_a = src * soft_a + mirrored * (1.0 - soft_a)
    half_b = src * (1.0 - soft_a) + mirrored * soft_a

    r, g, b = half_a[:, :, 0], half_a[:, :, 1], half_a[:, :, 2]
    redness = np.clip((r * 0.7 - g * 0.25 - b * 0.2) / 80.0, 0, 1)
    mobile = half_a.copy()
    mobile[:, :, :3] = np.clip(mobile[:, :, :3] * (0.85 + 0.3 * redness[..., None]), 0, 255)

    r, g, b = half_b[:, :, 0], half_b[:, :, 1], half_b[:, :, 2]
    blueness = np.clip((b * 0.65 + r * 0.15 - g * 0.25) / 80.0, 0, 1)
    fixed = half_b.copy()
    fixed[:, :, :3] = np.clip(fixed[:, :, :3] * (0.85 + 0.3 * blueness[..., None]), 0, 255)

    # Prefer blue-rich half as fixed, red-rich as mobile (swap if needed)
    mean_blue_a = float(np.mean(half_a[:, :, 2] - half_a[:, :, 0]))
    mean_blue_b = float(np.mean(half_b[:, :, 2] - half_b[:, :, 0]))
    if mean_blue_a > mean_blue_b:
        fixed, mobile = half_a, half_b
        r, g, b = fixed[:, :, 0], fixed[:, :, 1], fixed[:, :, 2]
        blueness = np.clip((b * 0.65 + r * 0.15 - g * 0.25) / 80.0, 0, 1)
        fixed = fixed.copy()
        fixed[:, :, :3] = np.clip(fixed[:, :, :3] * (0.85 + 0.3 * blueness[..., None]), 0, 255)
        r, g, b = mobile[:, :, 0], mobile[:, :, 1], mobile[:, :, 2]
        redness = np.clip((r * 0.7 - g * 0.25 - b * 0.2) / 80.0, 0, 1)
        mobile = mobile.copy()
        mobile[:, :, :3] = np.clip(mobile[:, :, :3] * (0.85 + 0.3 * redness[..., None]), 0, 255)

    fixed_u8 = np.clip(fixed, 0, 255).astype(np.uint8)
    mobile_u8 = np.clip(mobile, 0, 255).astype(np.uint8)
    for d in OUT_DIRS:
        d.mkdir(parents=True, exist_ok=True)
        Image.fromarray(fixed_u8, "RGBA").save(d / "fixed_stripe.png")
        Image.fromarray(mobile_u8, "RGBA").save(d / "mobile_stripe.png")
        Image.open(SRC).save(d / "shell_stripe.png")
        print("wrote", d)


if __name__ == "__main__":
    main()
