# -*- coding: utf-8 -*-
"""Estimated FPS vs on-screen tactical icons — MuMu-class Android emulator."""
from pathlib import Path
import matplotlib.pyplot as plt
import numpy as np

# MuMu-like: GLES translation + host contention → higher fixed cost than native mid phone.
# TopDog path: UITK markers updated in RefreshAll (~12.5 Hz) + every-frame render/layout tax.

def fps_curve(n, fixed_ms, per_icon_ms, fps_cap=60.0):
    ft = fixed_ms + n * per_icon_ms
    return np.minimum(fps_cap, 1000.0 / np.maximum(ft, 0.5))

n = np.arange(0, 161, 1)

# Curves (estimates, labeled as such)
uitk_mumu = fps_curve(n, fixed_ms=12.0, per_icon_ms=0.45, fps_cap=60)
uitk_optimistic = fps_curve(n, fixed_ms=9.0, per_icon_ms=0.22, fps_cap=60)
gpu_batch = fps_curve(n, fixed_ms=10.0, per_icon_ms=0.03, fps_cap=60)

fig, ax = plt.subplots(figsize=(10, 6), dpi=140)
ax.plot(n, uitk_mumu, color="#c0392b", lw=2.4, label="MuMu · UITK markers (est. mid)")
ax.plot(n, uitk_optimistic, color="#e67e22", lw=1.8, ls="--", label="MuMu · UITK lighter dirty-refresh (est.)")
ax.plot(n, gpu_batch, color="#27ae60", lw=2.0, label="GPU / Instancing billboards (est. target)")

ax.axhline(30, color="#7f8c8d", ls=":", lw=1, label="30 FPS")
ax.axhline(60, color="#bdc3c7", ls=":", lw=1)

# Typical scene sizes from current TopDog tests
ax.axvline(7, color="#3498db", ls="--", lw=1, alpha=0.85)
ax.annotate("mt_intra ~7", xy=(7, float(uitk_mumu[7])), xytext=(22, 54),
            fontsize=9, color="#2980b9",
            arrowprops=dict(arrowstyle="->", color="#2980b9", lw=0.8))
ax.axvline(25, color="#8e44ad", ls="--", lw=1, alpha=0.75)
ax.annotate("~25 icons (ships+proxies)", xy=(25, float(uitk_mumu[25])),
            xytext=(55, 42), fontsize=9, color="#8e44ad",
            arrowprops=dict(arrowstyle="->", color="#8e44ad", lw=0.8))

ax.set_xlabel("On-screen tactical icons (UITK markers / billboards)", fontsize=11)
ax.set_ylabel("Estimated frame rate (FPS)", fontsize=11)
ax.set_title("TopDog tactical icons vs FPS — MuMu-class emulator (modeled, not measured)", fontsize=12)
ax.set_xlim(0, 160)
ax.set_ylim(0, 65)
ax.grid(True, alpha=0.35)
ax.legend(loc="upper right", fontsize=9)

note = (
    "Assumptions: MuMu Android 12 / GLES translation; skybox RT blit + UITK shell fixed cost;\n"
    "per-icon cost = World→screen + VisualElement style dirty (main thread).\n"
    "Not a profiler capture — band for planning only. Actual device may ±30%."
)
ax.text(0.02, 0.02, note, transform=ax.transAxes, fontsize=8, va="bottom",
        bbox=dict(boxstyle="round", facecolor="white", alpha=0.88, edgecolor="#bdc3c7"))

out = Path(r"H:\game_dev\top_dog_unity\docs\figures\mumu_icons_vs_fps_estimate.png")
fig.tight_layout()
fig.savefig(out, bbox_inches="tight")
print("wrote", out, "bytes", out.stat().st_size)
