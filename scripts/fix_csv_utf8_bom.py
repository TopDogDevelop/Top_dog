#!/usr/bin/env python3
"""Re-save CSV as UTF-8 with BOM for Excel on Windows."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "content" / "starting_templates"


def fix(path: Path) -> bool:
    raw = path.read_bytes()
    if not raw:
        return False
    if raw.startswith(b"\xef\xbb\xbf"):
        text = raw[3:].decode("utf-8")
    else:
        for enc in ("utf-8", "gbk", "cp936"):
            try:
                text = raw.decode(enc)
                break
            except UnicodeDecodeError:
                continue
        else:
            raise UnicodeDecodeError("utf-8", raw, 0, 1, path.name)
    path.write_text(text, encoding="utf-8-sig", newline="")
    return True


def main() -> None:
    n = 0
    for path in sorted(ROOT.glob("*.csv")):
        if fix(path):
            n += 1
            print("BOM:", path.name)
    print(f"Done. {n} files.")


if __name__ == "__main__":
    main()
