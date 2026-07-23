#!/usr/bin/env python3
"""Export Second Galaxy field-aura textures into TopDog StreamingAssets (HF-friendly).

Loads fx_equipmentfx_abs.b with deps (fx_commonfx_abs.b, shaders_abs.b) so material
PPtrs resolve. Composites MainTex + SG tint into circular discs for UITK.
"""
from __future__ import annotations

import argparse
import json
import math
import re
import shutil
import sys
from pathlib import Path

try:
    import UnityPy
except ImportError:
    print("pip install UnityPy", file=sys.stderr)
    raise

try:
    from PIL import Image
except ImportError:
    print("pip install Pillow", file=sys.stderr)
    raise

ROOT = Path(__file__).resolve().parents[1]
OUT_CONTENT = ROOT / "content" / "vfx" / "field_aura"
OUT_STREAMING = (
    ROOT
    / "TopDog.Unity"
    / "Assets"
    / "StreamingAssets"
    / "content"
    / "vfx"
    / "field_aura"
)

BUNDLE_DIR_CANDIDATES = (
    Path(r"H:\sg\_run\decrypted\bundles"),
    Path(r"D:\refs-blobs\sg\decrypted\bundles"),
    Path(r"H:\sg\decrypted\bundles"),
)

BUNDLE_NAMES = (
    "fx_equipmentfx_abs.b",
    "fx_commonfx_abs.b",
    "shaders_abs.b",
)

# Primary materials for Active LOD0 field look (ForceField = sphere shell)
ROLE_MATERIALS: dict[str, tuple[str, ...]] = {
    "shield": (
        "FX_DisturbWeaknessField_Active_ForceField",
        "FX_DisturbWeaknessField_Active_Glow",
        "FX_DisturbWeaknessField_Active_Spot",
    ),
    "armor": (
        "FX_ArmorMaintainField_Active_ForceField",
        "FX_ArmorMaintainField_Active_Glow",
        "FX_ArmorMaintainField_Active_Fragment",
        "FX_ArmorMaintain_LoopShieldPlane",
    ),
}

DISC_SIZE = 512

# Named Texture2D exports for 3D shell (noise + tile)
SHELL_TEXTURE_EXPORTS: dict[str, tuple[str, ...]] = {
    "shell_tile.png": ("T_FX_Tile_042",),
    "noise.png": ("T_FX_Noise01", "T_FX_Noise05", "T_FX_Noise03", "T_FX_Noise04"),
    "shield_noise.png": ("T_FX_Tile_009", "T_FX_Noise01", "T_FX_Noise04"),
    "armor_noise.png": ("T_FX_Noise04", "T_FX_Noise01", "T_FX_Tile_042"),
}


def safe_name(name: str) -> str:
    name = str(name).strip() or "unnamed"
    return re.sub(r'[<>:"/\\|?*\x00-\x1f]', "_", name)[:180]


def resolve_bundle_dir() -> Path:
    for d in BUNDLE_DIR_CANDIDATES:
        if (d / "fx_equipmentfx_abs.b").is_file():
            return d
    raise FileNotFoundError("fx_equipmentfx_abs.b not found in known SG bundle dirs")


def mat_props(data) -> tuple[dict[str, object], dict[str, tuple[float, float, float, float]]]:
    """Return (tex_name_by_slot, color_by_slot) for a Material."""
    tex_names: dict[str, object] = {}
    colors: dict[str, tuple[float, float, float, float]] = {}
    sp = getattr(data, "m_SavedProperties", None)
    if sp is None:
        return tex_names, colors
    for t in getattr(sp, "m_TexEnvs", None) or []:
        key = t[0] if isinstance(t, (list, tuple)) else getattr(t, "first", None)
        val = t[1] if isinstance(t, (list, tuple)) else getattr(t, "second", None)
        tex_ref = getattr(val, "m_Texture", None) if val is not None else None
        if tex_ref is None:
            continue
        try:
            tr = tex_ref.read()
            tex_names[str(key)] = tr
        except Exception:
            continue
    for c in getattr(sp, "m_Colors", None) or []:
        key = c[0] if isinstance(c, (list, tuple)) else getattr(c, "first", None)
        val = c[1] if isinstance(c, (list, tuple)) else getattr(c, "second", None)
        if val is None:
            continue
        r = float(getattr(val, "r", 1))
        g = float(getattr(val, "g", 1))
        b = float(getattr(val, "b", 1))
        a = float(getattr(val, "a", 1))
        colors[str(key)] = (r, g, b, a)
    return tex_names, colors


def clamp01(x: float) -> float:
    return 0.0 if x < 0 else 1.0 if x > 1 else x


