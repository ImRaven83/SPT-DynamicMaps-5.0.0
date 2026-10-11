#!/usr/bin/env python3
"""
Pre-renders the map layer SVGs to PNGs.

SPT 5.0 runs EFT under IL2CPP where Unity.VectorGraphics can't be loaded, so the plugin can no longer tessellate
the SVGs at runtime. Every layer referenced by a map definition (Plugin/Resources/Maps/*/*.jsonc) is rendered to
a PNG next to the SVG; the plugin swaps the extension when loading.

Requires: pip install cairosvg pillow

Usage: python tools/render_map_layers.py [--force]
"""
import argparse
import io
import math
import re
from pathlib import Path

import cairosvg
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
RESOURCES = ROOT / "Plugin" / "Resources"

# pixels per svg viewBox unit, the long side is clamped to a power of two friendly range so small maps
# (e.g. Factory) stay sharp when zoomed and large maps don't blow up texture memory
PIXELS_PER_UNIT = 3.0
MIN_LONG_SIDE = 2048
MAX_LONG_SIDE = 4096

VIEWBOX_RE = re.compile(r'<svg[^>]*\sviewBox="([^"]+)"', re.S)
IMAGE_PATH_RE = re.compile(r'"ImagePath"\s*:\s*"([^"]+\.svg)"')


def layer_svgs():
    paths = set()
    for jsonc in RESOURCES.glob("Maps/*/*.jsonc"):
        for match in IMAGE_PATH_RE.finditer(jsonc.read_text(encoding="utf-8")):
            paths.add(RESOURCES / match.group(1))
    return sorted(paths)


def output_size(svg_text):
    match = VIEWBOX_RE.search(svg_text)
    if not match:
        raise ValueError("svg has no viewBox")
    _, _, width, height = (float(v) for v in re.split(r"[\s,]+", match.group(1).strip()))

    long_side = max(width, height)
    target = min(MAX_LONG_SIDE, max(MIN_LONG_SIDE, long_side * PIXELS_PER_UNIT))
    scale = target / long_side

    # block compression on the client wants dimensions that are a multiple of 4
    def round4(v):
        return max(4, int(math.ceil(v * scale / 4.0)) * 4)

    return round4(width), round4(height)


def render(svg_path, force):
    png_path = svg_path.with_suffix(".png")
    if not force and png_path.exists() and png_path.stat().st_mtime >= svg_path.stat().st_mtime:
        return png_path, False

    svg_text = svg_path.read_text(encoding="utf-8")
    width, height = output_size(svg_text)
    png = cairosvg.svg2png(bytestring=svg_text.encode("utf-8"), output_width=width, output_height=height)

    # palette PNGs are a fraction of the size and decode the same on the client
    image = Image.open(io.BytesIO(png)).convert("RGBA")
    image = image.quantize(colors=256, method=Image.Quantize.FASTOCTREE, dither=Image.Dither.NONE)
    image.save(png_path, optimize=True)
    return png_path, True


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--force", action="store_true", help="re-render even if the png is newer than the svg")
    args = parser.parse_args()

    svgs = layer_svgs()
    missing = [p for p in svgs if not p.exists()]
    if missing:
        raise SystemExit("map definitions reference missing svgs:\n  " + "\n  ".join(str(p) for p in missing))

    for svg in svgs:
        png, rendered = render(svg, args.force)
        size = Image.open(png).size
        status = "rendered" if rendered else "up to date"
        print(f"{status:>10}  {size[0]}x{size[1]}  {png.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