def tonemap_hdr(rgb: tuple[float, float, float], power: float = 1.0) -> tuple[float, float, float]:
    """SG tints are often HDR (r>1). Compress toward displayable pastel."""
    r, g, b = rgb
    # soft tone-map then normalize by max channel
    r, g, b = (math.log1p(max(0.0, c) * power) for c in (r, g, b))
    m = max(r, g, b, 1e-6)
    return (clamp01(r / m), clamp01(g / m), clamp01(b / m))


def composite_disc(
    main_img: Image.Image | None,
    tint_rgb: tuple[float, float, float],
    rim_boost: float,
    size: int = DISC_SIZE,
) -> Image.Image:
    """Soft circular field disc: SG tile (optional) × tint, radial falloff + rim."""
    out = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    cx = cy = (size - 1) * 0.5
    rad = size * 0.48

    tile = None
    if main_img is not None:
        tile = main_img.convert("RGBA").resize((size, size), Image.Resampling.BICUBIC)

    tr, tg, tb = tonemap_hdr(tint_rgb, power=0.55)
    # Push toward blue-white / gold-white per channel dominance
    pixels = out.load()
    tile_px = tile.load() if tile is not None else None

    for y in range(size):
        for x in range(size):
            dx = (x - cx) / rad
            dy = (y - cy) / rad
            r = math.sqrt(dx * dx + dy * dy)
            if r > 1.05:
                continue
            # soft fill + bright rim near edge
            fill = clamp01(1.0 - r) ** 1.35
            rim = clamp01(1.0 - abs(r - 0.78) * 5.5) * rim_boost
            a = clamp01(fill * 0.42 + rim * 0.55)

            sr = tr
            sg = tg
            sb = tb
            if tile_px is not None:
                rr, gg, bb, aa = tile_px[x, y]
                # luminance of tile modulates alpha; RGB multiplies tint
                lum = (rr + gg + bb) / (3 * 255.0)
                a = clamp01(a * (0.35 + 0.65 * (aa / 255.0) * (0.4 + 0.6 * lum)))
                sr = clamp01(tr * (0.55 + 0.45 * rr / 255.0))
                sg = clamp01(tg * (0.55 + 0.45 * gg / 255.0))
                sb = clamp01(tb * (0.55 + 0.45 * bb / 255.0))

            # brighten center toward white (shield glass / armor gold glow)
            center = clamp01(1.0 - r) ** 2
            sr = clamp01(sr * (1.0 - 0.35 * center) + center * 0.95)
            sg = clamp01(sg * (1.0 - 0.35 * center) + center * 0.95)
            sb = clamp01(sb * (1.0 - 0.25 * center) + center * 0.9)

            pixels[x, y] = (
                int(sr * 255),
                int(sg * 255),
                int(sb * 255),
                int(a * 255),
            )
    return out


def find_materials(env) -> dict[str, object]:
    found: dict[str, object] = {}
    for obj in env.objects:
        if obj.type.name != "Material":
            continue
        try:
            data = obj.read()
        except Exception:
            continue
        name = str(getattr(data, "m_Name", "") or "")
        if name:
            found[name] = data
    return found


def pick_role_assets(mats: dict[str, object], role: str) -> tuple[Image.Image | None, tuple[float, float, float], dict]:
    names = ROLE_MATERIALS[role]
    main_img = None
    tint = (0.35, 0.72, 1.0) if role == "shield" else (1.0, 0.82, 0.35)
    meta: dict = {"materials": [], "textures": []}

    for name in names:
        data = mats.get(name)
        if data is None:
            continue
        tex_map, colors = mat_props(data)
        meta["materials"].append(name)
        tint_c = colors.get("_TintColor") or colors.get("_Color")
        if tint_c is not None:
            # HDR tint preferred from ForceField
            if "ForceField" in name or tint == (0.35, 0.72, 1.0) or tint == (1.0, 0.82, 0.35):
                tint = (tint_c[0], tint_c[1], tint_c[2])
        for slot in ("_MainTex", "_AlphaTex", "_DistortTex"):
            tr = tex_map.get(slot)
            if tr is None:
                continue
            tname = str(getattr(tr, "m_Name", slot))
            meta["textures"].append({"slot": slot, "material": name, "texture": tname})
            if slot == "_MainTex" and main_img is None:
                try:
                    img = tr.image
                    if img is not None:
                        main_img = img
                        print(f"  [{role}] MainTex from {name} -> {tname} {img.size}")
                except Exception as e:
                    print(f"  [{role}] MainTex fail {name}: {e}", file=sys.stderr)
    return main_img, tint, meta


def write_outputs(role: str, disc: Image.Image, raw: Image.Image | None, meta: dict, out_dirs: list[Path]) -> None:
    for d in out_dirs:
        d.mkdir(parents=True, exist_ok=True)
        disc_path = d / f"{role}_main.png"
        disc.save(disc_path)
        print(f"  -> {disc_path}")
        if raw is not None:
            raw_path = d / f"{role}_sg_maintex.png"
            raw.convert("RGBA").save(raw_path)
        (d / f"{role}_meta.json").write_text(json.dumps(meta, indent=2), encoding="utf-8")


def collect_textures(env) -> dict[str, object]:
    found: dict[str, object] = {}
    for obj in env.objects:
        if obj.type.name != "Texture2D":
            continue
        try:
            data = obj.read()
        except Exception:
            continue
        name = str(getattr(data, "m_Name", "") or "")
        if name and name not in found:
            found[name] = data
    return found


def export_named_textures(tex_by_name: dict[str, object], out_dirs: list[Path]) -> dict[str, str]:
    """Export shell tile + noise PNGs for 3D FieldAuraSphere."""
    exported: dict[str, str] = {}
    for out_name, candidates in SHELL_TEXTURE_EXPORTS.items():
        data = None
        chosen = None
        for cand in candidates:
            data = tex_by_name.get(cand)
            if data is not None:
                chosen = cand
                break
        if data is None:
            print(f"  WARN: missing texture for {out_name} (tried {candidates})", file=sys.stderr)
            continue
        try:
            img = data.image
            if img is None:
                continue
            rgba = img.convert("RGBA")
            for d in out_dirs:
                d.mkdir(parents=True, exist_ok=True)
                path = d / out_name
                rgba.save(path)
                print(f"  -> {path} (from {chosen} {rgba.size})")
            exported[out_name] = chosen or ""
        except Exception as e:
            print(f"  skip {out_name}: {e}", file=sys.stderr)
    return exported


def main() -> int:
    parser = argparse.ArgumentParser(description="Build field aura VFX assets from SG bundles")
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--bundle-dir", type=Path, default=None)
    args = parser.parse_args()

    bundle_dir = args.bundle_dir or resolve_bundle_dir()
    paths = [bundle_dir / n for n in BUNDLE_NAMES]
    missing = [p for p in paths if not p.is_file()]
    if missing:
        print("missing bundles:", missing, file=sys.stderr)
        return 1

    print("source bundles:")
    for p in paths:
        print(f"  {p} ({p.stat().st_size} bytes)")

    if args.dry_run:
        print(f"dry-run: would write discs to {OUT_CONTENT} and {OUT_STREAMING}")
        return 0

    staging = ROOT / "scripts" / "_field_aura_vfx_staging"
    if staging.exists():
        shutil.rmtree(staging)
    staging.mkdir(parents=True, exist_ok=True)

    env = UnityPy.load(*[str(p) for p in paths])
    mats = find_materials(env)
    print(f"materials loaded: {len(mats)}")

    outs = [OUT_CONTENT, OUT_STREAMING]
    tex_by_name = collect_textures(env)
    print(f"textures loaded: {len(tex_by_name)}")
    shell_exports = export_named_textures(tex_by_name, outs)

    manifest: dict = {
        "sourceBundles": [str(p) for p in paths],
        "shellTextures": shell_exports,
        "roles": {},
        "note": (
            "3D FieldAuraSphere uses shell_tile.png + {shield,armor}_noise.png / noise.png; "
            "*_main.png kept as soft fallback disc (HF hot-update)"
        ),
    }

    ok = True
    for role in ("shield", "armor"):
        main_img, tint, meta = pick_role_assets(mats, role)
        meta["tintHdr"] = list(tint)
        meta["tintDisplay"] = list(tonemap_hdr(tint, 0.55))
        if main_img is None:
            print(f"WARN: no MainTex for {role}; tint-only disc", file=sys.stderr)
            ok = False
        disc = composite_disc(main_img, tint, rim_boost=1.0 if role == "shield" else 0.95)
        # also dump raw staging
        if main_img is not None:
            main_img.convert("RGBA").save(staging / f"{role}_raw.png")
        disc.save(staging / f"{role}_disc.png")
        write_outputs(role, disc, main_img, meta, outs)
        manifest["roles"][role] = {
            "file": f"{role}_main.png",
            "materials": meta["materials"],
            "textures": meta["textures"],
            "tintHdr": meta["tintHdr"],
        }

    if "shell_tile.png" not in shell_exports or "noise.png" not in shell_exports:
        ok = False

    for d in outs:
        (d / "manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")

    print("done.")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
